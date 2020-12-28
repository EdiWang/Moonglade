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
        private readonly IGetApiKeyQuery _getApiKeyQuery;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IGetApiKeyQuery getApiKeyQuery) : base(options, logger, encoder, clock)
        {
            _getApiKeyQuery = getApiKeyQuery ?? throw new ArgumentNullException(nameof(getApiKeyQuery));
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues))
            {
                return AuthenticateResult.NoResult();
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return AuthenticateResult.NoResult();
            }

            var existingApiKey = await _getApiKeyQuery.Execute(providedApiKey);
            if (null == existingApiKey)
            {
                return await Task.FromResult(AuthenticateResult.Fail("Invalid API Key provided."));
            }

            var claims = new List<Claim>
                         {
                             new (ClaimTypes.Name, existingApiKey.Owner)
                         };

            claims.AddRange(existingApiKey.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
            var identities = new List<ClaimsIdentity> { identity };
            var principal = new ClaimsPrincipal(identities);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);

            return await Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
