using System;

namespace Moonglade.Web.Middleware.RobotsTxt
{
    // From https://github.com/karl-sjogren/robots-txt-middleware
    public class RobotsTxtOptionsBuilder
    {
        private readonly RobotsTxtOptions _options;

        internal RobotsTxtOptionsBuilder()
        {
            _options = new RobotsTxtOptions();
        }

        public RobotsTxtOptionsBuilder DenyAll()
        {
            _options.Sections.Clear();
            return AddSection(section =>
                section
                    .SetUserAgent("*")
                    .Disallow("/")
            );
        }

        public RobotsTxtOptionsBuilder AllowAll()
        {
            _options.Sections.Clear();
            return AddSection(section =>
                section
                    .SetUserAgent("*")
                    .Disallow(string.Empty)
            );
        }

        public RobotsTxtOptionsBuilder AddSection(Func<SectionBuilder, SectionBuilder> builder)
        {
            var sectionBuilder = new SectionBuilder();
            sectionBuilder = builder(sectionBuilder);
            _options.Sections.Add(sectionBuilder.Section);
            return this;
        }

        public RobotsTxtOptionsBuilder AddSitemap(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentException("Url must be an absolute url for sitemaps.", nameof(url));

            _options.SitemapUrls.Add(url);
            return this;
        }

        public RobotsTxtOptions Build()
        {
            return _options;
        }
    }
}