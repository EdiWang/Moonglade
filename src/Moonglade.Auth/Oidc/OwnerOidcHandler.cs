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

internal sealed class OwnerOidcHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    HtmlEncoder htmlEncoder,
    UrlEncoder encoder,
    IDataProtectionProvider protection,
    IHttpClientFactory clients,
    OwnerOidcStore store,
    OwnerAccountStore accounts,
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
            var current = await store.ReadCredentialsAsync(Context.RequestAborted);
            if (current is not null)
            {
                using var handler = await CreateHandlerAsync(current);
                if (await handler.HandleRequestAsync()) return true;
            }
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            Logger.LogWarning("The OIDC callback could not complete ({ErrorType}).", ex.GetType().Name);
        }
        await RedirectOnFailureAsync();
        return true;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var binding = properties.Items.TryGetValue(OidcBindingFlow.ProofKey, out var proof);
        try
        {
            var current = await store.ReadCredentialsAsync(Context.RequestAborted);
            if (current is not null && (binding
                ? current.SessionRevision is null && flow.Matches(Context.User, current.Revision, proof)
                : CanSignIn(current, properties)))
            {
                using var handler = await CreateHandlerAsync(current);
                await handler.ChallengeAsync(properties);
                return;
            }
        }
        catch (Exception ex) when (IsProviderFailure(ex))
        {
            Logger.LogWarning("The OIDC challenge could not start ({ErrorType}).", ex.GetType().Name);
        }
        await RedirectOnFailureAsync(binding);
    }

    private async Task<ConfiguredHandler> CreateHandlerAsync(OwnerOidcCredentials current)
    {
        bool? binding = null;
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
            SaveTokens = false,
            RemoteAuthenticationTimeout = OidcBindingFlow.Lifetime,
            Backchannel = clients.CreateClient(BackchannelClient),
            Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = ctx =>
                    OidcPublicBackchannel.VerifyRedirectAsync(ctx.ProtocolMessage.IssuerAddress, ctx.HttpContext.RequestAborted),
                OnMessageReceived = async ctx =>
                {
                    if (ctx.Properties?.Items.TryGetValue(OidcBindingFlow.ProofKey, out var proof) == true)
                    {
                        binding = true;
                        var session = await ctx.HttpContext.AuthenticateAsync(BlogAuthSchemas.Local);
                        if (current.SessionRevision is not null || session.Principal is not { } principal ||
                            principal.FindFirstValue(ClaimTypes.NameIdentifier) != current.OwnerId.ToString() ||
                            !flow.Matches(principal, current.Revision, proof))
                            ctx.Fail("The OIDC binding request expired or changed.");
                    }
                    else if (ctx.Properties is { } state && state.Items.ContainsKey(OidcBindingFlow.SignInRevisionKey))
                    {
                        binding = false;
                        if (!CanSignIn(current, state)) ctx.Fail("The OIDC sign-in request expired or changed.");
                    }
                    else ctx.Fail("The OIDC request is invalid.");
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
                OnTicketReceived = async ctx =>
                {
                    if (binding != true)
                    {
                        await CompleteSignInAsync(ctx, current);
                        return;
                    }

                    ctx.Principal = new ClaimsPrincipal(new ClaimsIdentity(OidcBindingFlow.PendingScheme));
                    ctx.Properties!.ExpiresUtc = TimeProvider.GetUtcNow().Add(OidcBindingFlow.Lifetime);
                    ctx.Properties.IsPersistent = false;
                    ctx.ReturnUri = Request.PathBase.Add(OidcBindingFlow.ManagementPath);
                },
                OnRemoteFailure = async ctx =>
                {
                    Logger.LogWarning("The OIDC response was rejected ({ErrorType}).", ctx.Failure?.GetType().Name);
                    await RedirectOnFailureAsync(binding);
                    ctx.HandleResponse();
                }
            }
        };

        new OpenIdConnectPostConfigureOptions(protection).PostConfigure(Scheme.Name, options);
        options.Validate();
        var handler = new ConfiguredHandler(options, htmlEncoder, UrlEncoder);
        await handler.InitializeAsync(Scheme, Context);
        return handler;
    }

    private bool CanSignIn(OwnerOidcCredentials current, AuthenticationProperties properties) =>
        current.SessionRevision is not null && properties.ExpiresUtc > TimeProvider.GetUtcNow() &&
        properties.Items.TryGetValue(OidcBindingFlow.SignInRevisionKey, out var requestedRevision) &&
        Guid.TryParse(requestedRevision, out var revision) && revision == current.Revision;

    private async Task CompleteSignInAsync(TicketReceivedContext context, OwnerOidcCredentials current)
    {
        var ct = Context.RequestAborted;
        var items = context.Properties!.Items;
        items.TryGetValue(OidcBindingFlow.IssuerKey, out var issuer);
        items.TryGetValue(OidcBindingFlow.SubjectKey, out var subject);
        var revision = await store.MatchSignInAsync(current, issuer, subject, ct);
        var account = revision is not null ? await accounts.ReadAsync(ct) : null;
        if (revision is not { } sessionRevision || account is not { IsLocalInitialized: true } || account.Id != current.OwnerId)
        {
            await RedirectOnFailureAsync(binding: false);
        }
        else
        {
            await Context.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
            await Context.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
            await Context.SignInAsync(BlogAuthSchemas.Local, LocalAccountPrincipal.Create(account, BlogAuthSchemas.Local, sessionRevision));
            Response.Redirect(context.ReturnUri ?? Request.PathBase.Add("/admin"));
        }
        context.HandleResponse();
    }

    private bool IsProviderFailure(Exception exception) => !Context.RequestAborted.IsCancellationRequested &&
        exception is InvalidOperationException or HttpRequestException or CryptographicException or SocketException or TaskCanceledException;

    private async Task RedirectOnFailureAsync(bool? binding = null)
    {
        binding ??= (await Context.AuthenticateAsync(BlogAuthSchemas.Local)).Succeeded;
        Response.Redirect(Request.PathBase.Add(binding == true ? OidcBindingFlow.ManagementPath : "/auth/signin").Value + "?failed=true");
    }

    // Provider errors may contain credential or token material. Only the wrapper logs their type.
    private sealed class ConfiguredHandler(OpenIdConnectOptions options, HtmlEncoder htmlEncoder, UrlEncoder encoder)
        : OpenIdConnectHandler(new FixedOptions(options), NullLoggerFactory.Instance, htmlEncoder, encoder), IDisposable
    {
        public void Dispose() => Backchannel.Dispose();
    }

    private sealed class FixedOptions(OpenIdConnectOptions value) : IOptionsMonitor<OpenIdConnectOptions>
    {
        public OpenIdConnectOptions CurrentValue => value;
        public OpenIdConnectOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
    }
}
