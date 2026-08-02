using Azure;
using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Email.Core;
using Moq;
using System.Net;
using System.Text.Json;

namespace Moonglade.Email.Tests;

public class EmailOutboxMessageProcessorTests
{
    [Fact]
    public async Task ProcessNextAsync_WhenNoMessage_ReturnsFalse()
    {
        var store = new FakeEmailOutboxStore();
        var dispatcher = new CapturingDispatcher();
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.False(processed);
        Assert.False(store.Completed);
        Assert.False(store.Failed);
        Assert.False(store.DeadLettered);
    }

    [Fact]
    public async Task ProcessNextAsync_ValidMessage_SendsOneMessagePerRecipientAndCompletes()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com;reader@example.com")
        };
        var dispatcher = new CapturingDispatcher();
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.True(store.Completed);
        Assert.Equal(store.Message.Id, store.CompletedMessageId);
        Assert.False(store.Failed);
        Assert.False(store.DeadLettered);
        Assert.Equal(2, dispatcher.SentMessages.Count);
        Assert.Equal("admin@example.com", dispatcher.SentMessages[0].Receipts.Single());
        Assert.Equal("reader@example.com", dispatcher.SentMessages[1].Receipts.Single());
    }

    [Fact]
    public async Task ProcessNextAsync_PartialRecipientFailure_CompletesMessage()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com;reader@example.com")
        };
        var dispatcher = new CapturingDispatcher
        {
            ExceptionFactory = message =>
                message.Receipts.Single() == "reader@example.com"
                    ? new RequestFailedException((int)HttpStatusCode.BadRequest, "bad recipient")
                    : null
        };
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.True(store.Completed);
        Assert.False(store.Failed);
        Assert.False(store.DeadLettered);
    }

    [Fact]
    public async Task ProcessNextAsync_AllPermanentFailures_DeadLettersMessage()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com")
        };
        var dispatcher = new CapturingDispatcher
        {
            ExceptionFactory = _ => new RequestFailedException((int)HttpStatusCode.BadRequest, "bad request")
        };
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.False(store.Completed);
        Assert.False(store.Failed);
        Assert.True(store.DeadLettered);
        Assert.Contains("Permanent", store.DeadLetterFailure.ErrorMessage);
    }

    [Fact]
    public async Task ProcessNextAsync_AllTransientFailures_SchedulesRetry()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com", attemptCount: 1)
        };
        var dispatcher = new CapturingDispatcher
        {
            ExceptionFactory = _ => new RequestFailedException((int)HttpStatusCode.ServiceUnavailable, "service unavailable")
        };
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.False(store.Completed);
        Assert.True(store.Failed);
        Assert.False(store.DeadLettered);
        Assert.Equal(FixedNow.AddMinutes(1), store.NextAttemptUtc);
        Assert.Contains("Transient", store.Failure.ErrorMessage);
    }

    [Fact]
    public async Task ProcessNextAsync_AllTransientFailuresAtMaxAttempts_DeadLettersMessage()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com", attemptCount: 3)
        };
        var dispatcher = new CapturingDispatcher
        {
            ExceptionFactory = _ => new RequestFailedException((int)HttpStatusCode.ServiceUnavailable, "service unavailable")
        };
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.False(store.Completed);
        Assert.False(store.Failed);
        Assert.True(store.DeadLettered);
        Assert.Contains("Max retry attempts reached", store.DeadLetterFailure.ErrorMessage);
    }

    [Fact]
    public async Task ProcessNextAsync_InvalidPayload_DeadLettersWithoutSending()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com", messageBody: "{")
        };
        var dispatcher = new CapturingDispatcher();
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.Empty(dispatcher.SentMessages);
        Assert.True(store.DeadLettered);
        Assert.Contains("Payload is not valid JSON", store.DeadLetterFailure.ErrorMessage);
    }

    [Fact]
    public async Task ProcessNextAsync_UnknownException_IsTreatedAsTransient()
    {
        var store = new FakeEmailOutboxStore
        {
            Message = CreateOutboxMessage("admin@example.com", attemptCount: 2)
        };
        var dispatcher = new CapturingDispatcher
        {
            ExceptionFactory = _ => new InvalidOperationException("unexpected")
        };
        var processor = CreateProcessor(store, dispatcher);

        var processed = await processor.ProcessNextAsync("worker-1", TestContext.Current.CancellationToken);

        Assert.True(processed);
        Assert.True(store.Failed);
        Assert.Equal(FixedNow.AddMinutes(2), store.NextAttemptUtc);
        Assert.Contains("Transient", store.Failure.ErrorMessage);
    }

    [Fact]
    public void EmailOutboxWorkerOptionsValidator_InvalidValues_Fails()
    {
        var validator = new EmailOutboxWorkerOptionsValidator();
        var options = new EmailOutboxWorkerOptions
        {
            BatchSize = 0,
            PollIntervalSeconds = 0,
            LeaseDurationSeconds = 0,
            MaxAttempts = 0,
            InitialRetryDelaySeconds = 60,
            MaxRetryDelaySeconds = 30
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("BatchSize", result.FailureMessage);
        Assert.Contains("MaxRetryDelaySeconds", result.FailureMessage);
    }

    private static readonly DateTime FixedNow = new(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);

    private static EmailOutboxMessageProcessor CreateProcessor(
        FakeEmailOutboxStore store,
        CapturingDispatcher dispatcher)
    {
        var emailHelper = new Mock<IEmailHelper>();
        emailHelper
            .Setup(h => h.ForType(It.IsAny<string>()))
            .Returns(emailHelper.Object);
        emailHelper
            .Setup(h => h.Map(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(emailHelper.Object);
        emailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns((string[] recipients, string[] _) => new CommonMailMessage
            {
                Subject = "Subject",
                Body = "Body",
                BodyIsHtml = true,
                Receipts = recipients
            });

        return new EmailOutboxMessageProcessor(
            store,
            new MessageBuilder(emailHelper.Object),
            new EmailSettings
            {
                SmtpSettings = new SmtpSettings("localhost", string.Empty, string.Empty, 25)
            },
            dispatcher,
            Options.Create(new EmailOutboxWorkerOptions
            {
                InitialRetryDelaySeconds = 60,
                MaxRetryDelaySeconds = 300,
                MaxAttempts = 3
            }),
            new FixedTimeProvider(FixedNow),
            Mock.Of<ILogger<EmailOutboxMessageProcessor>>());
    }

    private static EmailOutboxMessage CreateOutboxMessage(
        string distributionList,
        int attemptCount = 1,
        string messageBody = null)
    {
        var payload = new NewCommentPayload
        {
            Username = "Reader",
            Email = "reader@example.com",
            IpAddress = "127.0.0.1",
            PostTitle = "Hello",
            CommentContent = "Great post"
        };

        return new EmailOutboxMessage(
            Guid.NewGuid(),
            MessageTypes.NewCommentNotification,
            distributionList,
            messageBody ?? JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default),
            attemptCount,
            FixedNow.AddMinutes(-5));
    }
    private sealed class FakeEmailOutboxStore : IEmailOutboxStore
    {
        public EmailOutboxMessage Message { get; set; }
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }
        public bool DeadLettered { get; private set; }
        public Guid CompletedMessageId { get; private set; }
        public EmailOutboxFailure Failure { get; private set; }
        public EmailOutboxFailure DeadLetterFailure { get; private set; }
        public DateTime NextAttemptUtc { get; private set; }

        public Task<Guid> EnqueueAsync(EmailNotification notification, CancellationToken cancellationToken = default) =>
            Task.FromResult(Guid.NewGuid());

        public Task<EmailOutboxMessage> ClaimNextAsync(
            EmailOutboxClaimRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Message);

        public Task CompleteAsync(Guid messageId, DateTime completedTimeUtc, CancellationToken cancellationToken = default)
        {
            Completed = true;
            CompletedMessageId = messageId;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            EmailOutboxFailure failure,
            DateTime nextAttemptUtc,
            CancellationToken cancellationToken = default)
        {
            Failed = true;
            Failure = failure;
            NextAttemptUtc = nextAttemptUtc;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(EmailOutboxFailure failure, CancellationToken cancellationToken = default)
        {
            DeadLettered = true;
            DeadLetterFailure = failure;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingDispatcher : IEmailDispatcher
    {
        public List<CommonMailMessage> SentMessages { get; } = [];
        public Func<CommonMailMessage, Exception> ExceptionFactory { get; set; }

        public Task SendAsync(CommonMailMessage message)
        {
            SentMessages.Add(message);
            var exception = ExceptionFactory?.Invoke(message);
            if (exception != null)
            {
                throw exception;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
