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
    public class PostDraftModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostService>();
        }

        private PostDraftModel CreatePostDraftModel()
        {
            return new(_mockPostService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PostSegment> data = new List<PostSegment>();

            _mockPostService.Setup(p => p.ListSegment(PostStatus.Draft)).Returns(Task.FromResult(data));

            var postDraftModel = CreatePostDraftModel();
            await postDraftModel.OnGet();

            Assert.IsNotNull(postDraftModel.PostSegments);
        }
    }
}
