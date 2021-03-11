using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Web.TagHelpers;
using NUnit.Framework;

namespace Moonglade.Web.Tests.TagHelpers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class UserInfoTagHelperTests
    {
        [Test]
        public void Process_UserNull()
        {
            var tagHelper = new UserInfoTagHelper
            {
                User = null
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList();
            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }

        [Test]
        public void Process_UserIdentityNull()
        {
            var tagHelper = new UserInfoTagHelper
            {
                User = new()
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList();
            Assert.AreEqual(expectedAttributeList, output.Attributes);
        }
    }
}
