using MediatR;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class ArchiveModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private ArchiveModel CreateArchiveModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            var fakeArchives = new List<Archive>
            {
                new (FakeData.Int2,9,6),
                new (FakeData.Int1,3,5)
            };

            _mockMediator.Setup(p => p.Send(It.IsAny<GetArchiveQuery>(), default))
                .Returns(Task.FromResult((IReadOnlyList<Archive>)fakeArchives));

            var archiveModel = CreateArchiveModel();

            await archiveModel.OnGet();

            Assert.IsNotNull(archiveModel.Archives);
        }
    }
}
