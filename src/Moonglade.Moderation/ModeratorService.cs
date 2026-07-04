using Microsoft.Extensions.Logging;

namespace Moonglade.Moderation;

public interface IModeratorService
{
    Task<string> Mask(string input);
    Task<bool> Detect(params string[] input);
}

public class MoongladeModeratorService : IModeratorService
{
    private readonly ILogger<MoongladeModeratorService> _logger;
    private readonly ILocalModerationService _localService;
    private readonly bool _isEnabled;

    public MoongladeModeratorService(
        ILogger<MoongladeModeratorService> logger,
        ILocalModerationService localService = null)
    {
        _logger = logger;
        _localService = localService;
        _isEnabled = ValidateConfiguration();
    }

    public Task<string> Mask(string input)
    {
        if (!_isEnabled || string.IsNullOrWhiteSpace(input))
            return Task.FromResult(input);

        return Task.FromResult(_localService?.ModerateContent(input) ?? input);
    }

    public Task<bool> Detect(params string[] input)
    {
        if (!_isEnabled || input == null || input.Length == 0)
            return Task.FromResult(false);

        var validInputs = input.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (validInputs.Length == 0)
            return Task.FromResult(false);

        return Task.FromResult(_localService?.HasBadWords(validInputs) ?? false);
    }

    private bool ValidateConfiguration()
    {
        if (_localService == null)
        {
            _logger.LogError("Local moderation service is not configured");
            return false;
        }

        return true;
    }
}
