using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Pingback;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class PingbackModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPingbackService> _mockPingbackService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPingbackService = _mockRepository.Create<IPingbackService>();
        }

        private PingbackModel CreatePingbackModel()
        {
            return new(_mockPingbackService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PingbackEntity> pingback = new PingbackEntity[] { };

            _mockPingbackService.Setup(p => p.GetPingbackHistoryAsync())
                .Returns(Task.FromResult(pingback));

            var pingbackModel = CreatePingbackModel();
            await pingbackModel.OnGet();

            Assert.IsNotNull(pingbackModel.PingbackRecords);
        }
    }
}
