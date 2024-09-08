using Microsoft.AspNetCore.Html;

namespace Moonglade.Web.Services;

public class DonationViewService(IBlogConfig config)
{
	private string BuildDonationString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(@"<table class=""tg""><tbody><tr>");
		if (config.SocialProfileSettings.BuyMeACoffee != null)
		{
			sb.Append($@"<td class=""tg-01ax""><a href=""{config.SocialProfileSettings.BuyMeACoffee}"" target=""_blank"" rel=""me""><img alt=""Donate on Buymeacoffee"" id=""buymeacoffee"" class=""ad"" src=""/images/buymeacoffee.png""</a></td>");
		}

		if (config.SocialProfileSettings.Liberapay != null)
		{
			sb.Append($@"<td class=""tg-01ax""><a href=""{config.SocialProfileSettings.Liberapay}"" target=""_blank"" rel=""me""><img alt=""Donate using Liberapay"" id=""liberapay"" class=""ad"" src=""https://liberapay.com/assets/widgets/donate.svg""></a></td>");
		}

		if (config.SocialProfileSettings.Patreon != null)
		{
			sb.Append($@"<td class=""tg-01ax""><div class=""""><a href=""{config.SocialProfileSettings.Patreon}"" rel=""me""></a><img src=""/images/patreon.svg"" id=""patreon"" class=""ad"" alt=""Donate on Patreon"" /><div></td>");
		}

		if (config.SocialProfileSettings.AmazonWishlist != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><a href=""{config.SocialProfileSettings.AmazonWishlist}"" target=""_blank"" rel=""me""><img src=""/images/amazon.svg"" id=""amazon"" class=""ad"" alt=""Show my Amazon Wishlist""/></a></td>");
		}

		if (config.SocialProfileSettings.Paypal != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><a href=""{config.SocialProfileSettings.Paypal}"" target=""_blank"" rel=""me""><img src=""/images/paypal_donate.svg"" class=""ad"" id=""paypal"" alt=""Donate on Paypal""/></a></td>");
		}

		sb.Append(@"</tr></tbody></table>");

		return sb.ToString();
	}

	public HtmlString InlineAdd()
	{
		string donations = BuildDonationString();
		return new HtmlString(donations);
	}
}
