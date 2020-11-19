using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Web.Controllers;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BlogControllerTests
    {
        [Test]
        public void ServerError()
        {
            var ctl = new BlogController();
            var result = ctl.ServerError();
            Assert.IsInstanceOf(typeof(StatusCodeResult), result);
            if (result is StatusCodeResult rdResult)
            {
                Assert.That(rdResult.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public void ServerError_HasMessage()
        {
            var ctl = new BlogController();
            var result = ctl.ServerError("Work 996");
            Assert.IsInstanceOf(typeof(ObjectResult), result);
            if (result is ObjectResult rdResult)
            {
                Assert.That(rdResult.StatusCode, Is.EqualTo(500));
                Assert.IsInstanceOf<string>(rdResult.Value);
                Assert.That(rdResult.Value.ToString(), Is.EqualTo("Work 996"));
            }
        }
    }
}
