using Microsoft.Extensions.Logging;
using Moonglade.Comments;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CommentListViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<CommentListViewComponent>> _mockLogger;
        private Mock<ICommentService> _mockCommentService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<CommentListViewComponent>>();
            _mockCommentService = _mockRepository.Create<ICommentService>();
        }

        private CommentListViewComponent CreateComponent()
        {
            return new(
                _mockLogger.Object,
                _mockCommentService.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockCommentService.Setup(p => p.GetApprovedCommentsAsync(It.IsAny<Guid>())).Throws(new("996"));

            var component = CreateComponent();
            var result = await component.InvokeAsync(Guid.NewGuid());

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var message = ((ViewViewComponentResult)result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }
    }
}
