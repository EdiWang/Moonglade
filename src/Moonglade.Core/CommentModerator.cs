using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edi.WordFilter;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Core
{
    public interface ICommentModerator
    {
        public Task<string> ModerateContent(string input);

        public Task<bool> HasBadWord(params string[] input);
    }

    public class LocalWordFilterModerator : ICommentModerator
    {
        private readonly IMaskWordFilter _filter;

        public LocalWordFilterModerator(IBlogConfig blogConfig)
        {
            var sw = new StringWordSource(blogConfig.ContentSettings.DisharmonyWords);
            _filter = new MaskWordFilter(sw);
        }

        public Task<string> ModerateContent(string input)
        {
            return Task.FromResult(_filter.FilterContent(input));
        }

        public Task<bool> HasBadWord(params string[] input)
        {
            return Task.FromResult(input.Any(s => _filter.ContainsAnyWord(s)));
        }
    }

    public class AzureContentModerator : ICommentModerator, IDisposable
    {
        private readonly ContentModeratorClient _client;

        public AzureContentModerator(AzureContentModeratorSettings settings)
        {
            _client = Authenticate(settings.OcpApimSubscriptionKey, settings.Endpoint);
        }

        private static ContentModeratorClient Authenticate(string key, string endpoint)
        {
            var client = new ContentModeratorClient(new ApiKeyServiceClientCredentials(key))
            {
                Endpoint = endpoint
            };
            return client;
        }

        public async Task<string> ModerateContent(string input)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(input);
            var stream = new MemoryStream(textBytes);
            var screenResult = await _client.TextModeration.ScreenTextAsync("text/plain", stream);

            if (screenResult.Terms is not null)
            {
                foreach (var item in screenResult.Terms)
                {
                    // TODO: Find a more efficient way
                    input = input.Replace(item.Term, "*");
                }
            }

            return input;
        }

        public async Task<bool> HasBadWord(params string[] input)
        {
            foreach (var s in input)
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(s);
                var stream = new MemoryStream(textBytes);
                var screenResult = await _client.TextModeration.ScreenTextAsync("text/plain", stream);
                if (screenResult.Terms is not null)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _client?.Dispose();
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
