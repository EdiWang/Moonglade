using MediatR;
using Moonglade.Configuration;
using Moonglade.Theme;
using Moonglade.Web.Pages.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Settings;

[TestFixture]
public class GeneralModelTests
{
    private MockRepository _mockRepository;

    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<ITimeZoneResolver> _mockTZoneResolver;
    private Mock<IMediator> _mockMediator;


    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockTZoneResolver = _mockRepository.Create<ITimeZoneResolver>();
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private GeneralModel CreateGeneralModel()
    {
        return new(
            _mockBlogConfig.Object,
            _mockTZoneResolver.Object,
            _mockMediator.Object);
    }

    [Test]
    public async Task OnGetAsync_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<ThemeSegment> themes = new List<ThemeSegment>();
        _mockMediator.Setup(p => p.Send(It.IsAny<GetAllThemeSegmentQuery>(), default)).Returns(Task.FromResult(themes));
        _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings());
        var generalModel = CreateGeneralModel();

        await generalModel.OnGetAsync();
        Assert.IsNotNull(generalModel.ViewModel);
    }
}