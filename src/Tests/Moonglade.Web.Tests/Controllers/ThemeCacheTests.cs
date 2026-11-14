using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data;
using Moonglade.Theme;
using Moonglade.Web.Controllers;
using Moq;

namespace Moonglade.Web.Tests.Controllers;

public class ThemeCacheTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public async Task ThemeChanges_InvalidateOnlyAfterSuccessfulCommands(bool delete, bool success)
    {
        var themes = new ThemeCacheInvalidator();
        var key = themes.GetCacheKey(1);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<CreateThemeCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? 1 : -1);
        commands.Setup(m => m.SendAsync(It.IsAny<DeleteThemeCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? OperationCode.Done : OperationCode.ObjectNotFound);
        var controller = new ThemeController(commands.Object, themes,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ThemeController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        if (delete) await controller.Delete(1);
        else await controller.Create(new CreateThemeRequest { Name = "Test", AccentColor = "#abcdef" });

        Assert.Equal(success, key != themes.GetCacheKey(1));
    }
}
