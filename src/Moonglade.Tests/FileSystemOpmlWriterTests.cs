using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moonglade.Syndication;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class FileSystemOpmlWriterTests
    {
        [Test]
        public async Task TestWriteOpmlFileAsync()
        {
            var catInfos = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Work 996", "work-996")
            };
            var siteRootUrl = "https://996.icu";

            var info = new OpmlDoc
            {
                SiteTitle = $"Work 996 - OPML",
                CategoryInfo = catInfos,
                HtmlUrl = $"{siteRootUrl}/post",
                XmlUrl = $"{siteRootUrl}/rss",
                CategoryXmlUrlTemplate = $"{siteRootUrl}/rss/category/[catTitle]",
                CategoryHtmlUrlTemplate = $"{siteRootUrl}/category/list/[catTitle]"
            };

            var path = Path.Join(Path.GetTempPath(), $"Moonglade-UT-OPML-{Guid.NewGuid()}.xml");

            var writer = new FileSystemOpmlWriter();
            await writer.WriteOpmlFileAsync(path, info);

            Assert.IsTrue(File.Exists(path));
        }
    }
}
