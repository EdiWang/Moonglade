using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

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
        public void SearchAsync_EmptyTerm(string keyword)
        {
            var service = CreateService();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                var result = await service.SearchAsync(keyword);
            });
        }
    }
}
