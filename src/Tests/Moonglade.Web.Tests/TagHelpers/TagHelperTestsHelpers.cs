using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.Tests.TagHelpers
{
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
}