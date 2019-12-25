using System.Collections.Generic;
using System.Text;

namespace Moonglade.Web.Middleware.RobotsTxt
{
    public class Rule
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class RobotsTxtSection
    {
        public string Comment { get; set; }
        public string UserAgent { get; set; }
        public List<Rule> Rules { get; set; }

        public RobotsTxtSection()
        {
            Comment = string.Empty;
            UserAgent = string.Empty;
            Rules = new List<Rule>();
        }

        public void Build(StringBuilder builder)
        {
            if (string.IsNullOrEmpty(UserAgent))
                return;

            builder.AppendLine("# " + Comment);
            builder.AppendLine("User-agent: " + UserAgent);

            foreach (var rule in Rules)
            {
                builder.AppendLine($"{rule.Key}: {rule.Value}");
            }
        }
    }
}
