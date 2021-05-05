using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration;
using Moonglade.FriendLink;
using Moonglade.Web.Middleware;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    public class FoafMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IFoafWriter> _mockFoafWriter;
        private Mock<IFriendLinkService> _mockFriendLinkService;
        private Mock<LinkGenerator> _mockLinkGenerator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockFoafWriter = _mockRepository.Create<IFoafWriter>();
            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
            _mockLinkGenerator = _mockRepository.Create<LinkGenerator>();

            _mockBlogConfig.Setup(bc => bc.GeneralSettings).Returns(new GeneralSettings
            {
                SiteTitle = "Fake Title",
                OwnerName = "Jack Ma",
                OwnerEmail = "TheBigFubao@996.icu",
                CanonicalPrefix = FakeData.Url1
            });

            IReadOnlyList<Link> links = new[]
            {
                new Link
                {
                    Id = FakeData.Uid1,
                    LinkUrl = FakeData.Url1,
                    Title = "Fubao"
                }
            };
            _mockFriendLinkService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(links));

            var xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<rdf:RDF xmlns:foaf=""http://xmlns.com/foaf/0.1/"" xmlns:rdfs=""http://www.w3.org/2000/01/rdf-schema#"" xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
  <foaf:PersonalProfileDocument rdf:about="""">
    <foaf:maker rdf:resource=""#me"" />
    <foaf:primaryTopic rdf:resource=""#me"" />
  </foaf:PersonalProfileDocument>
  <foaf:Person>
    <foaf:name>Edi Wang</foaf:name>
    <foaf:mbox_sha1sum>ffa4d0a8a3738b7a89ca925bd07e16f66260baa6</foaf:mbox_sha1sum>
    <foaf:weblog rdf:resource=""https://edi.wang/"" />
    <foaf:depiction rdf:resource=""https://edi.wang/avatar"" />
    <foaf:knows>
      <foaf:Person>
        <foaf:name>Jack Ma</foaf:name>
        <foaf:homepage rdf:resource=""https://996.icu/"" />
      </foaf:Person>
    </foaf:knows>
  </foaf:Person>
</rdf:RDF>";
            _mockFoafWriter.Setup(p =>
                p.GetFoafData(It.IsAny<FoafDoc>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<Link>>())).Returns(Task.FromResult(xml));
        }

        [Test]
        public async Task Invoke_NonFoafUrl()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/996";

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new FoafMiddleware(RequestDelegate);

            await middleware.Invoke(context, _mockBlogConfig.Object, _mockFoafWriter.Object, _mockFriendLinkService.Object, _mockLinkGenerator.Object);

            _mockFriendLinkService.Verify(p => p.GetAllAsync(), Times.Never);
            _mockFoafWriter.Verify(p => p.GetFoafData(It.IsAny<FoafDoc>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<Link>>()), Times.Never);
            Assert.Pass();
        }

        [Test]
        public async Task Invoke_FoafUrl()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/foaf.xml";
            context.Request.Scheme = "https";
            context.Request.Host = new("996.icu");
            context.Response.Body = new MemoryStream();

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new FoafMiddleware(RequestDelegate);

            await middleware.Invoke(context, _mockBlogConfig.Object, _mockFoafWriter.Object, _mockFriendLinkService.Object, _mockLinkGenerator.Object);

            _mockFriendLinkService.Verify(p => p.GetAllAsync());
            _mockFoafWriter.Verify(p => p.GetFoafData(It.IsAny<FoafDoc>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<Link>>()));
            Assert.Pass();
        }
    }
}
