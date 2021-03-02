using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Moonglade.Web.HtmlHelpers
{
    /// <summary>
    /// Globally Recognised Avatar - http://gravatar.com
    /// </summary>
    /// <remarks>
    /// This implementation by Andrew Freemantle - http://www.fatlemon.co.uk/
    /// <para>Source, Wiki and Issues: https://github.com/AndrewFreemantle/Gravatar-HtmlHelper </para>
    /// </remarks>
    public static class GravatarHtmlHelper
    {
        /// <summary>
        /// Returns a Globally Recognised Avatar as an &lt;img /&gt; - http://gravatar.com
        /// </summary>
        /// <param name="htmlHelper">IHtmlHelper</param>
        /// <param name="emailHash">Email Address for the Gravatar</param>
        /// <param name="size">Size in pixels (default: 80)</param>
        /// <param name="defaultImageUrl">URL to a custom default image (e.g: 'Url.Content("~/images/no-grvatar.png")' )</param>
        /// <param name="forceDefaultImage">Prefer the default image over the users own Gravatar</param>
        /// <param name="forceSecureRequest">Always do secure (https) requests</param>
        /// <param name="cssClass">CSS Class</param>
        /// <param name="alt">Alt Text</param>
        public static IHtmlContent GravatarImage(
            this IHtmlHelper htmlHelper,
            string emailHash,
            int size = 58,
            string defaultImageUrl = "",
            bool forceDefaultImage = false,
            bool forceSecureRequest = false,
            string cssClass = "gravatar",
            string alt = "Gravatar image")
        {
            var imgTag = new TagBuilder("img");
            emailHash = string.IsNullOrEmpty(emailHash) ? string.Empty : emailHash.Trim().ToLower();

            imgTag.Attributes.Add("src",
                string.Format("{0}://{1}.gravatar.com/avatar/{2}?s={3}{4}{5}{6}",
                    htmlHelper.ViewContext.HttpContext.Request.IsHttps || forceSecureRequest ? "https" : "http",
                    htmlHelper.ViewContext.HttpContext.Request.IsHttps || forceSecureRequest ? "secure" : "www",
                    emailHash,
                    size.ToString(),
                    "&d=" + (!string.IsNullOrEmpty(defaultImageUrl) ? htmlHelper.UrlEncoder.Encode(defaultImageUrl) : string.Empty),
                    forceDefaultImage ? "&f=y" : string.Empty, "&r=g"
                )
            );

            imgTag.Attributes.Add("class", cssClass);
            imgTag.Attributes.Add("alt", alt);
            imgTag.TagRenderMode = TagRenderMode.SelfClosing;

            return imgTag;
        }
    }
}
