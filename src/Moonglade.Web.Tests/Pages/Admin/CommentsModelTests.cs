using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Comments;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CommentsModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ICommentService> _mockCommentService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);
            _mockCommentService = _mockRepository.Create<ICommentService>();
        }

        private CommentsModel CreateCommentsModel()
        {
            return new(
                _mockCommentService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<CommentDetailedItem> comments = new List<CommentDetailedItem>();

            _mockCommentService.Setup(p => p.GetCommentsAsync(It.IsAny<int>(), 1))
                .Returns(Task.FromResult(comments));
            _mockCommentService.Setup(p => p.Count()).Returns(996);

            var commentsModel = CreateCommentsModel();
            int pageIndex = 1;

            await commentsModel.OnGet(pageIndex);

            Assert.IsNotNull(commentsModel.CommentDetailedItems);
        }
    }
}
