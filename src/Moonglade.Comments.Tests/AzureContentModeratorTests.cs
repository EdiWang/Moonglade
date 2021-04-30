using Microsoft.Azure.CognitiveServices.ContentModerator;
using Moonglade.Comments;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using Microsoft.Rest;

namespace Moonglade.Comments.Tests
{
    [TestFixture]
    public class AzureContentModeratorTests
    {
        private MockRepository _mockRepository;
        private Mock<IContentModeratorClient> _mockContentModeratorClient;
        private Mock<ITextModeration> _mockTextModeration;
        private readonly Screen _screen = new()
        {
            Terms = new List<DetectedTerms>()
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockContentModeratorClient = _mockRepository.Create<IContentModeratorClient>();
            _mockTextModeration = _mockRepository.Create<ITextModeration>();
        }

        private AzureContentModerator CreateAzureContentModerator()
        {
            _mockTextModeration.Setup(p => p.ScreenTextWithHttpMessagesAsync(
                    "text/plain",
                    It.IsAny<MemoryStream>(),
                    null,
                    false,
                    false,
                    null,
                    false,
                    null,
                    CancellationToken.None))
                .Returns(Task.FromResult(new HttpOperationResponse<Screen>
                {
                    Body = _screen
                }));

            _mockContentModeratorClient.Setup(p => p.TextModeration).Returns(_mockTextModeration.Object);

            return new(_mockContentModeratorClient.Object);
        }

        [Test]
        public async Task ModerateContent_StateUnderTest_ExpectedBehavior()
        {
            _screen.Terms.Add(new() { Term = "fuck" });

            var azureContentModerator = CreateAzureContentModerator();
            string input = "fuck 996";

            var result = await azureContentModerator.ModerateContent(input);

            Assert.AreEqual("* 996", result);
        }

        [Test]
        public async Task HasBadWord_True()
        {
            _screen.Terms.Add(new() { Term = "fuck" });

            var azureContentModerator = CreateAzureContentModerator();
            string[] input = { "fuck 996" };

            var result = await azureContentModerator.HasBadWord(input);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasBadWord_False()
        {
            var azureContentModerator = CreateAzureContentModerator();
            string[] input = { "go to icu" };

            var result = await azureContentModerator.HasBadWord(input);
            Assert.IsFalse(result);
        }
    }
}
