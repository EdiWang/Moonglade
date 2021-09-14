using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Pingback;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class PingbackModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private PingbackModel CreatePingbackModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PingbackEntity> pingback = new PingbackEntity[] { };

            _mockMediator.Setup(p => p.Send(It.IsAny<GetPingbacksQuery>(), default))
                .Returns(Task.FromResult(pingback));

            var pingbackModel = CreatePingbackModel();
            await pingbackModel.OnGet();

            Assert.IsNotNull(pingbackModel.PingbackRecords);
        }
    }
}
