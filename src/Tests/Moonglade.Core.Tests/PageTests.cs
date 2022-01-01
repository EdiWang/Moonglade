using Moonglade.Core.PageFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Core.Tests;

[TestFixture]
public class PageTests
{
    private MockRepository _mockRepository;

    private Mock<IRepository<PageEntity>> _mockPageRepository;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockPageRepository = _mockRepository.Create<IRepository<PageEntity>>();
    }

    private readonly PageEntity _fakePageEntity = new()
    {
        CreateTimeUtc = new(996, 9, 6),
        CssContent = ".pdd .work { border: 996px solid #007; }",
        HideSidebar = true,
        HtmlContent = "<p>PDD is evil</p>",
        Id = Guid.Empty,
        IsPublished = true,
        MetaDescription = "PDD is evil",
        Slug = "pdd-IS-evil",
        Title = "PDD is Evil "
    };

    [Test]
    public async Task GetAsync_PageId()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_fakePageEntity));

        var handler = new GetPageByIdQueryHandler(_mockPageRepository.Object);
        var page = await handler.Handle(new(Guid.Empty), default);

        Assert.IsNotNull(page);
        Assert.AreEqual("PDD is Evil", page.Title);
        Assert.AreEqual("pdd-is-evil", page.Slug);
    }

    [Test]
    public async Task GetAsync_PageId_Null()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((PageEntity)null));

        var handler = new GetPageByIdQueryHandler(_mockPageRepository.Object);
        var page = await handler.Handle(new(Guid.Empty), default);

        Assert.IsNull(page);
    }

    [Test]
    public async Task GetAsync_PageSlug()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<PageEntity, bool>>>()))
            .Returns(Task.FromResult(_fakePageEntity));

        var handler = new GetPageBySlugQueryHandler(_mockPageRepository.Object);
        var page = await handler.Handle(new("pdd-is-evil"), default);

        Assert.IsNotNull(page);
        Assert.AreEqual("PDD is Evil", page.Title);
        Assert.AreEqual("pdd-is-evil", page.Slug);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetAsync_InvalidTop(int top)
    {
        var handler = new GetPagesQueryHandler(_mockPageRepository.Object);
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await handler.Handle(new(top), default);
        });
    }

    [Test]
    public async Task ListSegment_OK()
    {
        var handler = new ListPageSegmentQueryHandler(_mockPageRepository.Object);
        await handler.Handle(new(), default);

        _mockPageRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<PageEntity, PageSegment>>>()));
    }

    [Test]
    public async Task GetAsync_List()
    {
        IReadOnlyList<PageEntity> pageEntities = new List<PageEntity> { _fakePageEntity };

        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<PageSpec>()))
            .Returns(Task.FromResult(pageEntities));

        var handler = new GetPagesQueryHandler(_mockPageRepository.Object);
        var pages = await handler.Handle(new(996), default);

        Assert.IsNotNull(pages);
        Assert.AreEqual(_fakePageEntity.Title.Trim(), pages[0].Title);
    }

    [Test]
    public void RemoveScriptTagFromHtml()
    {
        var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><script>console.info('hey');</script><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
        var output = Helper.RemoveScriptTagFromHtml(html);

        Assert.IsTrue(output == @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void RemoveScriptTagFromHtml_Empty(string html)
    {
        var output = Helper.RemoveScriptTagFromHtml(html);
        Assert.AreEqual(string.Empty, output);
    }

    [Test]
    public async Task DeleteAsync_PageExists()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_fakePageEntity));
        _mockPageRepository.Setup(p => p.DeleteAsync(It.IsAny<Guid>()));

        var handler = new DeletePageCommandHandler(_mockPageRepository.Object);
        await handler.Handle(new(Guid.Empty), default);
    }

    [Test]
    public void DeleteAsync_PageNotExists()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((PageEntity)null));

        var handler = new DeletePageCommandHandler(_mockPageRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Guid.Empty), default);
        });
    }

    [Test]
    public async Task CreateAsync_OK()
    {
        var handler = new CreatePageCommandHandler(_mockPageRepository.Object);
        var result = await handler.Handle(new(new()
        {
            CssContent = string.Empty,
            HideSidebar = true,
            RawHtmlContent = "<p>Work 996</p>",
            IsPublished = true,
            MetaDescription = "Work 996",
            Slug = "work-996",
            Title = "Work 996"
        }), default);

        Assert.AreNotEqual(Guid.Empty, result);
    }

    [Test]
    public void UpdateAsync_NullPage()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((PageEntity)null));

        var handler = new UpdatePageCommandHandler(_mockPageRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await handler.Handle(new(Guid.Empty, new()), default);
        });
    }

    [Test]
    public async Task UpdateAsync_OK()
    {
        _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_fakePageEntity));

        var handler = new UpdatePageCommandHandler(_mockPageRepository.Object);
        var result = await handler.Handle(new(Guid.Empty, new()
        {
            CssContent = string.Empty,
            HideSidebar = true,
            RawHtmlContent = "<p>Work 996</p>",
            IsPublished = true,
            MetaDescription = "Work 996",
            Slug = "work-996",
            Title = "Work 996"
        }), default);

        Assert.AreEqual(Guid.Empty, result);
    }
}