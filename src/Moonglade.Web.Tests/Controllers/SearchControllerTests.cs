using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SearchControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<ISearchService> _mockSearchService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockSearchService = _mockRepository.Create<ISearchService>();
        }

        private SearchController CreateSearchController()
        {
            return new(_mockSearchService.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Post_EmptyTerm(string term)
        {
            var searchController = CreateSearchController();
            var result = searchController.Post(term);

            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Home", redirectResult.ControllerName);
            Assert.AreEqual("Index", redirectResult.ActionName);
        }

        [Test]
        public void Post_HasTerm()
        {
            var searchController = CreateSearchController();
            var result = searchController.Post("Fuck 996");

            Assert.IsInstanceOf<RedirectToActionResult>(result);

            var redirectResult = (RedirectToActionResult)result;
            Assert.AreEqual("Search", redirectResult.ActionName);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task Search_EmptyTerm(string term)
        {
            var searchController = CreateSearchController();
            var result = await searchController.Search(term);

            Assert.IsInstanceOf<RedirectToActionResult>(result);
        }

        [Test]
        public async Task Search_ValidTerm()
        {
            var fakePosts = new List<PostDigest>
            {
                new()
                {
                    Title = "Work 996 and get into ICU",
                    ContentAbstract = "This is Jack Ma's fubao",
                    LangCode = "en-us",
                    PubDateUtc = new(996, 9, 6),
                    Slug = "fuck-jack-ma",
                    Tags = new Tag[] {new()
                    {
                        DisplayName = "Fubao", NormalizedName = "fubao", Id = 996
                    }}
                }
            };

            _mockSearchService.Setup(p => p.SearchAsync(It.IsAny<string>()))
                .Returns(Task.FromResult((IReadOnlyList<PostDigest>)fakePosts));

            var searchController = CreateSearchController();
            var result = await searchController.Search("996");

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.AreEqual(fakePosts, ((ViewResult)result).Model);
            Assert.AreEqual("996", ((ViewResult)result).ViewData["TitlePrefix"]);
        }
    }
}
