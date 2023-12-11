using Moonglade.Data.ExternalAPI.GitHub.Models;

namespace Moonglade.Web.Services;

public interface IGithubUserRepositoriesProvider
{
	Task<List<UserReposResponse.Root>> GetUserRepositories();
}
