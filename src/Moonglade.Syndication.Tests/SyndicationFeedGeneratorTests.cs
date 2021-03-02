using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Moonglade.Syndication.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SyndicationFeedGeneratorTests
    {
        readonly List<FeedEntry> _fakeFeedsNoAuthor = new()
        {
            new()
            {
                Categories = new[] { "fubao", "cusi" },
                Description = "Work **996** and get into ICU",
                Id = "FUBAO996",
                Link = "https://996.icu",
                PubDateUtc = new(996, 9, 9),
                Title = "996 is fubao"
            }
        };

        readonly List<FeedEntry> _fakeFeedsNoCategory = new()
        {
            new()
            {
                Author = "Jack Ma",
                AuthorEmail = "996@ali.com",
                Description = "Work **996** and get into ICU",
                Id = "FUBAO996",
                Link = "https://996.icu",
                PubDateUtc = new(996, 9, 9),
                Title = "996 is fubao"
            }
        };

        [Test]
        public async Task GetItemCollection_NoCategory()
        {
            var rw = new FeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = _fakeFeedsNoCategory,
                TrackBackUrl = "https://996.icu/trackback",
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteRssAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿﻿<?xml version=""1.0"" encoding=""utf-16""?><rss version=""2.0""><channel><title>996 ICU</title><description>Work 996 and get into ICU</description><link>https://996.icu/trackback</link>"));
        }

        [Test]
        public async Task GetItemCollection_NoEmail()
        {
            var rw = new FeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = _fakeFeedsNoAuthor,
                TrackBackUrl = "https://996.icu/trackback",
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteRssAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿﻿<?xml version=""1.0"" encoding=""utf-16""?><rss version=""2.0""><channel><title>996 ICU</title><description>Work 996 and get into ICU</description><link>https://996.icu/trackback</link>"));
        }

        [Test]
        public async Task Rss20_EmptyCollection()
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
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteRssAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿﻿<?xml version=""1.0"" encoding=""utf-16""?><rss version=""2.0""><channel><title>996 ICU</title><description>Work 996 and get into ICU</description><link>https://996.icu/trackback</link>"));
        }

        [Test]
        public async Task Rss20_HasCollection()
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
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteRssAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿﻿<?xml version=""1.0"" encoding=""utf-16""?><rss version=""2.0""><channel><title>996 ICU</title><description>Work 996 and get into ICU</description><link>https://996.icu/trackback</link>"));
        }

        [Test]
        public async Task Atom10_EmptyCollection()
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
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteAtomAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿<?xml version=""1.0"" encoding=""utf-16""?><feed xmlns=""http://www.w3.org/2005/Atom""><title>996 ICU</title><subtitle>Work 996 and get into ICU</subtitle><rights>(c) 2020 996.icu</rights>"));
        }

        [Test]
        public async Task Atom10_HasCollection()
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
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteAtomAsync();

            Assert.IsNotNull(xmlContent);
            Assert.IsTrue(xmlContent.StartsWith(@"﻿<?xml version=""1.0"" encoding=""utf-16""?><feed xmlns=""http://www.w3.org/2005/Atom""><title>996 ICU</title><subtitle>Work 996 and get into ICU</subtitle><rights>(c) 2020 996.icu</rights>"));
        }

        [Test]
        public async Task Rss20_NullCollection()
        {
            var rw = new FeedGenerator
            {
                HostUrl = "https://996.icu",
                HeadTitle = "996 ICU",
                HeadDescription = "Work 996 and get into ICU",
                Copyright = "(c) 2020 996.icu",
                Generator = "Fubao Generator",
                FeedItemCollection = null,
                TrackBackUrl = "https://996.icu/trackback",
                GeneratorVersion = "9.9.6"
            };

            var xmlContent = await rw.WriteRssAsync();
            Assert.IsNotNull(xmlContent);
        }

        private static IEnumerable<FeedEntry> GetFeedItems()
        {
            var itemCollection = new List<FeedEntry>
            {
                new()
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
                new()
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
