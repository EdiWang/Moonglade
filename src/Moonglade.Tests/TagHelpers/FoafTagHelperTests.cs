using Moonglade.Web.TagHelpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Foaf;

namespace Moonglade.Tests.TagHelpers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FoafTagHelperTests
    {
        [Test]
        public void Process_Success()
        {
            var outputAttributes = new TagHelperAttributeList();

            var foafTagHelper = new FoafTagHelper { Href = "https://996.icu/foaf" };

            var context = MakeTagHelperContext();
            var output = MakeTagHelperOutput("link", outputAttributes);

            foafTagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new("type", new HtmlString(FoafWriter.ContentType)),
                new("rel", "meta"),
                new("title", "FOAF"),
                new("href", "https://996.icu/foaf")
            };

            Assert.AreEqual(FoafWriter.ContentType, ((HtmlString)output.Attributes["type"].Value).Value);
            Assert.AreEqual(expectedAttributeList["rel"], output.Attributes["rel"]);
            Assert.AreEqual(expectedAttributeList["title"], output.Attributes["title"]);
            Assert.AreEqual(expectedAttributeList["href"], output.Attributes["href"]);
        }

        private static TagHelperContext MakeTagHelperContext(TagHelperAttributeList attributes = null)
        {
            attributes ??= new();

            return new(
                "link",
                attributes,
                new Dictionary<object, object>(),
                Guid.NewGuid().ToString("N"));
        }

        private static TagHelperOutput MakeTagHelperOutput(string tagName, TagHelperAttributeList attributes = null)
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
}
