using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class TagServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<TagEntity>> _mockRepositoryTagEntity;
        private Mock<IRepository<PostTagEntity>> _mockRepositoryPostTagEntity;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IConfiguration> _mockConfiguration;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockRepositoryTagEntity = _mockRepository.Create<IRepository<TagEntity>>();
            _mockRepositoryPostTagEntity = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockConfiguration = _mockRepository.Create<IConfiguration>();
        }

        private TagService CreateService()
        {
            var config = @"{""TagNormalization"":{"" "": ""-""}}";
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(config)));
            var configuration = builder.Build();

            return new(
                _mockRepositoryTagEntity.Object,
                _mockBlogAudit.Object,
                configuration);
        }

        [Test]
        public void Get_OK()
        {
            _mockRepositoryTagEntity.Setup(p =>
                    p.SelectFirstOrDefault(It.IsAny<TagSpec>(), It.IsAny<Expression<Func<TagEntity, Tag>>>()))
                .Returns(new Tag()
                {
                    DisplayName = "Work 996",
                    Id = 996,
                    NormalizedName = "work-996"
                });

            var svc = CreateService();
            var result = svc.Get("work-996");

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetAll_OK()
        {
            var svc = CreateService();
            await svc.GetAll();
            _mockRepositoryTagEntity.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<TagEntity, Tag>>>()));
        }

        [Test]
        public async Task GetAllNames_OK()
        {
            var svc = CreateService();
            await svc.GetAllNames();
            _mockRepositoryTagEntity.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<TagEntity, string>>>()));
        }

        [Test]
        public async Task Create_Exists()
        {
            _mockRepositoryTagEntity.Setup(p => p.Any(It.IsAny<Expression<Func<TagEntity, bool>>>())).Returns(true);
            _mockRepositoryTagEntity.Setup(p =>
                    p.SelectFirstOrDefault(It.IsAny<TagSpec>(), It.IsAny<Expression<Func<TagEntity, Tag>>>()))
                .Returns(new Tag());

            var svc = CreateService();
            var result = await svc.Create("Work 996");

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task Create_InvalidName()
        {
            var ctl = CreateService();

            var result = await ctl.Create("ます");

            Assert.IsNull(result);
            _mockRepositoryTagEntity.Verify(p => p.AddAsync(It.IsAny<TagEntity>()), Times.Never);
        }

        [Test]
        public async Task Create_New()
        {
            _mockRepositoryTagEntity.Setup(p => p.Any(It.IsAny<Expression<Func<TagEntity, bool>>>())).Returns(false);
            _mockRepositoryTagEntity.Setup(p => p.AddAsync(It.IsAny<TagEntity>())).Returns(Task.FromResult(
                new TagEntity
                {
                    DisplayName = "Work 996",
                    Id = 996,
                    NormalizedName = "work-996"
                }));

            var svc = CreateService();
            var result = await svc.Create("Work 996");

            Assert.IsNotNull(result);
            _mockRepositoryTagEntity.Verify(p => p.AddAsync(It.IsAny<TagEntity>()));
        }

        [Test]
        public async Task UpdateAsync_Null()
        {
            _mockRepositoryTagEntity.Setup(p => p.GetAsync(It.IsAny<int>())).Returns(null);

            var svc = CreateService();
            await svc.UpdateAsync(996, "fubao");

            _mockBlogAudit.VerifyNoOtherCalls();
        }

        [Test]
        public async Task UpdateAsync_HasTag()
        {
            _mockRepositoryTagEntity.Setup(p => p.GetAsync(It.IsAny<int>()))
                .Returns(ValueTask.FromResult(new TagEntity
                {
                    Id = 996,
                    DisplayName = "Ma Yun",
                    NormalizedName = "ma-yun"
                }));

            var svc = CreateService();
            await svc.UpdateAsync(996, "fubao");

            _mockBlogAudit.Verify();
        }

        [Test]
        public async Task DeleteAsync_OK()
        {
            _mockRepositoryTagEntity.Setup(p => p.Any(It.IsAny<Expression<Func<TagEntity, bool>>>())).Returns(true);

            _mockRepositoryTagEntity.Setup(p => p.GetAsync(It.IsAny<int>()))
                .Returns(ValueTask.FromResult(new TagEntity
                {
                    Id = 996,
                    DisplayName = "Ma Yun",
                    NormalizedName = "ma-yun"
                }));

            var handler = new DeleteTagCommandHandler(_mockRepositoryTagEntity.Object,
                _mockRepositoryPostTagEntity.Object, _mockBlogAudit.Object);
            await handler.Handle(new(996), default);

            _mockRepositoryPostTagEntity.Verify(p => p.DeleteAsync(It.IsAny<IEnumerable<PostTagEntity>>()));
            _mockRepositoryTagEntity.Verify(p => p.DeleteAsync(996));
            _mockBlogAudit.Verify(p => p.AddEntry(BlogEventType.Content, BlogEventId.TagDeleted, It.IsAny<string>()));
        }

        [Test]
        public async Task GetHotTagsAsync_Empty()
        {
            _mockRepositoryTagEntity.Setup(p => p.Any((Expression<Func<TagEntity, bool>>)null)).Returns(false);

            var svc = CreateService();
            var result = await svc.GetHotTagsAsync(35);

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task GetHotTagsAsync_OK()
        {
            _mockRepositoryTagEntity.Setup(p => p.Any((Expression<Func<TagEntity, bool>>)null)).Returns(true);

            var svc = CreateService();
            await svc.GetHotTagsAsync(35);

            _mockRepositoryTagEntity.Verify(p => p.SelectAsync(It.IsAny<TagSpec>(), It.IsAny<Expression<Func<TagEntity, KeyValuePair<Tag, int>>>>()));
        }

        [Test]
        public async Task GetTagCountList_OK()
        {
            var handler = new GetTagCountListQueryHandler(_mockRepositoryTagEntity.Object);
            await handler.Handle(new(), default);

            _mockRepositoryTagEntity.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<TagEntity, KeyValuePair<Tag, int>>>>()));
        }

        [TestCase(".NET Core", ExpectedResult = "dotnet-core")]
        [TestCase("C#", ExpectedResult = "csharp")]
        [TestCase("955", ExpectedResult = "955")]
        public string NormalizeTagNameEnglish(string str)
        {
            var dic = new Dictionary<string, string>
            {
                { " ", "-" },
                { "#", "sharp" },
                { ".", "dot" }
            };

            return TagService.NormalizeTagName(str, dic);
        }

        [TestCase("福报", ExpectedResult = "8f-79-a5-62")]
        public string NormalizeTagNameNonEnglish(string str)
        {
            var dic = new Dictionary<string, string>();
            return TagService.NormalizeTagName(str, dic);
        }

        [TestCase("C", ExpectedResult = true)]
        [TestCase("C++", ExpectedResult = true)]
        [TestCase("C#", ExpectedResult = true)]
        [TestCase("Java", ExpectedResult = true)]
        [TestCase("996", ExpectedResult = true)]
        [TestCase(".NET", ExpectedResult = true)]
        [TestCase("C Sharp", ExpectedResult = true)]
        [TestCase("Cup<T>", ExpectedResult = false)]
        [TestCase("(1)", ExpectedResult = false)]
        [TestCase("usr/bin", ExpectedResult = false)]
        [TestCase("", ExpectedResult = false)]
        public bool ValidateTagName(string tagDisplayName)
        {
            return TagService.ValidateTagName(tagDisplayName);
        }
    }
}
