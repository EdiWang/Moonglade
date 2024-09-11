using Microsoft.AspNetCore.Html;

namespace Moonglade.Web.Services;

public class DonationViewService
{
	private readonly Link _buyMeACoffee;
	private readonly Link _liberapay;
	private readonly Link _patreon;
	private readonly Link _amazonWishlist;
	private readonly Link _paypal;
	private string _paypalNewTab;
	private string _liberapayNewTab;
	private string _patreonNewTab;
	private string _amazonWishlistNewTab;
	private string _buyMeACoffeeNewTab;
	private string _donationString;

	public DonationViewService(IBlogConfig config)
	{
		_buyMeACoffee = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://buymeacoffee.com")).FirstOrDefault();
		_liberapay = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://liberapay.com")).FirstOrDefault();
		_patreon = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://patreon.com")).FirstOrDefault();
		_amazonWishlist = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("amazon") && b.Url.Contains("wishlist")).FirstOrDefault();
		_paypal = config.CustomLinkSettings.Links.Where(b => b.Url.Contains("https://paypal.com")).FirstOrDefault();
		_paypalNewTab = _paypal?.IsOpenInNewTab == true ? "target=\"_blank\"" : string.Empty;
		_liberapayNewTab = _liberapay?.IsOpenInNewTab == true ? "target=\"_blank\"" : string.Empty;
		_patreonNewTab = _patreon?.IsOpenInNewTab == true ? "target=\"_blank\"" : string.Empty;
		_amazonWishlistNewTab = _amazonWishlist?.IsOpenInNewTab == true ? "target=\"_blank\"" : string.Empty;
		_buyMeACoffeeNewTab = _buyMeACoffee?.IsOpenInNewTab == true ? "target=\"_blank\"" : string.Empty;
	}
	
	private string BuildDonationString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(@"<table class=""tg""><tbody><tr>");
		if (_buyMeACoffee != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_buyMeACoffee.Icon}""></i><a href=""{_buyMeACoffee.Url}"" {_buyMeACoffeeNewTab} rel=""me""></a></td>");
		}

		if (_liberapay != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_liberapay.Icon}""></i><a href=""{_liberapay.Url}"" {_liberapayNewTab} rel=""me""></a></td>");
		}

		if (_patreon != null)
		{
			sb.Append($@"<td class=""tg-01ax""><i class=""bi {_patreon.Icon}""></i><a href=""{_patreon.Url}"" rel=""me"" {_patreonNewTab}></a></td>");
		}

		if (_amazonWishlist != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><i class=""bi {_amazonWishlist.Icon}""></i><a href=""{_amazonWishlist.Url}"" {_amazonWishlistNewTab} rel=""me""></a></td>");
		}

		if (_paypal != null)
		{
			sb.Append(
				$@"<td class=""tg-01ax""><i class=""bi {_paypal.Icon}""></i><a href=""{_paypal.Url}"" {_paypalNewTab} rel=""me""></a></td>");
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
