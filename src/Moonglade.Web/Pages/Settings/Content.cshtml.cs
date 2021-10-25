using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;

namespace Moonglade.Web.Pages.Settings;

public class ContentModel : PageModel
{
    private readonly IBlogConfig _blogConfig;
    public ContentSettings ViewModel { get; set; }

    public ContentModel(IBlogConfig blogConfig)
    {
        _blogConfig = blogConfig;
    }

    public void OnGet()
    {
        ViewModel = new()
        {
            DisharmonyWords = _blogConfig.ContentSettings.DisharmonyWords,
            EnableComments = _blogConfig.ContentSettings.EnableComments,
            RequireCommentReview = _blogConfig.ContentSettings.RequireCommentReview,
            EnableWordFilter = _blogConfig.ContentSettings.EnableWordFilter,
            WordFilterMode = _blogConfig.ContentSettings.WordFilterMode,
            PostListPageSize = _blogConfig.ContentSettings.PostListPageSize,
            HotTagAmount = _blogConfig.ContentSettings.HotTagAmount,
            EnableGravatar = _blogConfig.ContentSettings.EnableGravatar,
            ShowCalloutSection = _blogConfig.ContentSettings.ShowCalloutSection,
            CalloutSectionHtmlPitch = _blogConfig.ContentSettings.CalloutSectionHtmlPitch,
            ShowPostFooter = _blogConfig.ContentSettings.ShowPostFooter,
            PostFooterHtmlPitch = _blogConfig.ContentSettings.PostFooterHtmlPitch
        };
    }
}