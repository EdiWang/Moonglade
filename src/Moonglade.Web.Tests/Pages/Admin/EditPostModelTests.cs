using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class EditPostModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IPostService> _mockPostService;
        private Mock<ITZoneResolver> _mockTZoneResolver;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockPostService = _mockRepository.Create<IPostService>();
            _mockTZoneResolver = _mockRepository.Create<ITZoneResolver>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private EditPostModel CreateEditPostModel()
        {
            return new(
                _mockCategoryService.Object,
                _mockPostService.Object,
                _mockTZoneResolver.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task OnGetAsync_CreatePost()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new(){Id = Guid.Empty, DisplayName = "Work 996", Note = "Get into ICU", RouteName = "work-996"}
            };

            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                DefaultLangCode = "en-US"
            });

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(null);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(editPostModel.PostEditModel);
            Assert.IsNull(editPostModel.PostEditModel.EditorContent);
        }
    }
}
