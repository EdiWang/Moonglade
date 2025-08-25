using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Moderation;

public interface IModeratorService
{
    Task<string> Mask(string input);
    Task<bool> Detect(params string[] input);
}

public class MoongladeModeratorService : IModeratorService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MoongladeModeratorService> _logger;
    private readonly ContentModeratorOptions _options;
    private readonly ILocalModerationService _localService;
    private readonly IRemoteModerationService _remoteService;
    private readonly bool _isEnabled;

    public MoongladeModeratorService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<MoongladeModeratorService> logger,
        IOptions<ContentModeratorOptions> options,
        ILocalModerationService localService = null,
        IRemoteModerationService remoteService = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _options = options.Value;
        _localService = localService;
        _remoteService = remoteService;
        _isEnabled = ValidateConfiguration();
    }

    public async Task<string> Mask(string input)
    {
        if (!_isEnabled || string.IsNullOrWhiteSpace(input))
            return input;

        if (IsLocalProvider())
        {
            return _localService?.ModerateContent(input) ?? input;
        }

        if (_remoteService != null)
        {
            var requestId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty;
            return await _remoteService.MaskAsync(input, requestId);
        }

        return input;
    }

    public async Task<bool> Detect(params string[] input)
    {
        if (!_isEnabled || input == null || input.Length == 0)
            return false;

        var validInputs = input.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (validInputs.Length == 0)
            return false;

        if (IsLocalProvider())
        {
            return _localService?.HasBadWords(validInputs) ?? false;
        }

        if (_remoteService != null)
        {
            var requestId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty;
            return await _remoteService.DetectAsync(validInputs, requestId);
        }

        return false;
    }

    private bool IsLocalProvider() =>
        string.Equals(_options.Provider, "local", StringComparison.OrdinalIgnoreCase);

    private bool ValidateConfiguration()
    {
        if (IsLocalProvider())
        {
            if (_localService == null)
            {
                _logger.LogError("Local moderation service is not configured");
                return false;
            }
            return true;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiEndpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogError("Remote ContentModerator API configuration is incomplete");
            return false;
        }

        if (_remoteService == null)
        {
            _logger.LogError("Remote moderation service is not configured");
            return false;
        }

        return true;
    }
}