using MediatR;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Web.Handlers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Handlers;

[TestFixture]
public class SaveAssetToCdnHandlerTests
{
    private MockRepository _mockRepository;
    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<IMediator> _mockMediator;
    private Mock<IBlogImageStorage> _mockBlogImageStorage;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockBlogImageStorage = _mockRepository.Create<IBlogImageStorage>();
    }

    [Test]
    public async Task TerminateWrongAssetId_OK()
    {
        var assetId = AssetId.SiteIconBase64;

        var handler =
            new SaveAssetToCdnHandler(_mockBlogImageStorage.Object, _mockBlogConfig.Object, _mockMediator.Object);

        await handler.Handle(new(assetId, string.Empty), CancellationToken.None);

        _mockBlogImageStorage.Verify(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task NoCDNRedirect_OK()
    {
        var assetId = AssetId.AvatarBase64;

        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings { EnableCDNRedirect = false });

        var handler =
            new SaveAssetToCdnHandler(_mockBlogImageStorage.Object, _mockBlogConfig.Object, _mockMediator.Object);

        await handler.Handle(new(assetId, string.Empty), CancellationToken.None);

        _mockBlogImageStorage.Verify(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Test]
    public async Task HasCDNRedirect_OK()
    {
        var assetId = AssetId.AvatarBase64;

        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings
        {
            EnableCDNRedirect = true,
            CDNEndpoint = "https://996.icu/images"
        });

        _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
        {
            AvatarUrl = "https://251.today/images/251.jpg"
        });

        _mockBlogImageStorage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(Task.FromResult("996.jpg"));

        var handler =
            new SaveAssetToCdnHandler(_mockBlogImageStorage.Object, _mockBlogConfig.Object, _mockMediator.Object);

        await handler.Handle(new(assetId, FakeData.ImageBase64), CancellationToken.None);

        _mockBlogImageStorage.Verify(p => p.DeleteAsync(It.IsAny<string>()));
        _mockBlogImageStorage.Verify(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()));
        _mockBlogConfig.Verify(p => p.UpdateAsync(It.IsAny<GeneralSettings>()));
        _mockMediator.Verify(p => p.Send(It.IsAny<UpdateConfigurationCommand>(), default));
    }
}