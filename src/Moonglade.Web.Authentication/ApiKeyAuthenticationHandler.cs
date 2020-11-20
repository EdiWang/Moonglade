using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Web.Authentication
{
    // Credits: https://josefottosson.se/asp-net-core-protect-your-api-with-api-keys/
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        public AuthenticationSettings AuthenticationSettings { get; set; }

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IOptions<AuthenticationSettings> authenticationSettings) : base(options, logger, encoder, clock)
        {
            AuthenticationSettings = authenticationSettings.Value;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                return await Task.FromResult(AuthenticateResult.NoResult());
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return await Task.FromResult(AuthenticateResult.NoResult());
            }

            if (AuthenticationSettings.ApiKeys is null || !AuthenticationSettings.ApiKeys.Any())
            {
                throw new ArgumentNullException(nameof(AuthenticationSettings.ApiKeys), "No API Keys configured.");
            }

            IReadOnlyDictionary<string, ApiKey> apiKeysDic = AuthenticationSettings.ApiKeys.ToDictionary(x => x.Key, x => x);

            if (apiKeysDic.ContainsKey(providedApiKey))
            {
                var apiKey = apiKeysDic[providedApiKey];

                var claims = new List<Claim>
                {
                    new (ClaimTypes.Name, apiKey.Owner)
                };
                claims.AddRange(apiKey.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

                var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
                var identities = new List<ClaimsIdentity> { identity };
                var principal = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(principal, Options.Scheme);

                return await Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return await Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
        }
    }
}
