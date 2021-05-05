using System.Threading.Tasks;
using Moonglade.Configuration;
using Moq;
using NUnit.Framework;

namespace Moonglade.Comments.Tests
{
    [TestFixture]
    public class LocalWordFilterModeratorTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private LocalWordFilterModerator CreateLocalWordFilterModerator()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                DisharmonyWords = "fuck|shit"
            });

            return new(
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task ModerateContent_ExpectedBehavior()
        {
            var localWordFilterModerator = CreateLocalWordFilterModerator();
            string input = "Fuck Jack Ma";
            var result = await localWordFilterModerator.ModerateContent(input);

            Assert.AreEqual("**** Jack Ma", result);
        }

        [Test]
        public async Task HasBadWord_ExpectedBehavior()
        {
            var localWordFilterModerator = CreateLocalWordFilterModerator();
            var result = await localWordFilterModerator.HasBadWord("Fuck 996", "Fuck some shit");
            Assert.IsTrue(result);
        }
    }
}
