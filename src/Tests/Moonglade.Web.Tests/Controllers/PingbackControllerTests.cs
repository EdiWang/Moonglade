using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Pingback;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers;

[TestFixture]
public class PingbackControllerTests
{
    private MockRepository _mockRepository;

    private Mock<ILogger<PingbackController>> _mockLogger;
    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<IMediator> _mockMediator;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Loose);

        _mockLogger = _mockRepository.Create<ILogger<PingbackController>>();
        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockMediator = _mockRepository.Create<IMediator>();
        _mockServiceScopeFactory = _mockRepository.Create<IServiceScopeFactory>();
    }

    private PingbackController CreatePingbackController()
    {
        return new(
            _mockLogger.Object,
            _mockBlogConfig.Object,
            _mockMediator.Object);
    }

    [Test]
    public async Task Process_PingbackDisabled()
    {
        _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings
        {
            EnablePingbackReceive = false
        });

        var pingbackController = CreatePingbackController();
        var result = await pingbackController.Process(_mockServiceScopeFactory.Object);
        Assert.IsInstanceOf(typeof(ForbidResult), result);
    }

    [Test]
    public async Task Process_OK()
    {
        _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings
        {
            EnablePingbackReceive = true
        });

        _mockMediator
            .Setup(p => p.Send(It.IsAny<ReceivePingCommand>(), default)).Returns(Task.FromResult(PingbackResponse.Success));

        var pingbackController = CreatePingbackController();
        pingbackController.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await pingbackController.Process(_mockServiceScopeFactory.Object);
        Assert.IsInstanceOf(typeof(PingbackResult), result);
    }

    [Test]
    public async Task Delete_Success()
    {
        var pingbackController = CreatePingbackController();

        var result = await pingbackController.Delete(Guid.Empty);
        Assert.IsInstanceOf(typeof(NoContentResult), result);
    }

    [Test]
    public async Task Clear_Success()
    {
        var pingbackController = CreatePingbackController();

        var result = await pingbackController.Clear();
        Assert.IsInstanceOf(typeof(NoContentResult), result);
    }
}