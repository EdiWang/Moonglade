using MediatR;
using Moonglade.Comments;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class CommentsModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private CommentsModel CreateCommentsModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<CommentDetailedItem> comments = new List<CommentDetailedItem>();

            _mockMediator.Setup(p => p.Send(It.IsAny<GetCommentsQuery>(), default))
                .Returns(Task.FromResult(comments));
            _mockMediator.Setup(p => p.Send(It.IsAny<CountCommentsQuery>(), default)).Returns(Task.FromResult(FakeData.Int2));

            var commentsModel = CreateCommentsModel();
            int pageIndex = 1;

            await commentsModel.OnGet(pageIndex);

            Assert.IsNotNull(commentsModel.CommentDetailedItems);
        }
    }
}
