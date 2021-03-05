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
    public class PostControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IPostService> _mockPostService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostService = _mockRepository.Create<IPostService>();
        }

        private PostController CreatePostController()
        {
            return new(_mockPostService.Object);
        }


    }
}
