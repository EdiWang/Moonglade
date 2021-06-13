using Moq;
using NUnit.Framework;

namespace Moonglade.Data.Tests
{
    [TestFixture]
    public class BlogDbContextTests
    {
        private MockRepository _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
        }

        private BlogDbContext CreateBlogDbContext()
        {
            return new();
        }

        [Test]
        public void CreateBlogDbContext_OK()
        {
            var blogDbContext = CreateBlogDbContext();
            Assert.IsNotNull(blogDbContext);
        }
    }
}
