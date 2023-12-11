using System.Globalization;

using Microsoft.AspNetCore.Html;

using Moonglade.Web.Services.Models;

namespace Moonglade.Web.Services
{
	/// <summary>
	/// Ad Service.
	/// This part you can modify and use your own stuff.
	/// </summary>
	public class DonationService
	{
		private readonly IBlogConfig _config;

		/// <summary>
		/// Initializes a new instance of the <see cref="DonationService"/> class.
		/// </summary>
		public DonationService(IBlogConfig config)
		{
			_config = config;
		}

		private string BuildDonationString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(@"<table class=""tg""><tbody><tr>");
			if (_config.SocialProfileSettings.BuyMeACoffee != null)
			{
				sb.Append($@"<td class=""tg-01ax""><a href=""{_config.SocialProfileSettings.BuyMeACoffee}"" target=""_blank"" rel=""me""><img alt=""Donate on Buymeacoffee"" id=""buymeacoffee"" class=""ad"" src=""/images/buymeacoffee.png""</a></td>");
			}

			if (_config.SocialProfileSettings.Liberapay != null)
			{
				sb.Append($@"<td class=""tg-01ax""><a href=""{_config.SocialProfileSettings.Liberapay}"" target=""_blank"" rel=""me""><img alt=""Donate using Liberapay"" id=""liberapay"" class=""ad"" src=""https://liberapay.com/assets/widgets/donate.svg""></a></td>");
			}

			if (_config.SocialProfileSettings.Patreon != null)
			{
				sb.Append($@"<td class=""tg-01ax""><div class=""""><a href=""{_config.SocialProfileSettings.Patreon}"" rel=""me""></a><img src=""/images/patreon.svg"" id=""patreon"" class=""ad"" alt=""Donate on Patreon"" /><div></td>");
			}

			if (_config.SocialProfileSettings.AmazonWishlist != null)
			{
				sb.Append(
					$@"<td class=""tg-01ax""><a href=""{_config.SocialProfileSettings.AmazonWishlist}"" target=""_blank"" rel=""me""><img src=""/images/amazon.svg"" id=""amazon"" class=""ad"" alt=""Show my Amazon Wishlist""/></a></td>");
			}

			if (_config.SocialProfileSettings.Paypal != null)
			{
				sb.Append(
					$@"<td class=""tg-01ax""><a href=""{_config.SocialProfileSettings.Paypal}"" target=""_blank"" rel=""me""><img src=""/images/paypal_donate.svg"" class=""ad"" id=""paypal"" alt=""Donate on Paypal""/></a></td>");
			}

			sb.Append(@"</tr></tbody></table>");

			return sb.ToString();
		}

		/// <summary>
		/// Ads for inline.
		/// </summary>
		/// <returns>Ad HTML-String.</returns>
		public HtmlString InlineAdd()
		{
			string ad = BuildDonationString();
			var ranges = new List<AdDateRange>()
			{
				new AdDateRange( // Fallback
                DateTime.MinValue.ToString(CultureInfo.CurrentCulture),
				DateTime.MaxValue.ToString(CultureInfo.CurrentCulture),
				ad),
			};
			var now = DateTime.Now;
			var ads = ranges.FirstOrDefault(r => r.Start <= now && r.End >= now);

			if (ads == null)
			{
				return HtmlString.Empty;
			}

			var item = new Random().Next(0, ads.Ads.Length);

			return new HtmlString(ads.Ads[item]);
		}

		/// <summary>
		/// Sidebars the add.
		/// </summary>
		/// <returns>Ad HTML String.</returns>
		public HtmlString SidebarAdd()
		{
			string ad = BuildDonationString();
			var ranges = new List<AdDateRange>()
			{
				new AdDateRange( // Fallback
                DateTime.MinValue.ToString(CultureInfo.CurrentCulture),
				DateTime.MaxValue.ToString(CultureInfo.CurrentCulture),
				ad),
			};
			var now = DateTime.Now;
			var ads = ranges.FirstOrDefault(r => r.Start <= now && r.End >= now);

			if (ads == null)
			{
				return HtmlString.Empty;
			}

			var item = new Random().Next(0, ads.Ads.Length);

			return new HtmlString(ads.Ads[item]);
		}
	}
}
