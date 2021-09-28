using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core.StatisticFeature;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class StatisticsControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private StatisticsController CreateStatisticsController()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task Get_OK()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetStatisticQuery>(), default))
                         .Returns(Task.FromResult((996, 404)));

            var ctl = CreateStatisticsController();
            var result = await ctl.Get(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));

            Assert.IsInstanceOf(typeof(OkObjectResult), result);
        }

        [Test]
        public async Task Hit_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_mockMediator.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = false });
            Assert.IsInstanceOf(typeof(NoContentResult), result);
        }

        [Test]
        public async Task Like_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_mockMediator.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = true });
            Assert.IsInstanceOf(typeof(NoContentResult), result);
        }
    }
}
