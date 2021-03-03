using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Web.BlogProtocols;
using Moonglade.Web.TagHelpers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.TagHelpers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagHelperTests
    {
        [Test]
        public void RSDTagHelper_Process()
        {
            var outputAttributes = new TagHelperAttributeList();
            var tagHelper = new RSDTagHelper { Href = "https://996.icu/rsd" };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("link");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("link", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("type", new HtmlString("application/rsd+xml")),
                new("rel", "edituri"),
                new("title", "RSD"),
                new("href", tagHelper.Href)
            };

            Assert.AreEqual("application/rsd+xml", ((HtmlString)output.Attributes["type"].Value).Value);
            Assert.AreEqual(expectedAttributeList["rel"], output.Attributes["rel"]);
            Assert.AreEqual(expectedAttributeList["title"], output.Attributes["title"]);
            Assert.AreEqual(expectedAttributeList["href"], output.Attributes["href"]);
        }

        [Test]
        public void FoafTagHelper_Process()
        {
            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new FoafTagHelper { Href = "https://996.icu/foaf" };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("link");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("link", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("type", new HtmlString(FoafWriter.ContentType)),
                new("rel", "meta"),
                new("title", "FOAF"),
                new("href", tagHelper.Href)
            };

            Assert.AreEqual(FoafWriter.ContentType, ((HtmlString)output.Attributes["type"].Value).Value);
            Assert.AreEqual(expectedAttributeList["rel"], output.Attributes["rel"]);
            Assert.AreEqual(expectedAttributeList["title"], output.Attributes["title"]);
            Assert.AreEqual(expectedAttributeList["href"], output.Attributes["href"]);
        }

        [Test]
        public void RssTagHelper_Process()
        {
            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new RssTagHelper
            {
                Href = "https://996.icu/rss",
                Title = "Work 996 and get into ICU"
            };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("link");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("link", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("type", new HtmlString(FoafWriter.ContentType)),
                new("rel", "alternate"),
                new("title", tagHelper.Title),
                new("href", tagHelper.Href)
            };

            Assert.AreEqual("application/rss+xml", ((HtmlString)output.Attributes["type"].Value).Value);
            Assert.AreEqual(expectedAttributeList["rel"], output.Attributes["rel"]);
            Assert.AreEqual(expectedAttributeList["title"], output.Attributes["title"]);
            Assert.AreEqual(expectedAttributeList["href"], output.Attributes["href"]);
        }

        [Test]
        public void MetaDescriptionTagHelper_Process()
        {
            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new MetaDescriptionTagHelper
            {
                Description = "Stay away from PDD"
            };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("meta");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("meta", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("name", "description"),
                new("content", tagHelper.Description.Trim())
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }

        [Test]
        public void OpenSearchTagHelper_Process()
        {
            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new OpenSearchTagHelper
            {
                Href = "https://996.icu/opensearch",
                Title = "Work 996 and get into ICU"
            };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("link");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("link", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("type", new HtmlString("application/opensearchdescription+xml")),
                new("rel", "search"),
                new("title", tagHelper.Title.Trim()),
                new("href", tagHelper.Href)
            };

            Assert.AreEqual("application/opensearchdescription+xml", ((HtmlString)output.Attributes["type"].Value).Value);
            Assert.AreEqual(expectedAttributeList["rel"], output.Attributes["rel"]);
            Assert.AreEqual(expectedAttributeList["title"], output.Attributes["title"]);
            Assert.AreEqual(expectedAttributeList["href"], output.Attributes["href"]);
        }

        [Test]
        public void PubDateTagHelper_Process()
        {
            var pubDateUtc = new DateTime(996, 9, 6);

            var dateTimeResolverMock = new Mock<ITZoneResolver>();
            dateTimeResolverMock.Setup(p => p.ToTimeZone(It.IsAny<DateTime>())).Returns(pubDateUtc);

            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new PubDateTagHelper
            {
                PubDateUtc = pubDateUtc,
                TZoneResolver = dateTimeResolverMock.Object
            };

            var context = TagHelperTestsHelpers.MakeTagHelperContext("time");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("time", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("title", $"GMT {pubDateUtc}"),
                new("datetime", pubDateUtc.ToString("u"))
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);

            var d = new DefaultTagHelperContent();
            var expectedContent = d.SetContent(pubDateUtc.ToLongDateString());

            Assert.AreEqual(expectedContent.GetContent(), output.Content.GetContent());
        }

        [Test]
        public void GravatarImage_Process()
        {
            var tagHelper = new GravatarImageHelper
            {
                Email = "996@icu.com"
            };
            
            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("img");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("img", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("src", "https://secure.gravatar.com/avatar/a7da2236b13239fc32f99efa50540744?s=58&d=&r=g"),
                new("alt", "Gravatar image"),
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }

        [Test]
        public void GravatarImage_Process_ImageSize()
        {
            var tagHelper = new GravatarImageHelper
            {
                Email = "996@icu.com",
                Size = 996
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("img");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("img", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("src", "https://secure.gravatar.com/avatar/a7da2236b13239fc32f99efa50540744?s=996&d=&r=g"),
                new("alt", "Gravatar image"),
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }

        [Test]
        public void GravatarImage_Process_NoHttps()
        {
            var tagHelper = new GravatarImageHelper
            {
                Email = "996@icu.com",
                PreferHttps = false
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("img");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("img", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("src", "http://www.gravatar.com/avatar/a7da2236b13239fc32f99efa50540744?s=58&d=&r=g"),
                new("alt", "Gravatar image"),
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }
    }
}
