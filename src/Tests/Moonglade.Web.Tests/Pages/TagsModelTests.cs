using MediatR;
using Moonglade.Core.TagFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class TagsModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private TagsModel CreateTagsModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            var fakeTags = new List<KeyValuePair<Tag, int>>
            {
                new(new() { DisplayName = "Huawei", Id = 35, NormalizedName = "aiguo" }, FakeData.Int1),
                new(new() { DisplayName = "Ali", Id = 35, NormalizedName = FakeData.ShortString1 }, FakeData.Int2)
            };

            _mockMediator.Setup(p => p.Send(It.IsAny<GetTagCountListQuery>(), default))
                .Returns(Task.FromResult((IReadOnlyList<KeyValuePair<Tag, int>>)fakeTags));

            var tagsModel = CreateTagsModel();

            await tagsModel.OnGet();

            Assert.IsNotNull(tagsModel.Tags);
        }
    }
}
