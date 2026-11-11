using QRCoder;

namespace Moonglade.Web.Services;

internal static class AuthenticatorQrCode
{
    public static string Create(string uri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var image = new PngByteQRCode(data);
        return $"data:image/png;base64,{Convert.ToBase64String(image.GetGraphic(8))}";
    }
}
