#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace Moonglade.Auth.Oidc;

internal sealed class OwnerOidcBindingHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    HtmlEncoder htmlEncoder,
    UrlEncoder encoder,
    IDataProtectionProvider protection,
    IHttpClientFactory clients,
    OwnerOidcStore store,
    OidcBindingFlow flow) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder),
    IAuthenticationRequestHandler
{
    internal const string BackchannelClient = "OwnerOidc";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

    public async Task<bool> HandleRequestAsync()
    {
        if (!Request.Path.Equals(OidcBindingFlow.CallbackPath, StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            using var handler = await CreateHandlerAsync();
            if (handler is not null) return await handler.HandleRequestAsync();
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            Logger.LogWarning("The OIDC binding callback could not complete ({ErrorType}).", ex.GetType().Name);
        }
        RedirectOnFailure();
        return true;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        try
        {
            using var handler = await CreateHandlerAsync();
            if (handler is not null && properties.Items.TryGetValue(OidcBindingFlow.ProofKey, out var proof) &&
                flow.Matches(Context.User, handler.Revision, proof))
            {
                await handler.ChallengeAsync(properties);
                return;
            }
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            Logger.LogWarning("The OIDC binding challenge could not start ({ErrorType}).", ex.GetType().Name);
        }
        RedirectOnFailure();
    }

    private async Task<ConfiguredHandler?> CreateHandlerAsync()
    {
        var current = await store.ReadBindingCredentialsAsync(Context.RequestAborted);
        if (current is null) return null;
        var options = new OpenIdConnectOptions
        {
            Authority = current.Authority,
            ClientId = current.ClientId,
            ClientSecret = current.ClientSecret,
            SignInScheme = OidcBindingFlow.PendingScheme,
            CallbackPath = OidcBindingFlow.CallbackPath,
            ResponseType = OpenIdConnectResponseType.Code,
            ResponseMode = OpenIdConnectResponseMode.Query,
            UsePkce = true,
            MapInboundClaims = false,
            RemoteAuthenticationTimeout = OidcBindingFlow.Lifetime,
            Backchannel = clients.CreateClient(BackchannelClient),
            Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = ctx =>
                    OidcPublicBackchannel.VerifyRedirectAsync(ctx.ProtocolMessage.IssuerAddress, ctx.HttpContext.RequestAborted),
                OnMessageReceived = async ctx =>
                {
                    var session = await ctx.HttpContext.AuthenticateAsync(BlogAuthSchemas.Local);
                    if (session.Principal is not { } principal ||
                        principal.FindFirstValue(ClaimTypes.NameIdentifier) != current.OwnerId.ToString() ||
                        ctx.Properties is null ||
                        !ctx.Properties.Items.TryGetValue(OidcBindingFlow.ProofKey, out var proof) ||
                        !flow.Matches(principal, current.Revision, proof))
                        ctx.Fail("The OIDC binding request expired or changed.");
                },
                OnTokenValidated = ctx =>
                {
                    var subject = ctx.Principal?.FindFirstValue("sub");
                    var issuer = ctx.SecurityToken.Issuer;
                    if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255 || issuer.Length > 500 ||
                        !OidcPublicBackchannel.IsHttpsUrl(issuer))
                    {
                        ctx.Fail("The OIDC response did not identify an external account.");
                        return Task.CompletedTask;
                    }

                    var principal = ctx.Principal!;
                    var label = new[] { "preferred_username", "email", "name" }
                        .Select(principal.FindFirstValue).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? subject;
                    ctx.Properties!.Items[OidcBindingFlow.IssuerKey] = issuer;
                    ctx.Properties.Items[OidcBindingFlow.SubjectKey] = subject;
                    ctx.Properties.Items[OidcBindingFlow.LabelKey] = label.Length <= 255 ? label : label[..255];
                    return Task.CompletedTask;
                },
                OnTicketReceived = ctx =>
                {
                    ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(OidcBindingFlow.PendingScheme));
                    ctx.Properties!.ExpiresUtc = TimeProvider.GetUtcNow().Add(OidcBindingFlow.Lifetime);
                    ctx.Properties.IsPersistent = false;
                    ctx.ReturnUri = Request.PathBase.Add(OidcBindingFlow.ManagementPath);
                    return Task.CompletedTask;
                },
                OnRemoteFailure = ctx =>
                {
                    Logger.LogWarning("The OIDC binding response was rejected ({ErrorType}).", ctx.Failure?.GetType().Name);
                    RedirectOnFailure();
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                }
            }
        };

        new OpenIdConnectPostConfigureOptions(protection).PostConfigure(Scheme.Name, options);
        options.Validate();
        var handler = new ConfiguredHandler(current.Revision, options, htmlEncoder, UrlEncoder);
        await handler.InitializeAsync(Scheme, Context);
        return handler;
    }

    private bool IsProviderFailure(Exception exception) => !Context.RequestAborted.IsCancellationRequested &&
        exception is InvalidOperationException or HttpRequestException or CryptographicException or SocketException or TaskCanceledException;

    private void RedirectOnFailure() =>
        Response.Redirect(Request.PathBase.Add(OidcBindingFlow.ManagementPath).Value + "?failed=true");

    // Provider errors may contain credential or token material. Only the wrapper logs their type.
    private sealed class ConfiguredHandler(Guid revision, OpenIdConnectOptions options, HtmlEncoder htmlEncoder, UrlEncoder encoder)
        : OpenIdConnectHandler(new FixedOptions(options), NullLoggerFactory.Instance, htmlEncoder, encoder), IDisposable
    {
        public Guid Revision { get; } = revision;
        public void Dispose() => Backchannel.Dispose();
    }

    private sealed class FixedOptions(OpenIdConnectOptions value) : IOptionsMonitor<OpenIdConnectOptions>
    {
        public OpenIdConnectOptions CurrentValue => value;
        public OpenIdConnectOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
    }
}
