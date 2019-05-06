using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Configuration;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class ConfigTests
    {
        [Test]
        public void TestBlogConfigDefaultValues()
        {
            IBlogConfig config = new BlogConfig();
            Assert.IsNotNull(config.BlogOwnerSettings);
            Assert.IsNotNull(config.GeneralSettings);
            Assert.IsNotNull(config.ContentSettings);
            Assert.IsNotNull(config.EmailConfiguration);
            Assert.IsNotNull(config.FeedSettings);
            Assert.IsNotNull(config.WatermarkSettings);
        }
    }
}
