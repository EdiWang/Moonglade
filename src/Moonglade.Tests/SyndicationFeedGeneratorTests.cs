using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moonglade.Syndication;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class SyndicationFeedGeneratorTests
    {
        [Test]
        public async Task  TestRss20EmptyCollection()
        {
            var itemCollection = new List<SimpleFeedItem>();

            var rw = new SyndicationFeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator v9.9.6",
                FeedItemCollection = itemCollection,
                TrackBackUrl = "https://996.icu/trackback",
                MaxContentLength = 0
            };

            var path = Path.Join(Path.GetTempPath(), $"Moonglade-UT-{Guid.NewGuid()}.xml");
            await rw.WriteRss20FileAsync(path);

            Assert.IsTrue(File.Exists(path));
        }
    }
}
