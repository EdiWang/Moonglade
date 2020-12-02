using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using Moonglade.Model.Settings;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class TagServiceTests
    {
        [TestCase(".NET Core", ExpectedResult = "dotnet-core")]
        [TestCase("C#", ExpectedResult = "csharp")]
        [TestCase("955", ExpectedResult = "955")]
        public string NormalizeTagNameEnglish(string str)
        {
            var dic = new TagNormalization[]
            {
                new TagNormalization { Source = " ", Target = "-" },
                new TagNormalization { Source = "#", Target = "sharp" },
                new TagNormalization { Source = ".", Target = "dot" }
            };

            return TagService.NormalizeTagName(str, dic);
        }

        [TestCase("福报", ExpectedResult = "8f-79-a5-62")]
        public string NormalizeTagNameNonEnglish(string str)
        {
            var dic = System.Array.Empty<TagNormalization>();
            return TagService.NormalizeTagName(str, dic);
        }

        [TestCase("C", ExpectedResult = true)]
        [TestCase("C++", ExpectedResult = true)]
        [TestCase("C#", ExpectedResult = true)]
        [TestCase("Java", ExpectedResult = true)]
        [TestCase("996", ExpectedResult = true)]
        [TestCase(".NET", ExpectedResult = true)]
        [TestCase("C Sharp", ExpectedResult = true)]
        [TestCase("Cup<T>", ExpectedResult = false)]
        [TestCase("(1)", ExpectedResult = false)]
        [TestCase("usr/bin", ExpectedResult = false)]
        [TestCase("", ExpectedResult = false)]
        public bool ValidateTagName(string tagDisplayName)
        {
            return TagService.ValidateTagName(tagDisplayName);
        }
    }
}
