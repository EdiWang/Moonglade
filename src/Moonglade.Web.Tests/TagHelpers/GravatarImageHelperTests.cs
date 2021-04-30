using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Web.TagHelpers;
using NUnit.Framework;

namespace Moonglade.Web.Tests.TagHelpers
{
    [TestFixture]

    public class GravatarImageHelperTests
    {
        [Test]
        public void Process_OK()
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
        public void Process_ImageSize()
        {
            var tagHelper = new GravatarImageHelper
            {
                Email = "996@icu.com",
                Size = FakeData.Int2
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
        public void Process_NoHttps()
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
