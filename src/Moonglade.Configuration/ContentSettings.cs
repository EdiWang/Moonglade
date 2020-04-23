using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class ContentSettings : MoongladeSettings
    {
        public string DisharmonyWords { get; set; }

        public bool EnableComments { get; set; }

        public bool RequireCommentReview { get; set; }

        public bool EnableWordFilter { get; set; }

        public bool UseFriendlyNotFoundImage { get; set; }

        public int PostListPageSize { get; set; }

        public int HotTagAmount { get; set; }

        public bool EnableGravatar { get; set; }

        public bool ShowCalloutSection { get; set; }

        public string CalloutSectionHtmlPitch { get; set; }

        public bool ShowPostFooter { get; set; }

        public string PostFooterHtmlPitch { get; set; }

        public ContentSettings()
        {
            DisharmonyWords = string.Empty;
            EnableComments = true;
            UseFriendlyNotFoundImage = true;
            PostListPageSize = 10;
            HotTagAmount = 10;
        }
    }
}
