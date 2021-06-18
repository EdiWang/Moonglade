using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Web.TagHelpers;
using NUnit.Framework;

namespace Moonglade.Web.Tests.TagHelpers
{
    [TestFixture]
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

        [TestCase("name")]
        [TestCase(ClaimTypes.Name)]
        public void Process_Name(string claimType)
        {
            var tagHelper = new UserInfoTagHelper
            {
                UserInfoDisplay = UserInfoDisplay.PreferName,
                User = GetClaimsPrincipal(new Claim[]
                {
                    new(claimType, FakeData.ShortString1)
                })
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new ("class", UserInfoTagHelper.TagClassBase)
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
            Assert.AreEqual(FakeData.ShortString1, output.Content.GetContent());
        }

        [TestCase("email")]
        [TestCase(ClaimTypes.Email)]
        public void Process_Email(string claimType)
        {
            var tagHelper = new UserInfoTagHelper
            {
                UserInfoDisplay = UserInfoDisplay.PreferEmail,
                User = GetClaimsPrincipal(new Claim[]
                {
                    new(claimType, "996@icu.com")
                })
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new ("class", UserInfoTagHelper.TagClassBase)
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
            Assert.AreEqual("996@icu.com", output.Content.GetContent());
        }

        [Test]
        public void Process_PreferNameWithEmail()
        {
            var tagHelper = new UserInfoTagHelper
            {
                UserInfoDisplay = UserInfoDisplay.PreferName,
                User = GetClaimsPrincipal(new Claim[]
                {
                    new(ClaimTypes.Email, "996@icu.com")
                })
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new ("class", UserInfoTagHelper.TagClassBase)
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
            Assert.AreEqual("996@icu.com", output.Content.GetContent());
        }


        [Test]
        public void Process_PreferEmailWithName()
        {
            var tagHelper = new UserInfoTagHelper
            {
                UserInfoDisplay = UserInfoDisplay.PreferEmail,
                User = GetClaimsPrincipal(new Claim[]
                {
                    new(ClaimTypes.Name, FakeData.ShortString1)
                })
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new ("class", UserInfoTagHelper.TagClassBase)
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
            Assert.AreEqual(FakeData.ShortString1, output.Content.GetContent());
        }

        [Test]
        public void Process_Both()
        {
            var tagHelper = new UserInfoTagHelper
            {
                UserInfoDisplay = UserInfoDisplay.Both,
                User = GetClaimsPrincipal(new Claim[]
                {
                    new(ClaimTypes.Name, FakeData.ShortString1),
                    new(ClaimTypes.Email, "996@icu.com")
                })
            };

            var outputAttributes = new TagHelperAttributeList();
            var context = TagHelperTestsHelpers.MakeTagHelperContext("div");
            var output = TagHelperTestsHelpers.MakeTagHelperOutput("div", outputAttributes);

            tagHelper.Process(context, output);

            var expectedAttributeList = new TagHelperAttributeList
            {
                new ("class", UserInfoTagHelper.TagClassBase)
            };

            Assert.AreEqual(expectedAttributeList, output.Attributes);
            Assert.AreEqual($"<div class='{UserInfoTagHelper.TagClassBase}-name'>fubao</div><email class='{UserInfoTagHelper.TagClassBase}-email'>996@icu.com</email>", output.Content.GetContent());
        }

        private ClaimsPrincipal GetClaimsPrincipal(IEnumerable<Claim> claims)
        {
            var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var p = new ClaimsPrincipal(ci);

            return p;
        }
    }
}
