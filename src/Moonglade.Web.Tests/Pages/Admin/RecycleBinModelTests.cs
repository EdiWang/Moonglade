using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Core;
using Moonglade.Data.Spec;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class RecycleBinModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostService>();
        }

        private RecycleBinModel CreateRecycleBinModel()
        {
            return new(
                _mockPostService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PostSegment> segments = new List<PostSegment>();

            _mockPostService.Setup(p => p.ListSegment(It.IsAny<PostStatus>()))
                .Returns(Task.FromResult(segments));

            var recycleBinModel = CreateRecycleBinModel();
            await recycleBinModel.OnGet();

            Assert.IsNotNull(recycleBinModel.Posts);
        }
    }
}
