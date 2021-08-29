using Moonglade.Core;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class RecycleBinModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostQueryService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostQueryService>();
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

            _mockPostService.Setup(p => p.ListSegmentAsync(It.IsAny<PostStatus>()))
                .Returns(Task.FromResult(segments));

            var recycleBinModel = CreateRecycleBinModel();
            await recycleBinModel.OnGet();

            Assert.IsNotNull(recycleBinModel.Posts);
        }
    }
}
