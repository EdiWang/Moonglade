using System.Threading.Tasks;
using Moonglade.Configuration;
using Moonglade.Theme;
using Moonglade.Web.Pages.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Settings
{
    [TestFixture]
    public class GeneralModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITimeZoneResolver> _mockTZoneResolver;
        private Mock<IThemeService> _mockThemeService;


        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
            _mockThemeService = _mockRepository.Create<IThemeService>();
        }

        private GeneralModel CreateGeneralModel()
        {
            return new(
                _mockBlogConfig.Object,
                _mockTZoneResolver.Object,
                _mockThemeService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings());
            var generalModel = CreateGeneralModel();

            await generalModel.OnGetAsync();
            Assert.IsNotNull(generalModel.ViewModel);
        }
    }
}
