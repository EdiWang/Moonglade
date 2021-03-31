using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Settings;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CaptchaImageMiddlewareTests
    {
        private MockRepository _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
        }
    }
}
