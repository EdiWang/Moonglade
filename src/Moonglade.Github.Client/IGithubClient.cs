using RestSharp;

namespace Moonglade.Github.Client
{
    public interface IGithubClient
    {
        Task<RestResponse> SendRequest(Method method, string endpoint, string? body = "");
    }
}
