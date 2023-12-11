using Moonglade.Data.ExternalAPI.GitHub;
using Moonglade.Data.ExternalAPI.GitHub.Models;

using Newtonsoft.Json;

using RestSharp;

namespace Moonglade.Web.Services;

public class GithubUserRepositoriesProvider : IGithubUserRepositoriesProvider
{
	private IBlogConfig _blogConfig { get; set; }
	private string _ghProfile { get; set; }
	private string _ghUser { get; set; }
	public GithubUserRepositoriesProvider(IBlogConfig blogConfig)
	{
		_blogConfig = blogConfig;
		_ghProfile = _blogConfig.SocialProfileSettings.GitHub;
		_ghUser = _ghProfile.Replace("https://github.com/", "");
	}

	public async Task<List<UserReposResponse.Root>> GetUserRepositories()
	{
		var endpoint = $"/users/{_ghUser}/repos";
		var _githubClient = new GithubClient(_blogConfig);
		var ghResponse = await _githubClient.SendRequest(Method.Get, endpoint, "");
		return JsonConvert.DeserializeObject<List<UserReposResponse.Root>>(ghResponse.Content);
	}
}
