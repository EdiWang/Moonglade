using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class SocialProfileSettings : IBlogSettings
{
    /// <summary>
    /// Gets or sets the GPG key URL.
    /// </summary>
    /// <value>
    /// The GPG key URL.
    /// </value>
    [Display(Name = "Url to your public GPG key")]
    public string GPGKeyUrl { get; set; }

    /// <summary>
    /// Gets or sets the twitter profile link.
    /// </summary>
    /// <value>
    /// The twitter link.
    /// </value>
    [Display(Name = "Twitter Profile Link")]
    public string Twitter { get; set; }

    /// <summary>
    /// Gets or sets the mastodon profile link..
    /// </summary>
    /// <value>
    /// The mastodon url.
    /// </value>
    [Display(Name = "Mastodon Profile Link")]
    public string Mastodon { get; set; }

    /// <summary>
    /// Gets or sets the facebook profile url.
    /// </summary>
    /// <value>
    /// The facebook profile url.
    /// </value>
    [Display(Name = "Facebook Profile Link")]
    public string Facebook { get; set; }

    /// <summary>
    /// Gets or sets the instagram profile url.
    /// </summary>
    /// <value>
    /// The instagram profile url.
    /// </value> 
    [Display(Name = "Instagram Profile Link")]
    public string Instagram { get; set; }

    /// <summary>
    /// Gets or sets the linkedIn profile url.
    /// </summary>
    /// <value>
    /// The linkedIn profile url.
    /// </value>
    [Display(Name = "Linkedin Profile Link")]
    public string LinkedIn { get; set; }

    /// <summary>
    /// Gets or sets YouTube profile url.
    /// </summary>
    /// <value>
    /// You tube profile url.
    /// </value>
    [Display(Name = "Youtube Profile Link")]
    public string YouTube { get; set; }

    /// <summary>
    /// Gets or sets the GitHub profile url.
    /// </summary>
    /// <value>
    /// The GitHub profile url.
    /// </value>
    [Display(Name = "Github Profile Link")]
    public string GitHub { get; set; }

    /// <summary>
    /// Gets or sets the xing profile url.
    /// </summary>
    /// <value>
    /// The xing profile url.
    /// </value>
    [Display(Name = "Xing Profile Link")]
    public string Xing { get; set; }

    /// <summary>
    /// Gets or sets the Pinterest profile url.
    /// </summary>
    /// <value>
    /// The pinterest profile url.
    /// </value>
    [Display(Name = "Pinterest Profile Link")]
    public string Pinterest { get; set; }

    /// <summary>
    /// Gets or sets the Reddit profile url.
    /// </summary>
    /// <value>
    /// The reddit profile url.
    /// </value>
    [Display(Name = "Reddit Profile Link")]
    public string Reddit { get; set; }

    /// <summary>
    /// Gets or sets the Vimeo profile url.
    /// </summary>
    /// <value>
    /// The Vimeo profile url.
    /// </value>
    [Display(Name = "Vimeo Profile Link")]
    public string Vimeo { get; set; }

    /// <summary>
    /// Gets or sets the Behance profile url.
    /// </summary>
    /// <value>
    /// The Behance profile url.
    /// </value>
    [Display(Name = "Behance Profile Link")]
    public string Behance { get; set; }

    /// <summary>
    /// Gets or sets the Spotify profile url.
    /// </summary>
    /// <value>
    /// The Spotify profile url.
    /// </value>
    [Display(Name = "Spotify Profile Link")]
    public string Spotify { get; set; }

    /// <summary>
    /// Gets or sets the Twitch profile url.
    /// </summary>
    /// <value>
    /// The Twitch profile url.
    /// </value>
    [Display(Name = "Twitch Profile Link")]
    public string Twitch { get; set; }

    /// <summary>
    /// Gets or sets the WhatsAPp profile page.
    /// </summary>
    /// <value>
    /// The profile Url Like: https://wa.me/YourNumber .
    /// </value>.
    /// </value>
    [Display(Name = "WhatsApp Profile Link")]
    public string WhatsApp { get; set; }

    /// <summary>
    /// Gets or sets the Skype profile page.
    /// </summary>
    /// <value>
    /// The Skype profile page.
    /// </value>
    [Display(Name = "Skype Profile Link")]
    public string Skype { get; set; }

    /// <summary>
    /// Gets or sets the Discord profile page.
    /// </summary>
    /// <value>
    /// The Discord profile page.
    /// </value>
    [Display(Name = "Discord Profile Link")]
    public string Discord { get; set; }

    /// <summary>
    /// Gets or sets the Steam profile page.
    /// </summary>
    /// <value>
    /// The Steam profile page.
    /// </value>
    [Display(Name = "Steam Profile Link")]
    public string Steam { get; set; }

    /// <summary>
    /// Gets or sets the Stackoverflow profile page.
    /// </summary>
    /// <value>
    /// The Stackoverflow profile page.
    /// </value>
    [Display(Name = "Stackoverflow Profile Link")]
    public string Stackoverflow { get; set; }

    /// <summary>
    /// Gets or sets the DevTo profile page.
    /// </summary>
    /// <value>
    /// The DevTo profile page.
    /// </value>
    [Display(Name = "DevTo Profile Link")]
    public string DevTo { get; set; }

    /// <summary>
    /// Gets or sets the Codersrank profile page.
    /// </summary>
    /// <value>
    /// The Codersrank profile page.
    /// </value>
    [Display(Name = "Codersrank Profile Link")]
    public string Codersrank { get; set; }

    /// <summary>
    /// Gets or sets the amazon author page.  If you published a book, this is your author page on Amazon.
    /// </summary>
    /// <value>
    /// The amazon author page.
    /// </value>
    [Display(Name = "AmazonAuthor Profile Link")]
    public string AmazonAuthorPage { get; set; }

    /// <summary>
    /// Gets or sets the Last.fm profile page.
    /// </summary>
    /// <value>
    /// The last.fm profile page.
    /// </value>
    [Display(Name = "LastFM Profile Link")]
    public string LastFm { get; set; }

    /// <summary>
    /// Gets or sets the CodeProject profile page.
    /// </summary>
    /// <value>
    /// The CodeProject profile page.
    /// </value>
    [Display(Name = "Codeproject Profile Link")]
    public string CodeProject { get; set; }

    /// <summary>
    /// Gets or sets the Matrix profile page.
    /// </summary>
    /// <value>
    /// The Matrix profile page.
    /// </value>
    [Display(Name = "Matrix Profile Link")]
    public string Matrix { get; set; }

    /// <summary>
    /// Gets or sets the OpenHub profile page.
    /// </summary>
    /// <value>
    /// The OpenHub profile page.
    /// </value>
    [Display(Name = "OpenHub Profile Link")]
    public string OpenHub { get; set; }

    /// <summary>
    /// Gets or sets the CodeStats profile page.
    /// </summary>
    /// <value>
    /// The CodeStats profile page.
    /// </value>
    [Display(Name = "CodeStats Profile Link")]
    public string CodeStats { get; set; }

    /// <summary>
    /// Gets or sets the KeyBase profile page.
    /// </summary>
    [Display(Name = "KeyBase Profile link")]
    public string KeyBase { get; set; }

    // Donations
    /// <summary>
    /// Gets or sets the buy me a coffee.
    /// </summary>
    /// <value>
    /// The buy me a coffee.
    /// </value>
    [Display(Name = "BuyMeACoffe Link")]
    public string BuyMeACoffee { get; set; }

    /// <summary>
    /// Gets or sets the amazon wishlist.
    /// </summary>
    /// <value>
    /// The amazon wishlist.
    /// </value>
    [Display(Name = "Amazon Wishlist Link")]
    public string AmazonWishlist { get; set; }

    /// <summary>
    /// Gets or sets the Paypal.me link.
    /// </summary>
    /// <value>
    /// The Paypal.me link.
    /// </value>
    [Display(Name = "Paypal.me Link")]
    public string Paypal { get; set; }

    /// <summary>
    /// Gets or sets the Patreon profile page.
    /// </summary>
    /// <value>
    /// The Patreon profile page.
    /// </value>
    [Display(Name = "Patreon Link")]
    public string Patreon { get; set; }

    /// <summary>
    /// Gets or sets the Liberapay profile page.
    /// </summary>
    /// <value>
    /// The Liberapay profile page.
    /// </value>
    [Display(Name = "Liberapay Link")]
    public string Liberapay { get; set; }
}
