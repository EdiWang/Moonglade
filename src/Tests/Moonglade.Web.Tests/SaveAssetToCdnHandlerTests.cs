using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
using Moonglade.Web.Handlers;
using Moq;

namespace Moonglade.Web.Tests;

public class SaveAssetToCdnHandlerTests
{
    private const string AvatarFileName = "avatar-0922e4.png";

    private readonly Mock<ILogger<SaveAssetToCdnHandler>> _logger = new();
    private readonly Mock<IBlogImageStorage> _imageStorage = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    [Fact]
    public async Task HandleAsync_WhenAssetIsNotAvatar_DoesNothing()
    {
        var handler = CreateHandler();
        var request = new SaveAssetEvent(Guid.NewGuid(), Convert.ToBase64String([1, 2, 3]));

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenCdnRedirectIsDisabled_DoesNothing()
    {
        var blogConfig = CreateBlogConfig(enableCdnRedirect: false);
        var handler = CreateHandler(blogConfig);
        var request = new SaveAssetEvent(AssetId.AvatarBase64, Convert.ToBase64String([1, 2, 3]));

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenAssetBase64IsEmpty_DoesNothing()
    {
        var handler = CreateHandler();
        var request = new SaveAssetEvent(AssetId.AvatarBase64, string.Empty);

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenAvatarIsSaved_UpdatesAvatarUrlAndPersistsConfiguration()
    {
        var blogConfig = CreateBlogConfig();
        var handler = CreateHandler(blogConfig);
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var request = new SaveAssetEvent(AssetId.AvatarBase64, Convert.ToBase64String(imageBytes));

        _imageStorage
            .Setup(x => x.InsertAsync(AvatarFileName, It.IsAny<byte[]>()))
            .ReturnsAsync("avatar-cdn.png");

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(AvatarFileName), Times.Once);
        _imageStorage.Verify(x => x.InsertAsync(AvatarFileName, It.Is<byte[]>(bytes => bytes.SequenceEqual(imageBytes))), Times.Once);

        var avatarUrl = blogConfig.GeneralSettings.AvatarUrl;
        Assert.NotNull(avatarUrl);
        Assert.StartsWith("https://cdn.example.com/images/avatar-cdn.png?", avatarUrl);

        var cacheBuster = avatarUrl.Split('?')[1];
        Assert.InRange(int.Parse(cacheBuster), 100, 999);

        var command = _commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Equal(nameof(GeneralSettings), command.Name);

        var persistedSettings = command.Json.FromJson<GeneralSettings>();
        Assert.NotNull(persistedSettings);
        Assert.Equal(avatarUrl, persistedSettings!.AvatarUrl);
    }

    private SaveAssetToCdnHandler CreateHandler(BlogConfig? blogConfig = null)
    {
        return new SaveAssetToCdnHandler(
            _logger.Object,
            _imageStorage.Object,
            blogConfig ?? CreateBlogConfig(),
            _commandMediator);
    }

    private static BlogConfig CreateBlogConfig(bool enableCdnRedirect = true)
    {
        return new BlogConfig
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = new ImageSettings
            {
                EnableCDNRedirect = enableCdnRedirect,
                CDNEndpoint = "https://cdn.example.com/images"
            }
        };
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CommandMediationSettings settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);

            if (typeof(TCommandResult) == typeof(OperationCode))
            {
                return Task.FromResult((TCommandResult)(object)OperationCode.Done);
            }

            return Task.FromException<TCommandResult>(new NotSupportedException("No command results configured for this test."));
        }

        public TCommand Single<TCommand>() where TCommand : ICommand
        {
            return Commands.OfType<TCommand>().Single();
        }
    }
}
