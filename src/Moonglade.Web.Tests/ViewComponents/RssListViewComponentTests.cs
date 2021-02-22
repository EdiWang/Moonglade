using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class RssListViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<RssListViewComponent>> _mockLogger;
        private Mock<ICategoryService> _mockCategoryService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<RssListViewComponent>>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
        }

        private RssListViewComponent CreateComponent()
        {
            return new(
                _mockLogger.Object,
                _mockCategoryService.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockCategoryService.Setup(p => p.GetAll()).Throws(new("996"));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var message = ((ViewViewComponentResult)result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new() {DisplayName = "Fubao", Id = Guid.Empty, Note = "996", RouteName = "work-996"}
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var model = ((ViewViewComponentResult)result).ViewData.Model;
            Assert.IsNotNull(model);
        }
    }
}
