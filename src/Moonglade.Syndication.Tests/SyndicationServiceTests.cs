using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Syndication.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SyndicationServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AppSettings>> _mockOptions;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IRepository<CategoryEntity>> _mockRepositoryCategoryEntity;
        private Mock<IRepository<PostEntity>> _mockRepositoryPostEntity;

        readonly List<FeedEntry> _fakeFeedsFullProperties = new()
        {
            new()
            {
                Author = "Jack Ma",
                AuthorEmail = "996@ali.com",
                Categories = new[] { "fubao", "cusi" },
                Description = "Work **996** and get into ICU",
                Id = "FUBAO996",
                Link = "https://996.icu",
                PubDateUtc = new(996, 9, 9),
                Title = "996 is fubao"
            }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Loose);

            _mockOptions = _mockRepository.Create<IOptions<AppSettings>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockHttpContextAccessor = _mockRepository.Create<IHttpContextAccessor>();
            _mockRepositoryCategoryEntity = _mockRepository.Create<IRepository<CategoryEntity>>();
            _mockRepositoryPostEntity = _mockRepository.Create<IRepository<PostEntity>>();

            _mockHttpContextAccessor.Setup(p => p.HttpContext).Returns(new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new("pdd.icu", 11116),
                    Path = "/fuck-pdd"
                }
            });

            _mockBlogConfig.Setup(bc => bc.FeedSettings).Returns(new FeedSettings
            {
                RssTitle = "Fuck 996",
                AuthorName = "Dead Workers",
                RssCopyright = "(C) 2021 Gank PDD",
                RssDescription = "Die in pain",
                RssItemCount = 20
            });
        }

        private void SetupPostEntity(List<FeedEntry> entries)
        {
            _mockRepositoryPostEntity.Setup(p =>
                    p.SelectAsync(It.IsAny<ISpecification<PostEntity>>(),
                        It.IsAny<Expression<Func<PostEntity, FeedEntry>>>(), true))
                .Returns(Task.FromResult((IReadOnlyList<FeedEntry>)entries));
        }

        private SyndicationService CreateService()
        {
            return new(
                _mockOptions.Object,
                _mockBlogConfig.Object,
                _mockHttpContextAccessor.Object,
                _mockRepositoryCategoryEntity.Object,
                _mockRepositoryPostEntity.Object);
        }

        [Test]
        public async Task GetRssStreamDataAsync_NullCategory()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            var service = CreateService();

            var result = await service.GetRssStringAsync();
            Assert.IsNotNull(result);

            var xdoc = XDocument.Parse(result);
            var titles = xdoc.Descendants("title").Select(x => x.Value).ToList();

            Assert.AreEqual(2, titles.Count);
            Assert.AreEqual("996 is fubao", titles[1]);
            Assert.AreEqual("Fuck 996", titles[0]);
        }

        [Test]
        public async Task GetRssStreamDataAsync_NonExistingCategory()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            _mockRepositoryCategoryEntity.Setup(p =>
                p.GetAsync(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(Task.FromResult((CategoryEntity)null));

            var service = CreateService();

            var result = await service.GetRssStringAsync("fuckpdd");
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetRssStreamDataAsync_ExistingCategory()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            var fakeCat = new CategoryEntity
            {
                DisplayName = "PDD is shit",
                Id = Guid.NewGuid(),
                Note = "No PDD no death",
                RouteName = "fuck-pdd"
            };

            _mockRepositoryCategoryEntity.Setup(p =>
                p.GetAsync(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(Task.FromResult(fakeCat));

            var service = CreateService();

            var result = await service.GetRssStringAsync("fuckpdd");
            Assert.IsNotNull(result);

            var xdoc = XDocument.Parse(result);
            var titles = xdoc.Descendants("title").Select(x => x.Value).ToList();

            Assert.AreEqual(2, titles.Count);
            Assert.AreEqual("996 is fubao", titles[1]);
            Assert.AreEqual("Fuck 996", titles[0]);
        }

        [Test]
        public async Task GetAtomData_Success()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            var service = CreateService();

            var result = await service.GetAtomStringAsync();
            Assert.IsNotNull(result);

            var xdoc = XDocument.Parse(result);

            XNamespace ns = "http://www.w3.org/2005/Atom";
            var titles = xdoc.Descendants(ns + "title").Select(x => x.Value).ToList();
            Assert.AreEqual(2, titles.Count);
            Assert.AreEqual("996 is fubao", titles[1]);
            Assert.AreEqual("Fuck 996", titles[0]);
        }

        [Test]
        public async Task GetFeedEntriesAsync_UseFullContent_Markdown()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            _mockBlogConfig.Object.FeedSettings.UseFullContent = true;
            _mockOptions.Setup(p => p.Value).Returns(new AppSettings
            {
                Editor = EditorChoice.Markdown
            });

            var service = CreateService();
            var result = await service.GetRssStringAsync();
            Assert.IsNotNull(result);

            var xdoc = XDocument.Parse(result);
            var html = xdoc.Root.Element("channel").Element("item").Element("description").Value;

            Assert.AreEqual("<p>Work <strong>996</strong> and get into ICU</p>\n", html);
        }

        [Test]
        public async Task GetFeedEntriesAsync_UseFullContent_Html()
        {
            SetupPostEntity(_fakeFeedsFullProperties);

            _mockBlogConfig.Object.FeedSettings.UseFullContent = true;
            _mockOptions.Setup(p => p.Value).Returns(new AppSettings
            {
                Editor = EditorChoice.Html
            });

            var service = CreateService();
            var result = await service.GetRssStringAsync();
            Assert.IsNotNull(result);

            var xdoc = XDocument.Parse(result);
            var html = xdoc.Root.Element("channel").Element("item").Element("description").Value;

            Assert.AreEqual("Work **996** and get into ICU", html);
        }
    }
}
