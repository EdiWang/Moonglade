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
                Generator = "Fubao Generator",
                FeedItemCollection = itemCollection,
                TrackBackUrl = "https://996.icu/trackback",
                MaxContentLength = 996,
                GeneratorVersion = "9.9.6"
            };

            var path = Path.Join(Path.GetTempPath(), $"Moonglade-UT-RSS-{Guid.NewGuid()}.xml");
            await rw.WriteRss20FileAsync(path);

            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public async Task TestAtom10EmptyCollection()
        {
            var itemCollection = new List<SimpleFeedItem>();

            var rw = new SyndicationFeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = itemCollection,
                TrackBackUrl = "https://996.icu/trackback",
                MaxContentLength = 996,
                GeneratorVersion = "9.9.6"
            };

            var path = Path.Join(Path.GetTempPath(), $"Moonglade-UT-ATOM-{Guid.NewGuid()}.xml");
            await rw.WriteAtom10FileAsync(path);

            Assert.IsTrue(File.Exists(path));
        }
    }
}
