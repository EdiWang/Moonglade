using Microsoft.AspNetCore.Html;

namespace Moonglade.Web.Services;

public class DonationViewService
{
	private readonly Link _buyMeACoffee;
	private readonly Link _liberapay;
	private readonly Link _patreon;
	private readonly Link _amazonWishlist;
	private readonly Link _paypal;

	public DonationViewService(IBlogConfig config)
	{
		_buyMeACoffee = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://buymeacoffee.com")).FirstOrDefault();
		_liberapay = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://liberapay.com")).FirstOrDefault();
		_patreon = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://patreon.com")).FirstOrDefault();
		_amazonWishlist = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("amazon") && b.Url.Contains("wishlist")).FirstOrDefault();
		_paypal = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://paypal.com")).FirstOrDefault();
	}
	
	private string BuildDonationString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(@"<table class=""tg""><tbody><tr>");
		if (_buyMeACoffee != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_buyMeACoffee.Icon}""></i><a href=""{_buyMeACoffee.Url}"" target=""_blank"" rel=""me""></a></td>");
		}

		if (_liberapay != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_liberapay.Icon}""></i><a href=""{_liberapay.Url}"" target=""_blank"" rel=""me""></a></td>");
		}

		if (_patreon != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_patreon.Icon}""></i><a href=""{_patreon.Url}"" rel=""me"" target=""_blank""></a></td>");
		}

		if (_amazonWishlist != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><i class=""bi {_amazonWishlist.Icon}""></i><a href=""{_amazonWishlist.Url}"" target=""_blank"" rel=""me""></a></td>");
		}

		if (_paypal != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><i class=""bi {_paypal.Icon}""></i><a href=""{_paypal.Url}"" target=""_blank"" rel=""me""></a></td>");
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
