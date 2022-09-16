using Microsoft.Azure.CognitiveServices.ContentModerator;
using System.Text;

namespace Moonglade.Comments.Moderators;

public class AzureContentModerator : ICommentModerator, IDisposable
{
    private readonly IContentModeratorClient _client;

    public AzureContentModerator(IContentModeratorClient client) => _client = client;

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
            if (screenResult.Terms is not null && screenResult.Terms.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose() => _client?.Dispose();
}