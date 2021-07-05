namespace Moonglade.Configuration
{
    public class ImageSettings : IBlogSettings
    {
        public bool IsWatermarkEnabled { get; set; }
        public bool KeepOriginImage { get; set; }
        public int WatermarkFontSize { get; set; }
        public string WatermarkText { get; set; }
        public bool UseFriendlyNotFoundImage { get; set; }
        public bool FitImageToDevicePixelRatio { get; set; }
    }
}