using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Core;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagsModelTests
    {
        private MockRepository _mockRepository;

        private Mock<ITagService> _mockTagService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _mockTagService = _mockRepository.Create<ITagService>();
        }

        private TagsModel CreateTagsModel()
        {
            return new(_mockTagService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Tag> tags = new List<Tag>
            {
                new() { Id = 996, DisplayName = "Work 996", NormalizedName = "work-996" }
            };
            _mockTagService.Setup(p => p.GetAll()).Returns(Task.FromResult(tags));

            var tagsModel = CreateTagsModel();
            await tagsModel.OnGet();

            Assert.IsNotNull(tagsModel.Tags);
        }
    }
}
