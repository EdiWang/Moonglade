using System;
using System.Globalization;

namespace Moonglade.Web.Middleware.RobotsTxt
{
    public class SectionBuilder
    {
        internal RobotsTxtSection Section { get; }

        internal SectionBuilder()
        {
            Section = new RobotsTxtSection();
        }

        public SectionBuilder SetUserAgent(string userAgent)
        {
            Section.UserAgent = userAgent;
            return this;
        }

        public SectionBuilder SetComment(string comment)
        {
            Section.Comment = comment;
            return this;
        }

        public SectionBuilder AddCrawlDelay(TimeSpan delay)
        {
            Section.Rules.Add(new Rule
            {
                Key = "Crawl-delay",
                Value = delay.TotalSeconds.ToString(CultureInfo.InvariantCulture)
            });
            return this;
        }

        public SectionBuilder Allow(string path)
        {
            Section.Rules.Add(new Rule
            {
                Key = "Allow",
                Value = path
            });
            return this;
        }

        public SectionBuilder Disallow(string path)
        {
            Section.Rules.Add(new Rule
            {
                Key = "Disallow",
                Value = path
            });
            return this;
        }
    }
}
