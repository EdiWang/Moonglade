using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PostManageServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<ILogger<PostManageService>> _mockLogger;
        private Mock<IRepository<TagEntity>> _mockTagEntityRepo;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IOptions<Dictionary<string, string>>> _mockOptionsTagNormalization;
        private Mock<IBlogCache> _mockBlogCache;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        private readonly PostEntity _postEntity = new()
        {
            Id = Uid,
            Title = "Work 996 and Get into ICU",
            Slug = "work-996-and-get-into-icu",
            ContentLanguageCode = "en-us",
            PostContent = "<p>996 is fubao</p>",
            ContentAbstract = "996 is fubao",
            CommentEnabled = true,
            ExposedToSiteMap = true,
            IsFeedIncluded = true,
            IsPublished = true,
            IsFeatured = true,
            PostExtension = new() { Hits = 996, Likes = 251, PostId = Uid },
            IsDeleted = false,
            CreateTimeUtc = new(2020, 9, 9, 6, 35, 7),
            LastModifiedUtc = new(2021, 2, 5, 1, 4, 4),
            PubDateUtc = new(2020, 10, 5, 5, 6, 6),
            Tags = new List<TagEntity> { new() { DisplayName = "996", Id = 996, NormalizedName = "996" } },
            PostCategory = new List<PostCategoryEntity> { new() { PostId = Uid, CategoryId = Guid.Parse("b20b3a09-f436-4b42-877c-f6acdd16b105") } }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockLogger = _mockRepository.Create<ILogger<PostManageService>>();
            _mockTagEntityRepo = _mockRepository.Create<IRepository<TagEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockOptionsTagNormalization = _mockRepository.Create<IOptions<Dictionary<string, string>>>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private PostManageService CreateService()
        {
            return new(
                _mockBlogAudit.Object,
                _mockLogger.Object,
                _mockOptionsTagNormalization.Object,
                _mockOptionsAppSettings.Object,
                _mockTagEntityRepo.Object,
                _mockPostEntityRepo.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task CreateAsync_HTMLEditor_HappyPath()
        {
            _mockOptionsAppSettings.Setup(p => p.Value).Returns(new AppSettings
            {
                PostAbstractWords = 404,
                Editor = EditorChoice.Html
            });

            _mockOptionsTagNormalization.Setup(p => p.Value).Returns(new Dictionary<string, string>());
            _mockPostEntityRepo.Setup(p => p.Any(It.IsAny<PostSpec>())).Returns(false);
            _mockTagEntityRepo.Setup(p => p.GetAsync(It.IsAny<Expression<Func<TagEntity, bool>>>()))
                .Returns(Task.FromResult((TagEntity)null));
            _mockTagEntityRepo.Setup(p => p.AddAsync(It.IsAny<TagEntity>())).Returns(Task.FromResult(new TagEntity()));

            var req = new UpdatePostRequest
            {
                Title = "Work 996 and Get into ICU",
                Slug = "work-996-and-get-into-icu",
                ContentLanguageCode = "en-us",
                EditorContent = "<p>996 is fubao</p>",
                EnableComment = true,
                ExposedToSiteMap = true,
                IsFeedIncluded = true,
                IsPublished = true,
                IsFeatured = true,
                Tags = new[] { "996", "Fubao" },
                CategoryIds = new[] { Uid }
            };

            var svc = CreateService();
            var result = await svc.CreateAsync(req);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(Guid.Empty, result.Id);
            _mockPostEntityRepo.Verify(p => p.AddAsync(It.IsAny<PostEntity>()));
        }

        [Test]
        public void UpdateAsync_NullPost()
        {
            _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult((PostEntity)null));
            var svc = CreateService();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await svc.UpdateAsync(Uid, new());
            });
        }

        [Test]
        public async Task UpdateAsync_HTMLEditor_HappyPath()
        {
            _mockOptionsTagNormalization.Setup(p => p.Value).Returns(new Dictionary<string, string>());
            _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult(_postEntity));
            _mockTagEntityRepo.Setup(p => p.Any(It.IsAny<Expression<Func<TagEntity, bool>>>())).Returns(false);
            _mockTagEntityRepo.Setup(p => p.AddAsync(It.IsAny<TagEntity>())).Returns(Task.FromResult(new TagEntity()));
            _mockOptionsAppSettings.Setup(p => p.Value).Returns(new AppSettings
            {
                PostAbstractWords = 404,
                Editor = EditorChoice.Html
            });

            var req = new UpdatePostRequest
            {
                Title = "Work 996 and Get into ICU",
                Slug = "work-996-and-get-into-icu",
                ContentLanguageCode = "en-us",
                EditorContent = "<p>996 is fubao</p>",
                EnableComment = true,
                ExposedToSiteMap = true,
                IsFeedIncluded = true,
                IsPublished = true,
                IsFeatured = true,
                Tags = new[] { "996", "Fubao" },
                CategoryIds = new[] { Uid }
            };

            var svc = CreateService();
            var result = await svc.UpdateAsync(Uid, req);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(Guid.Empty, result.Id);
            _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()));
        }

        [Test]
        public async Task RestoreAsync_NullPost()
        {
            _mockPostEntityRepo.Setup(p => p.GetAsync(Guid.Empty)).Returns(ValueTask.FromResult((PostEntity)null));

            var svc = CreateService();
            await svc.RestoreAsync(Guid.Empty);

            _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()), Times.Never);
        }

        [Test]
        public async Task RestoreAsync_OK()
        {
            var post = new PostEntity
            {
                IsDeleted = true
            };

            _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult(post));

            var svc = CreateService();
            await svc.RestoreAsync(Uid);

            _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()));
            Assert.IsFalse(post.IsDeleted);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DeleteAsync_NullPost(bool softDelete)
        {
            _mockPostEntityRepo.Setup(p => p.GetAsync(Guid.Empty)).Returns(ValueTask.FromResult((PostEntity)null));

            var svc = CreateService();
            await svc.DeleteAsync(Guid.Empty, softDelete);

            _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()), Times.Never);
            _mockPostEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<PostEntity>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_SoftDelete()
        {
            var post = new PostEntity
            {
                IsDeleted = false
            };

            _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult(post));

            var svc = CreateService();
            await svc.DeleteAsync(Uid, true);

            _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()));
            Assert.IsTrue(post.IsDeleted);
        }

        [Test]
        public async Task DeleteAsync_HardDelete()
        {
            var post = new PostEntity
            {
                IsDeleted = false
            };

            _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult(post));

            var svc = CreateService();
            await svc.DeleteAsync(Uid);

            _mockPostEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<PostEntity>()));
            Assert.IsFalse(post.IsDeleted);
        }

        [Test]
        public async Task PurgeRecycledAsync_OK()
        {
            IReadOnlyList<PostEntity> entities = new List<PostEntity> { _postEntity };

            _mockPostEntityRepo.Setup(p => p.GetAsync(It.IsAny<ISpecification<PostEntity>>(), true))
                .Returns(Task.FromResult(entities));

            var svc = CreateService();
            await svc.PurgeRecycledAsync();

            _mockPostEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<IEnumerable<PostEntity>>()));
        }
    }
}
