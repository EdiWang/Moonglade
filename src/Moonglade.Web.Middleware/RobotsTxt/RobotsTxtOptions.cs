using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Web.Middleware.RobotsTxt
{
    // From https://github.com/karl-sjogren/robots-txt-middleware
    public class RobotsTxtOptions
    {
        public RobotsTxtOptions()
        {
            Sections = new List<RobotsTxtSection>();
            SitemapUrls = new List<string>();
        }

        public List<RobotsTxtSection> Sections { get; set; }

        public List<string> SitemapUrls { get; set; }

        public TimeSpan MaxAge { get; } = TimeSpan.FromDays(1);

        internal StringBuilder Build()
        {
            var builder = new StringBuilder();

            foreach (var section in Sections)
            {
                section.Build(builder);
                builder.AppendLine();
            }

            foreach (var url in SitemapUrls)
            {
                builder.AppendLine("Sitemap: " + url);
            }

            return builder;
        }
    }
}