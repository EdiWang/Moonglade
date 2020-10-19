using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moonglade.Syndication;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class SyndicationFeedGeneratorTests
    {
        [Test]
        public async Task TestRss20EmptyCollection()
        {
            var itemCollection = new List<FeedEntry>();

            var rw = new FeedGenerator
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
        public async Task TestRss20WithCollection()
        {
            var rw = new FeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = GetFeedItems(),
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
            var itemCollection = new List<FeedEntry>();

            var rw = new FeedGenerator
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

        [Test]
        public async Task TestAtom10WithCollection()
        {
            var itemCollection = new List<FeedEntry>();

            var rw = new FeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = GetFeedItems(),
                TrackBackUrl = "https://996.icu/trackback",
                MaxContentLength = 996,
                GeneratorVersion = "9.9.6"
            };

            var path = Path.Join(Path.GetTempPath(), $"Moonglade-UT-ATOM-{Guid.NewGuid()}.xml");
            await rw.WriteAtom10FileAsync(path);

            Assert.IsTrue(File.Exists(path));
        }

        private static IEnumerable<FeedEntry> GetFeedItems()
        {
            var itemCollection = new List<FeedEntry>
            {
                new FeedEntry
                {
                    Author = "J Ma",
                    Title = "996 is Fubao",
                    AuthorEmail = "admin@996.icu",
                    Categories = new []{ "Hard work" },
                    Description = "You young people need fen dou",
                    Id = "996",
                    Link = "https://996.icu/fubao",
                    PubDateUtc = DateTime.Now
                },
                new FeedEntry
                {
                    Author = "G Ni",
                    Title = "Cheating funds from zero to hero",
                    AuthorEmail = "copy-paste@from.github",
                    Categories = new []{ "Independent Development" },
                    Description = "Nation's Proud",
                    Id = "251",
                    Link = "https://404.com/no-such-thing",
                    PubDateUtc = DateTime.Now
                }
            };

            return itemCollection;
        }
    }
}
