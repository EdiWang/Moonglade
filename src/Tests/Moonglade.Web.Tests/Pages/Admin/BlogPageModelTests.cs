using MediatR;
using Moonglade.Core;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class BlogPageModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private BlogPageModel CreatePageModel()
        {
            return new(_mockMediator.Object);
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
            _mockMediator.Setup(p => p.Send(It.IsAny<ListPageSegmentQuery>(), default)).Returns(Task.FromResult(fakePageSegments));

            var pageModel = CreatePageModel();
            await pageModel.OnGet();

            Assert.IsNotNull(pageModel.PageSegments);
        }
    }
}
