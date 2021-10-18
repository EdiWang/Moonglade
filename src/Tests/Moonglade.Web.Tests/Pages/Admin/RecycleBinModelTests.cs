using MediatR;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class RecycleBinModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private RecycleBinModel CreateRecycleBinModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PostSegment> segments = new List<PostSegment>();

            _mockMediator.Setup(p => p.Send(It.IsAny<ListPostSegmentByStatusQuery>(), default))
                .Returns(Task.FromResult(segments));

            var recycleBinModel = CreateRecycleBinModel();
            await recycleBinModel.OnGet();

            Assert.IsNotNull(recycleBinModel.Posts);
        }
    }
}
