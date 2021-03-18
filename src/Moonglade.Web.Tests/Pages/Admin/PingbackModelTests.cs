using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Pingback;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PingbackModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPingbackService> _mockPingbackService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _mockPingbackService = _mockRepository.Create<IPingbackService>();
        }

        private PingbackModel CreatePingbackModel()
        {
            return new(_mockPingbackService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IEnumerable<PingbackRecord> pingback = new PingbackRecord[] { };

            _mockPingbackService.Setup(p => p.GetPingbackHistoryAsync())
                .Returns(Task.FromResult(pingback));

            var pingbackModel = CreatePingbackModel();
            await pingbackModel.OnGet();

            Assert.IsNotNull(pingbackModel.PingbackRecords);
        }
    }
}
