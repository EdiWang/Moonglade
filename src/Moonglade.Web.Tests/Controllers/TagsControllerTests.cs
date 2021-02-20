using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagsControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<ITagService> _mockTagService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockTagService = _mockRepository.Create<ITagService>();
        }

        private TagsController CreateTagsController()
        {
            return new(_mockTagService.Object);
        }

    }
}
