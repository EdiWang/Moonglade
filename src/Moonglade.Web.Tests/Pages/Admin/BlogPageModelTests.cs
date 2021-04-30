using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Pages;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class BlogPageModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogPageService> _mockPageService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPageService = _mockRepository.Create<IBlogPageService>();
        }

        private BlogPageModel CreatePageModel()
        {
            return new(_mockPageService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<PageSegment> fakePageSegments = new List<PageSegment>
            {
                new ()
                {
                    IsPublished = true,
                    CreateTimeUtc = DateTime.UtcNow,
                    Id = Guid.Empty,
                    Slug = "fuck-996",
                    Title = "Fuck Jack Ma's Fu Bao"
                }
            };
            _mockPageService.Setup(p => p.ListSegment()).Returns(Task.FromResult(fakePageSegments));

            var pageModel = CreatePageModel();
            await pageModel.OnGet();

            Assert.IsNotNull(pageModel.PageSegments);
        }
    }
}
