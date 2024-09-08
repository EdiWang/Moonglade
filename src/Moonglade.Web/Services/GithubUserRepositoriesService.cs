using Moonglade.Github.Client;
using Moonglade.Github.Client.Models;

using Newtonsoft.Json;

using RestSharp;

namespace Moonglade.Web.Services;

public class GithubUserRepositoriesService : IGithubUserRepositoriesService
{
    private readonly string _ghProfile;
    private readonly string _ghUser;
    private readonly ILogger<GithubUserRepositoriesService> _logger;
    private readonly IGithubClient _githubClient;

    public GithubUserRepositoriesService(IBlogConfig blogConfig, IGithubClient githubClient, ILogger<GithubUserRepositoriesService> logger)
    {
        _ghProfile = blogConfig.SocialProfileSettings.GitHub;
        _ghUser = _ghProfile.Replace("https://github.com/", "");
        _githubClient = githubClient;
        _logger = logger;
    }

    public async Task<List<UserRepository>> GetUserRepositories()
    {
        try
        {
            var endpoint = $"/users/{_ghUser}/repos";
            var ghResponse = await _githubClient.SendRequest(Method.Get, endpoint, "");
            _logger.LogInformation("Github response: {0}", ghResponse.Content);
            return JsonConvert.DeserializeObject<List<UserRepository>>(ghResponse.Content);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while fetching user repositories from Github. Error: {Message}", exception.Message);
            throw;
        }
    }
}
