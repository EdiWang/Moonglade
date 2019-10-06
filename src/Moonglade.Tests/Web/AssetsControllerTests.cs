using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model.Settings;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class AssetsControllerTests
    {
        private Mock<ILogger<AssetsController>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;
        private Mock<IBlogConfig> _blogConfigMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<AssetsController>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();
            _blogConfigMock = new Mock<IBlogConfig>();
        }
    }
}
