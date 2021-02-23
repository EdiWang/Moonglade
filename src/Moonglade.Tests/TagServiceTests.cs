using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Data.Spec;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<TagEntity>> _mockRepositoryTagEntity;
        private Mock<IRepository<PostTagEntity>> _mockRepositoryPostTagEntity;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IOptions<List<TagNormalization>>> _mockOptions;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockRepositoryTagEntity = _mockRepository.Create<IRepository<TagEntity>>();
            _mockRepositoryPostTagEntity = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockOptions = _mockRepository.Create<IOptions<List<TagNormalization>>>();
        }

        private TagService CreateService()
        {
            return new(
                _mockRepositoryTagEntity.Object,
                _mockRepositoryPostTagEntity.Object,
                _mockBlogAudit.Object,
                _mockOptions.Object);
        }

        [Test]
        public void Get_OK()
        {
            _mockRepositoryTagEntity.Setup(p =>
                    p.SelectFirstOrDefault(It.IsAny<TagSpec>(), It.IsAny<Expression<Func<TagEntity, Tag>>>(), true))
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

        [TestCase(".NET Core", ExpectedResult = "dotnet-core")]
        [TestCase("C#", ExpectedResult = "csharp")]
        [TestCase("955", ExpectedResult = "955")]
        public string NormalizeTagNameEnglish(string str)
        {
            var dic = new TagNormalization[]
            {
                new() { Source = " ", Target = "-" },
                new() { Source = "#", Target = "sharp" },
                new() { Source = ".", Target = "dot" }
            };

            return TagService.NormalizeTagName(str, dic);
        }

        [TestCase("福报", ExpectedResult = "8f-79-a5-62")]
        public string NormalizeTagNameNonEnglish(string str)
        {
            var dic = Array.Empty<TagNormalization>();
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
