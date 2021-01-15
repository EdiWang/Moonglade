using Moonglade.Web.TagHelpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DateTimeOps;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Foaf;
using Moq;

namespace Moonglade.Tests.TagHelpers
{
    [ExcludeFromCodeCoverage]
    public class TagHelperTestsHelpers
    {
        public static TagHelperContext MakeTagHelperContext(string tagName, TagHelperAttributeList attributes = null)
        {
            attributes ??= new();

            return new(
                tagName,
                attributes,
                new Dictionary<object, object>(),
                Guid.NewGuid().ToString("N"));
        }

        public static TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
        {
            attributes ??= new();

            return new(
                tagName,
                attributes,
                (_, _) => Task.FromResult<TagHelperContent>(
                    new DefaultTagHelperContent()))
            {
                TagMode = TagMode.SelfClosing,
            };
        }
    }

    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagHelperTests
    {
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

            var dateTimeResolverMock = new Mock<IDateTimeResolver>();
            dateTimeResolverMock.Setup(p => p.ToTimeZone(It.IsAny<DateTime>())).Returns(pubDateUtc);

            var outputAttributes = new TagHelperAttributeList();

            var tagHelper = new PubDateTagHelper
            {
                PubDateUtc = pubDateUtc,
                DateTimeResolver = dateTimeResolverMock.Object
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
    }
}
