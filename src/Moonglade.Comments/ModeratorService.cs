using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Moonglade.Comments;

public interface IModeratorService
{
    public Task<string> Mask(string input);

    public Task<bool> Detect(params string[] input);
}

public class AzureFunctionModeratorService : IModeratorService
{
    private readonly ILogger<AzureFunctionModeratorService> _logger;
    private readonly HttpClient _httpClient;

    public AzureFunctionModeratorService(ILogger<AzureFunctionModeratorService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;

        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.BaseAddress = new(configuration["ContentModerator:FunctionEndpoint"]!);
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", configuration["ContentModerator:FunctionKey"]);
    }

    public Task<string> Mask(string input)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Detect(params string[] input)
    {
        throw new NotImplementedException();
    }
}