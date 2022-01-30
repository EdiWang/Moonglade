using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;
using System.Text;

namespace Moonglade.Core.Tests;

[TestFixture]
public class PostManageServiceTests
{
    private MockRepository _mockRepository;

    private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
    private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
    private Mock<ILogger<CreatePostCommandHandler>> _mockLogger2;
    private Mock<IRepository<TagEntity>> _mockTagEntityRepo;
    private Mock<IBlogConfig> _mockBlogConfig;
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
        _mockLogger2 = _mockRepository.Create<ILogger<CreatePostCommandHandler>>();
        _mockTagEntityRepo = _mockRepository.Create<IRepository<TagEntity>>();
        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockBlogCache = _mockRepository.Create<IBlogCache>();

        _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
        {
            PostAbstractWords = 404
        });
    }

    private IConfigurationRoot GetFakeConfiguration()
    {
        var config = @"{""TagNormalization"":{""."": ""dot""}}";
        var builder = new ConfigurationBuilder();
        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(config)));
        var configuration = builder.Build();

        return configuration;
    }

    [Test]
    public async Task CreateAsync_HTMLEditor_HappyPath()
    {
        _mockOptionsAppSettings.Setup(p => p.Value).Returns(new AppSettings
        {
            Editor = EditorChoice.Html
        });

        _mockPostEntityRepo.Setup(p => p.Any(It.IsAny<PostSpec>())).Returns(false);
        _mockTagEntityRepo.Setup(p => p.GetAsync(It.IsAny<Expression<Func<TagEntity, bool>>>()))
            .Returns(Task.FromResult((TagEntity)null));
        _mockTagEntityRepo.Setup(p => p.AddAsync(It.IsAny<TagEntity>())).Returns(Task.FromResult(new TagEntity()));

        var req = new PostEditModel
        {
            Title = "Work 996 and Get into ICU",
            Slug = "work-996-and-get-into-icu",
            LanguageCode = "en-us",
            EditorContent = "<p>996 is fubao</p>",
            EnableComment = true,
            FeedIncluded = true,
            IsPublished = true,
            Featured = true,
            Tags = "996,Fubao",
            SelectedCatIds = new[] { Uid }
        };

        var handler = new CreatePostCommandHandler(_mockPostEntityRepo.Object,
            _mockLogger2.Object, _mockTagEntityRepo.Object, _mockOptionsAppSettings.Object,
            GetFakeConfiguration(), _mockBlogConfig.Object);
        var result = await handler.Handle(new(req), default);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        _mockPostEntityRepo.Verify(p => p.AddAsync(It.IsAny<PostEntity>()));
    }

    [Test]
    public void UpdateAsync_NullPost()
    {
        _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult((PostEntity)null));
        var handler = new UpdatePostCommandHandler(
            GetFakeConfiguration(),
            _mockOptionsAppSettings.Object,
            _mockTagEntityRepo.Object,
            _mockPostEntityRepo.Object,
            _mockBlogCache.Object,
            _mockBlogConfig.Object
        );

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Uid, new()), default);
        });
    }

    [Test]
    public async Task UpdateAsync_HTMLEditor_HappyPath()
    {
        _mockPostEntityRepo.Setup(p => p.GetAsync(Uid)).Returns(ValueTask.FromResult(_postEntity));
        _mockTagEntityRepo.Setup(p => p.Any(It.IsAny<Expression<Func<TagEntity, bool>>>())).Returns(false);
        _mockTagEntityRepo.Setup(p => p.AddAsync(It.IsAny<TagEntity>())).Returns(Task.FromResult(new TagEntity()));
        _mockOptionsAppSettings.Setup(p => p.Value).Returns(new AppSettings
        {
            Editor = EditorChoice.Html
        });

        var req = new PostEditModel
        {
            Title = "Work 996 and Get into ICU",
            Slug = "work-996-and-get-into-icu",
            LanguageCode = "en-us",
            EditorContent = "<p>996 is fubao</p>",
            EnableComment = true,
            FeedIncluded = true,
            IsPublished = true,
            Featured = true,
            Tags = "996,Fubao",
            SelectedCatIds = new[] { Uid }
        };

        var handler = new UpdatePostCommandHandler(
            GetFakeConfiguration(),
            _mockOptionsAppSettings.Object,
            _mockTagEntityRepo.Object,
            _mockPostEntityRepo.Object,
            _mockBlogCache.Object,
            _mockBlogConfig.Object
        );

        var result = await handler.Handle(new(Uid, req), default);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(Guid.Empty, result.Id);
        _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()));
    }

    [Test]
    public async Task RestoreAsync_NullPost()
    {
        _mockPostEntityRepo.Setup(p => p.GetAsync(Guid.Empty)).Returns(ValueTask.FromResult((PostEntity)null));

        var handler = new RestorePostCommandHandler(_mockPostEntityRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty), default);

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

        var handler = new RestorePostCommandHandler(_mockPostEntityRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(Uid), default);

        _mockPostEntityRepo.Verify(p => p.UpdateAsync(It.IsAny<PostEntity>()));
        Assert.IsFalse(post.IsDeleted);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeleteAsync_NullPost(bool softDelete)
    {
        _mockPostEntityRepo.Setup(p => p.GetAsync(Guid.Empty)).Returns(ValueTask.FromResult((PostEntity)null));

        var handler = new DeletePostCommandHandler(_mockPostEntityRepo.Object,
            _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty, softDelete), default);

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

        var handler = new DeletePostCommandHandler(_mockPostEntityRepo.Object,
            _mockBlogCache.Object);
        await handler.Handle(new(Uid, true), default);

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

        var handler = new DeletePostCommandHandler(_mockPostEntityRepo.Object,
            _mockBlogCache.Object);
        await handler.Handle(new(Uid), default);

        _mockPostEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<PostEntity>()));
        Assert.IsFalse(post.IsDeleted);
    }

    [Test]
    public async Task PurgeRecycledAsync_OK()
    {
        IReadOnlyList<PostEntity> entities = new List<PostEntity> { _postEntity };

        _mockPostEntityRepo.Setup(p => p.GetAsync(It.IsAny<ISpecification<PostEntity>>()))
            .Returns(Task.FromResult(entities));

        var handler = new PurgeRecycledCommandHandler(_mockBlogCache.Object,
            _mockPostEntityRepo.Object);
        await handler.Handle(new(), default);

        _mockPostEntityRepo.Verify(p => p.DeleteAsync(It.IsAny<IEnumerable<PostEntity>>()));
    }
}