using Moonglade.Github.Client.Models;

namespace Moonglade.Web.Services;

public interface IGithubUserRepositoriesService
{
	Task<List<UserRepository>> GetUserRepositories();
}
