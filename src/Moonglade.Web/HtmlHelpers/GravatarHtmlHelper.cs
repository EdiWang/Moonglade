using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        /// In addition to allowing you to use your own image, Gravatar has a number of built in options which you can also use as defaults. Most of these work by taking the requested email hash and using it to generate a themed image that is unique to that email address
        /// </summary>
        public enum DefaultImage
        {
            /// <summary>Default Gravatar logo</summary>
            [Description("")]
            Default,
            /// <summary>404 - do not load any image if none is associated with the email hash, instead return an HTTP 404 (File Not Found) response</summary>
            [Description("404")]
            Http404,
            /// <summary>Mystery-Man - a simple, cartoon-style silhouetted outline of a person (does not vary by email hash)</summary>
            [Description("mm")]
            MysteryMan,
            /// <summary>Identicon - a geometric pattern based on an email hash</summary>
            [Description("identicon")]
            Identicon,
            /// <summary>MonsterId - a generated 'monster' with different colors, faces, etc</summary>
            [Description("monsterid")]
            MonsterId,
            /// <summary>Wavatar - generated faces with differing features and backgrounds</summary>
            [Description("wavatar")]
            Wavatar,
            /// <summary>Retro - awesome generated, 8-bit arcade-style pixelated faces</summary>
            [Description("retro")]
            Retro
        }

        /// <summary>
        /// Gravatar allows users to self-rate their images so that they can indicate if an image is appropriate for a certain audience. By default, only 'G' rated images are displayed unless you indicate that you would like to see higher ratings
        /// </summary>
        public enum Rating
        {
            /// <summary>Suitable for display on all websites with any audience type</summary>
            [Description("g")]
            G,
            /// <summary>May contain rude gestures, provocatively dressed individuals, the lesser swear words, or mild violence</summary>
            [Description("pg")]
            PG,
            /// <summary>May contain such things as harsh profanity, intense violence, nudity, or hard drug use</summary>
            [Description("r")]
            R,
            /// <summary>May contain hardcore sexual imagery or extremely disturbing violence</summary>
            [Description("x")]
            X
        }

        /// <summary>
        /// Returns a Globally Recognised Avatar as an &lt;img /&gt; - http://gravatar.com
        /// </summary>
        /// <param name="htmlHelper">IHtmlHelper</param>
        /// <param name="emailAddress">Email Address for the Gravatar</param>
        /// <param name="defaultImage">Default image if user hasn't created a Gravatar</param>
        /// <param name="size">Size in pixels (default: 80)</param>
        /// <param name="defaultImageUrl">URL to a custom default image (e.g: 'Url.Content("~/images/no-grvatar.png")' )</param>
        /// <param name="forceDefaultImage">Prefer the default image over the users own Gravatar</param>
        /// <param name="rating">Gravatar content rating (note that Gravatars are self-rated)</param>
        /// <param name="forceSecureRequest">Always do secure (https) requests</param>
        /// <param name="cssClass">CSS Class</param>
        /// <param name="alt">Alt Text</param>
        public static IHtmlContent GravatarImage(
            this IHtmlHelper htmlHelper,
            string emailAddress,
            int size = 80,
            DefaultImage defaultImage = DefaultImage.Default,
            string defaultImageUrl = "",
            bool forceDefaultImage = false,
            Rating rating = Rating.G,
            bool forceSecureRequest = false,
            string cssClass = "gravatar",
            string alt = "Gravatar image")
        {

            var imgTag = new TagBuilder("img");

            emailAddress = string.IsNullOrEmpty(emailAddress) ? string.Empty : emailAddress.Trim().ToLower();

            imgTag.Attributes.Add("src",
                string.Format("{0}://{1}.gravatar.com/avatar/{2}?s={3}{4}{5}{6}",
                    htmlHelper.ViewContext.HttpContext.Request.IsHttps || forceSecureRequest ? "https" : "http",
                    htmlHelper.ViewContext.HttpContext.Request.IsHttps || forceSecureRequest ? "secure" : "www",
                    GetMd5Hash(emailAddress),
                    size.ToString(),
                    "&d=" + (!string.IsNullOrEmpty(defaultImageUrl) ? htmlHelper.UrlEncoder.Encode(defaultImageUrl) : defaultImage.GetDescription()),
                    forceDefaultImage ? "&f=y" : "",
                    "&r=" + rating.GetDescription()
                )
            );

            imgTag.Attributes.Add("class", cssClass);
            imgTag.Attributes.Add("alt", alt);
            imgTag.TagRenderMode = TagRenderMode.SelfClosing;

            return imgTag;
        }

        /// <summary>
        /// Generates an MD5 hash of the given string
        /// </summary>
        /// <remarks>Source: http://msdn.microsoft.com/en-us/library/system.security.cryptography.md5.aspx </remarks>
        private static string GetMd5Hash(string input)
        {
            // Convert the input string to a byte array and compute the hash.
            var data = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            foreach (var t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        /// <summary>
        /// Returns the value of a DescriptionAttribute for a given Enum value
        /// </summary>
        /// <remarks>Source: http://blogs.msdn.com/b/abhinaba/archive/2005/10/21/483337.aspx </remarks>
        /// <param name="en"></param>
        /// <returns></returns>
        private static string GetDescription(this Enum en)
        {
            var type = en.GetType();
            var memInfo = type.GetMember(en.ToString());

            if (memInfo.Length <= 0) return en.ToString();
            var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attrs.Any() ? ((DescriptionAttribute)attrs.First()).Description : en.ToString();
        }
    }
}
