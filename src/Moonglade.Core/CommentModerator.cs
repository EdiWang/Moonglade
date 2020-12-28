using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edi.WordFilter;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Core
{
    public interface ICommentModerator
    {
        public string ModerateContent(string input);

        public bool HasBadWord(params string[] input);
    }

    public class LocalWordFilterModerator : ICommentModerator
    {
        private readonly IMaskWordFilter _filter;

        public LocalWordFilterModerator(IBlogConfig blogConfig)
        {
            var sw = new StringWordSource(blogConfig.ContentSettings.DisharmonyWords);
            _filter = new MaskWordFilter(sw);
        }

        public string ModerateContent(string input)
        {
            return _filter.FilterContent(input);
        }

        public bool HasBadWord(params string[] input)
        {
            return input.Any(s => _filter.ContainsAnyWord(s));
        }
    }

    public class AzureContentModerator : ICommentModerator
    {
        private readonly AzureContentModeratorSettings _settings;

        public AzureContentModerator(AzureContentModeratorSettings settings)
        {
            _settings = settings;
        }

        public string ModerateContent(string input)
        {
            throw new NotImplementedException();
        }

        public bool HasBadWord(params string[] input)
        {
            throw new NotImplementedException();
        }
    }

    public class CommentModeratorSettings
    {
        public string Provider { get; set; }

        public AzureContentModeratorSettings AzureContentModeratorSettings { get; set; }
    }

    public class AzureContentModeratorSettings
    {
        public string OcpApimSubscriptionKey { get; set; }
        public string Endpoint { get; set; }
    }
}
