using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model.Settings;
using Moonglade.Syndication;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Moonglade.Configuration;

namespace Moonglade.Tests
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

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Loose);

            _mockOptions = _mockRepository.Create<IOptions<AppSettings>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockHttpContextAccessor = _mockRepository.Create<IHttpContextAccessor>();
            _mockRepositoryCategoryEntity = _mockRepository.Create<IRepository<CategoryEntity>>();
            _mockRepositoryPostEntity = _mockRepository.Create<IRepository<PostEntity>>();
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
            _mockHttpContextAccessor.Setup(p => p.HttpContext).Returns(new DefaultHttpContext
            {
                Request =
                {
                    Scheme = "https",
                    Host = new("996.icu", 996),
                    Path = "/fuck-jack-ma"
                }
            });

            _mockBlogConfig.Setup(bc => bc.FeedSettings).Returns(new FeedSettings
            {
                RssTitle = "Fuck 996",
                AuthorName = "996 Workers",
                RssCopyright = "(C) 2021 Gank Alibaba",
                RssDescription = "Reject all your fubao",
                RssItemCount = 20
            });

            var fakeFeeds = new List<FeedEntry>
            {
                new()
                {
                    Author = "Jack Ma",
                    AuthorEmail = "996@ali.com",
                    Categories = new[] {"fubao", "cusi"},
                    Description = "Work 996 and get into ICU",
                    Id = "FUBAO996",
                    Link = "https://996.icu",
                    PubDateUtc = new(996, 9, 9),
                    Title = "996 is fubao"
                }
            };

            _mockRepositoryPostEntity.Setup(p =>
                p.SelectAsync(It.IsAny<ISpecification<PostEntity>>(),
                    It.IsAny<Expression<Func<PostEntity, FeedEntry>>>(), true))
                .Returns(Task.FromResult((IReadOnlyList<FeedEntry>)fakeFeeds));

            var service = CreateService();

            var result = await service.GetRssDataAsync();
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
                RssTitle = "Fuck PDD",
                AuthorName = "Dead Workers",
                RssCopyright = "(C) 2021 Gank PDD",
                RssDescription = "Die in pain",
                RssItemCount = 20
            });

            _mockRepositoryCategoryEntity.Setup(p =>
                p.GetAsync(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(Task.FromResult((CategoryEntity) null));

            var service = CreateService();

            var result = await service.GetRssDataAsync("fuckpdd");
            Assert.IsNull(result);
        }
    }
}
