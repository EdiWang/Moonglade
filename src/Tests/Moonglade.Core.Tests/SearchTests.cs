using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class SearchTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void SearchAsync_EmptyTerm(string keyword)
        {
            var handler = new SearchPostQueryHandler(_mockPostEntityRepo.Object);

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var result = await handler.Handle(It.IsAny<SearchPostQuery>(), default);
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
