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
using Microsoft.AspNetCore.Mvc;

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

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        private static readonly Category Cat = new()
        {
            DisplayName = "WTF",
            Id = Guid.Parse("6364e9be-2423-44da-bd11-bc6fa9c3fa5d"),
            Note = "A wonderful contry",
            RouteName = "wtf"
        };

        private static readonly Post Post = new()
        {
            Id = Uid,
            Title = "Work 996 and Get into ICU",
            Slug = "work-996-and-get-into-icu",
            ContentAbstract = "Get some fubao",
            RawPostContent = "<p>Get some fubao</p>",
            ContentLanguageCode = "en-us",
            Featured = true,
            ExposedToSiteMap = true,
            IsFeedIncluded = true,
            IsPublished = true,
            CommentEnabled = true,
            PubDateUtc = new(2019, 9, 6, 6, 35, 7),
            LastModifiedUtc = new(2020, 9, 6, 6, 35, 7),
            CreateTimeUtc = new(2018, 9, 6, 6, 35, 7),
            Tags = new[]
            {
                new Tag { DisplayName = "Fubao", Id = 996, NormalizedName = "fubao" },
                new Tag { DisplayName = "996", Id = 251, NormalizedName = "996" }
            },
            Categories = new[] { Cat }
        };

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
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                DefaultLangCode = "en-US"
            });

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

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(null);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(editPostModel.PostEditModel);
            Assert.IsNull(editPostModel.PostEditModel.EditorContent);
        }

        [Test]
        public async Task OnGetAsync_NotFound()
        {
            _mockPostService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult((Post)null));

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task OnGetAsync_FoundPost()
        {
            IReadOnlyList<Category> cats = new List<Category> { Cat };

            _mockPostService.Setup(p => p.GetAsync(Uid)).Returns(Task.FromResult(Post));
            _mockCategoryService.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var editPostModel = CreateEditPostModel();
            var result = await editPostModel.OnGetAsync(Uid);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(editPostModel.PostEditModel);
        }
    }
}
