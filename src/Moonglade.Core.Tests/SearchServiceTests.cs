using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SearchServiceTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
        }

        private SearchService CreateService()
        {
            return new(_mockPostEntityRepo.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SearchAsync_EmptyTerm(string keyword)
        {
            var service = CreateService();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var result = await service.SearchAsync(keyword);
            });
        }

        //[Test]
        //public async Task SearchAsync_HappyPath()
        //{
        //    var entities = new List<PostEntity>();
        //    IReadOnlyList<PostDigest> digests = new List<PostDigest>();

        //    var postQuery = new TestAsyncEnumerable<PostEntity>(entities).AsQueryable();
        //    _mockPostEntityRepo.Setup(p => p.GetAsQueryable()).Returns(postQuery);
        //    _mockPostEntityRepo.Setup(p => p.Select(It.IsAny<Expression<Func<PostEntity, PostDigest>>>(), true))
        //        .Returns(digests);

        //    var service = CreateService();
        //    var result = await service.SearchAsync("996");

        //    Assert.IsNotNull(result);
        //}
    }
}
