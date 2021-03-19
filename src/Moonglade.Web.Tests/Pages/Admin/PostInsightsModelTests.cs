using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Moonglade.Data.Spec;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PostInsightsModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostService>();
        }

        private PostInsightsModel CreatePostInsightsModel()
        {
            return new(_mockPostService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PostSegment> segments = new List<PostSegment>();

            _mockPostService.Setup(p => p.ListInsights(It.IsAny<PostInsightsType>()))
                .Returns(Task.FromResult(segments));

            var postInsightsModel = CreatePostInsightsModel();
            await postInsightsModel.OnGet();

            Assert.IsNotNull(postInsightsModel.TopReadList);
            Assert.IsNotNull(postInsightsModel.TopCommentedList);
        }
    }
}
