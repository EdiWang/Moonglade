using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Page.Tests
{
    [TestFixture]
    public class PageServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<PageEntity>> _mockPageRepository;
        private Mock<IBlogAudit> _mockBlogAudit;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockPageRepository = _mockRepository.Create<IRepository<PageEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
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

        private BlogPageService CreatePageService()
        {
            return new(
                _mockPageRepository.Object,
                _mockBlogAudit.Object);
        }

        [Test]
        public async Task GetAsync_PageId()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_fakePageEntity));

            var svc = CreatePageService();
            var page = await svc.GetAsync(Guid.Empty);

            Assert.IsNotNull(page);
            Assert.AreEqual("PDD is Evil", page.Title);
            Assert.AreEqual("pdd-is-evil", page.Slug);
        }

        [Test]
        public async Task GetAsync_PageId_Null()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((PageEntity)null));

            var svc = CreatePageService();
            var page = await svc.GetAsync(Guid.Empty);

            Assert.IsNull(page);
        }

        [Test]
        public async Task GetAsync_PageSlug()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<PageEntity, bool>>>()))
                .Returns(Task.FromResult(_fakePageEntity));

            var svc = CreatePageService();
            var page = await svc.GetAsync("pdd-is-evil");

            Assert.IsNotNull(page);
            Assert.AreEqual("PDD is Evil", page.Title);
            Assert.AreEqual("pdd-is-evil", page.Slug);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetAsync_InvalidTop(int top)
        {
            var svc = CreatePageService();
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.GetAsync(top);
            });
        }

        [Test]
        public async Task ListSegment_OK()
        {
            var svc = CreatePageService();
            await svc.ListSegment();

            _mockPageRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<PageEntity, PageSegment>>>()));
        }

        [Test]
        public async Task GetAsync_List()
        {
            IReadOnlyList<PageEntity> pageEntities = new List<PageEntity> { _fakePageEntity };

            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<PageSpec>()))
                .Returns(Task.FromResult(pageEntities));

            var svc = CreatePageService();
            var pages = await svc.GetAsync(996);

            Assert.IsNotNull(pages);
            Assert.AreEqual(_fakePageEntity.Title.Trim(), pages[0].Title);
        }

        [Test]
        public void RemoveScriptTagFromHtml()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><script>console.info('hey');</script><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = BlogPageService.RemoveScriptTagFromHtml(html);

            Assert.IsTrue(output == @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void RemoveScriptTagFromHtml_Empty(string html)
        {
            var output = BlogPageService.RemoveScriptTagFromHtml(html);
            Assert.AreEqual(string.Empty, output);
        }

        [Test]
        public async Task DeleteAsync_PageExists()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_fakePageEntity));
            _mockPageRepository.Setup(p => p.DeleteAsync(It.IsAny<Guid>()));

            var svc = CreatePageService();
            await svc.DeleteAsync(Guid.Empty);

            _mockBlogAudit.Verify();
        }

        [Test]
        public void DeleteAsync_PageNotExists()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((PageEntity)null));

            var svc = CreatePageService();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await svc.DeleteAsync(Guid.Empty);
            });
        }

        [Test]
        public async Task CreateAsync_OK()
        {
            var svc = CreatePageService();
            var result = await svc.CreateAsync(new()
            {
                CssContent = string.Empty,
                HideSidebar = true,
                HtmlContent = "<p>Work 996</p>",
                IsPublished = true,
                MetaDescription = "Work 996",
                Slug = "work-996",
                Title = "Work 996"
            });

            Assert.AreNotEqual(Guid.Empty, result);
            _mockBlogAudit.Verify(p => p.AddAuditEntry(BlogEventType.Content, BlogEventId.PageCreated, It.IsAny<string>()));
        }

        [Test]
        public void UpdateAsync_NullPage()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((PageEntity)null));

            var svc = CreatePageService();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                var result = await svc.UpdateAsync(Guid.Empty, new());
            });
        }

        [Test]
        public async Task UpdateAsync_OK()
        {
            _mockPageRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_fakePageEntity));

            var svc = CreatePageService();
            var result = await svc.UpdateAsync(Guid.Empty, new()
            {
                CssContent = string.Empty,
                HideSidebar = true,
                HtmlContent = "<p>Work 996</p>",
                IsPublished = true,
                MetaDescription = "Work 996",
                Slug = "work-996",
                Title = "Work 996"
            });

            Assert.AreEqual(Guid.Empty, result);
            _mockBlogAudit.Verify(p => p.AddAuditEntry(BlogEventType.Content, BlogEventId.PageUpdated, It.IsAny<string>()));
        }
    }
}
