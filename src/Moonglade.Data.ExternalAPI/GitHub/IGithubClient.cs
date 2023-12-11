using RestSharp;

namespace Moonglade.Data.ExternalAPI.GitHub;

public interface IGithubClient
{
	Task<RestResponse> SendRequest(Method method, string endpoint, string? body = "");
}
