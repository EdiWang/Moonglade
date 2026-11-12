#nullable enable

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Moonglade.Auth;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Tests.TestSupport;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Moonglade.Web.Tests.Hosting;

public class OwnerOidcHttpTests
{
    private const string Path = "/admin/account/oidc";
    private const string VerifyPath = Path + "/verify";
    private const string Secret = "disposable-oidc-secret";

    [Fact]
    public async Task Binding_UsesSavedProofAndRealOidcValidation_ThenRequiresExplicitConfirmation()
    {
        var ct = TestContext.Current.CancellationToken;
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        var owner = await SignInLocalAsync(fixture);
        var revision = await SaveAsync(fixture);

        using var challenge = await PostAsync(fixture, "Bind", revision);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var callback = provider.CallbackFor(challenge.Headers.Location!);
        using var returned = await fixture.SendAsync(HttpMethod.Get, callback);
        Assert.Equal(HttpStatusCode.Redirect, returned.StatusCode);
        Assert.Equal(Path, returned.Headers.Location!.OriginalString);
        Assert.Equal(1, provider.Exchanges);
        Assert.Null((await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct)).Subject);
        Assert.Equal(owner.Id.ToString(), fixture.ReadTicket(BlogAuthSchemas.Local).Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.False(fixture.ReadTicket(OidcBindingFlow.PendingScheme).Principal.IsInRole("Administrator"));

        using var confirm = await PostAsync(fixture, "Confirm", revision);
        Assert.Equal(HttpStatusCode.Redirect, confirm.StatusCode);
        var bound = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.Equal(owner.Id, bound.OwnerId);
        Assert.Equal(SyntheticProvider.Authority, bound.Issuer);
        Assert.Equal("external-subject", bound.Subject);
        Assert.NotEqual(Secret, bound.ProtectedSecret);
        Assert.Equal(owner, await fixture.Store.Accounts.ReadAsync(ct));
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("changed-session")]
    [InlineData("wrong-nonce")]
    [InlineData("stale-config")]
    public async Task Binding_CannotPersistFromAnInvalidOrAbandonedRoundTrip(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        var owner = await SignInLocalAsync(fixture);
        var revision = await SaveAsync(fixture);
        using var challenge = await PostAsync(fixture, "Bind", revision);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var callback = provider.CallbackFor(challenge.Headers.Location!, wrongNonce: scenario == "wrong-nonce");
        if (scenario == "stale-config") await SaveAsync(fixture, revision);
        using var returned = await fixture.SendAsync(HttpMethod.Get, callback);
        Assert.Equal(HttpStatusCode.Redirect, returned.StatusCode);

        if (scenario == "changed-session") fixture.SetTicket(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        if (scenario == "cancel")
        {
            using var cancel = await PostAsync(fixture, "Cancel", revision);
            Assert.Equal(HttpStatusCode.Redirect, cancel.StatusCode);
        }
        using var confirm = await PostAsync(fixture, "Confirm", revision);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        Assert.Null((await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct)).Subject);
        if (scenario == "stale-config") Assert.Equal(0, provider.Exchanges);
    }

    [Fact]
    public async Task Management_RequiresProof_AndDisableRotateUnbindAndDeletePreserveLocalAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        var owner = await SignInLocalAsync(fixture);
        var revision = await SaveAsync(fixture);
        using var challenge = await PostAsync(fixture, "Bind", revision);
        using var callback = await fixture.SendAsync(HttpMethod.Get, provider.CallbackFor(challenge.Headers.Location!));
        using var confirm = await PostAsync(fixture, "Confirm", revision);
        var row = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.NotNull(row.SessionRevision);

        using var noProof = await PostAsync(fixture, "Manage", row.Revision, ("operation", "disable"));
        Assert.Equal(HttpStatusCode.OK, noProof.StatusCode);
        Assert.True((await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct)).Enabled);
        using var disable = await ManageAsync(fixture, row.Revision, "disable");
        Assert.Equal(HttpStatusCode.Redirect, disable.StatusCode);
        var disabled = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.False(disabled.Enabled);
        Assert.Equal(row.Subject, disabled.Subject);
        Assert.NotEqual(row.SessionRevision, disabled.SessionRevision);

        var rotatedRevision = await SaveAsync(fixture, disabled.Revision, "rotated-disposable-secret");
        var rotated = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.NotEqual(disabled.ProtectedSecret, rotated.ProtectedSecret);
        Assert.Equal(disabled.SessionRevision, rotated.SessionRevision);
        using var enable = await ManageAsync(fixture, rotatedRevision, "enable");
        Assert.Equal(HttpStatusCode.Redirect, enable.StatusCode);
        var enabled = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.True(enabled.Enabled);
        Assert.Equal(row.Subject, enabled.Subject);
        Assert.NotEqual(disabled.SessionRevision, enabled.SessionRevision);

        using var unbind = await ManageAsync(fixture, enabled.Revision, "unbind");
        Assert.Equal(HttpStatusCode.Redirect, unbind.StatusCode);
        var unbound = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.Null(unbound.Subject);
        Assert.Null(unbound.SessionRevision);

        provider.ExpectedClientSecret = "rotated-disposable-secret";
        using var verify = await PostAsync(fixture, "Bind", unbound.Revision);
        var verification = verify.Headers.Location!.OriginalString;
        Assert.StartsWith(VerifyPath + "?", verification);
        using var rotatedChallenge = await VerifyAsync(fixture, verification, LocalAccountPageFixture.Password);
        using var rotatedCallback = await fixture.SendAsync(HttpMethod.Get, provider.CallbackFor(rotatedChallenge.Headers.Location!));
        using var rebound = await PostAsync(fixture, "Confirm", unbound.Revision);
        Assert.Equal(HttpStatusCode.Redirect, rebound.StatusCode);
        Assert.Equal(2, provider.Exchanges);
        var boundAgain = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        Assert.Equal(row.Subject, boundAgain.Subject);

        using var unconfirmedDelete = await ManageAsync(fixture, boundAgain.Revision, "delete");
        Assert.Equal(HttpStatusCode.OK, unconfirmedDelete.StatusCode);
        Assert.True(await fixture.Store.Db.OwnerOidc.AnyAsync(ct));
        using var delete = await ManageAsync(fixture, boundAgain.Revision, "delete", confirmDelete: true);
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(ct));
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Equal(owner, await fixture.Store.Accounts.ReadAsync(ct));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://localhost")]
    [InlineData("https://169.254.169.254")]
    [InlineData("https://[::ffff:10.10.10.10]")]
    public async Task Configuration_RejectsNonPublicHttpsAuthorities(string authority)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        await SignInLocalAsync(fixture);
        using var result = await PostAsync(fixture, "Save", null,
            ("DisplayName", "External"), ("Authority", authority), ("ClientId", "site"), ("ClientSecret", Secret),
            ("CurrentPassword", LocalAccountPageFixture.Password));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnreadableClientSecret_FailsBindingButLeavesLocalAccess()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        await SignInLocalAsync(fixture);
        var revision = await SaveAsync(fixture);
        await fixture.Store.Db.OwnerOidc.ExecuteUpdateAsync(
            setters => setters.SetProperty(row => row.ProtectedSecret, "unreadable"), ct);

        using var bind = await PostAsync(fixture, "Bind", revision);
        Assert.Equal(HttpStatusCode.Redirect, bind.StatusCode);
        Assert.Equal(Path + "?failed=true", bind.Headers.Location!.OriginalString);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Null((await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct)).Subject);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetupDraft_RequiresProofAndCannotBeUsedAfterCancellationOrSessionChange(bool changeSession)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var owner = await SignInLocalAsync(fixture);
        using var start = await PostAsync(fixture, "Save", null,
            ("DisplayName", "Synthetic provider"), ("Authority", SyntheticProvider.Authority), ("ClientId", "site"),
            ("ClientSecret", Secret));
        var verification = start.Headers.Location!.OriginalString;
        Assert.StartsWith(VerifyPath + "?", verification);
        Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(ct));

        using var rejected = await VerifyAsync(fixture, verification, "Incorrect123!");
        Assert.Equal(HttpStatusCode.OK, rejected.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(ct));
        var token = await fixture.FormTokenAsync(verification);
        if (changeSession)
        {
            fixture.SetTicket(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
            token = await fixture.FormTokenAsync(Path);
        }
        else
        {
            using var cancel = await VerifyAsync(fixture, verification, string.Empty, handler: "Cancel");
            Assert.Equal(Path, cancel.Headers.Location!.OriginalString);
        }

        using var stale = await VerifyAsync(fixture, verification, LocalAccountPageFixture.Password, token: token);
        Assert.StartsWith(Path + "?expired=", stale.Headers.Location!.OriginalString);
        Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(ct));
    }

    private static async Task<OwnerAccountSnapshot> SignInLocalAsync(LocalAccountHttpFixture fixture)
    {
        var ct = TestContext.Current.CancellationToken;
        var pending = await fixture.Store.Accounts.PrepareLocalSetupAsync(
            LocalAccountPageFixture.Email, LocalAccountPageFixture.Password, ct);
        var owner = await fixture.Store.Accounts.CompleteLocalSetupAsync(pending, null, ct);
        fixture.SetTicket(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        return owner;
    }

    private static async Task<Guid> SaveAsync(LocalAccountHttpFixture fixture, Guid? revision = null, string secret = Secret)
    {
        using var response = await PostAsync(fixture, "Save", revision,
            ("DisplayName", "Synthetic provider"), ("Authority", SyntheticProvider.Authority), ("ClientId", "site"),
            ("ClientSecret", secret), ("CurrentPassword", LocalAccountPageFixture.Password));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        if (revision is null)
        {
            var verification = response.Headers.Location!.OriginalString;
            Assert.StartsWith(VerifyPath + "?", verification);
            Assert.False(await fixture.Store.Db.OwnerOidc.AnyAsync(TestContext.Current.CancellationToken));
            using var verified = await VerifyAsync(fixture, verification, LocalAccountPageFixture.Password);
            Assert.Equal(Path, verified.Headers.Location!.OriginalString);
        }
        return (await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Revision;
    }

    private static async Task<HttpResponseMessage> VerifyAsync(LocalAccountHttpFixture fixture, string verification,
        string password, string? handler = null, string? token = null)
    {
        token ??= await fixture.FormTokenAsync(verification);
        var query = QueryHelpers.ParseQuery(new Uri("https://localhost" + verification).Query);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Draft"] = query.GetValueOrDefault("draft").ToString(),
            ["Revision"] = query.GetValueOrDefault("revision").ToString(),
            ["CurrentPassword"] = password
        });
        return await fixture.SendAsync(HttpMethod.Post, VerifyPath + (handler is null ? "" : "?handler=" + handler), content, token);
    }

    private static Task<HttpResponseMessage> ManageAsync(
        LocalAccountHttpFixture fixture, Guid revision, string operation, bool confirmDelete = false) =>
        PostAsync(fixture, "Manage", revision, ("operation", operation), ("ConfirmDelete", confirmDelete.ToString()),
            ("CurrentPassword", LocalAccountPageFixture.Password));

    private static async Task<HttpResponseMessage> PostAsync(
        LocalAccountHttpFixture fixture, string handler, Guid? revision, params (string Key, string Value)[] fields)
    {
        var token = await fixture.FormTokenAsync(Path);
        var form = fields.ToDictionary(field => field.Key, field => field.Value);
        form["Revision"] = revision?.ToString() ?? string.Empty;
        using var content = new FormUrlEncodedContent(form);
        return await fixture.SendAsync(HttpMethod.Post, Path + "?handler=" + handler, content, token);
    }

    private sealed class SyntheticProvider : HttpMessageHandler
    {
        public const string Authority = "https://93.184.215.14";
        private readonly RSA _key = RSA.Create(2048);
        private string? _nonce;
        private string? _codeChallenge;
        public int Exchanges { get; private set; }
        public string ExpectedClientSecret { get; set; } = Secret;

        public string CallbackFor(Uri redirect, bool wrongNonce = false)
        {
            Assert.Equal(Authority + "/authorize", redirect.GetLeftPart(UriPartial.Path));
            var query = QueryHelpers.ParseQuery(redirect.Query);
            _nonce = wrongNonce ? "invalid" : query["nonce"];
            _codeChallenge = query["code_challenge"];
            Assert.False(string.IsNullOrEmpty(_nonce));
            Assert.False(string.IsNullOrEmpty(_codeChallenge));
            return QueryHelpers.AddQueryString("/signin-oidc", new Dictionary<string, string?>
            {
                ["code"] = "synthetic-code", ["state"] = query["state"]
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal(Authority, request.RequestUri!.GetLeftPart(UriPartial.Authority));
            switch (request.RequestUri.AbsolutePath)
            {
                case "/.well-known/openid-configuration":
                    return Json(new Dictionary<string, string>
                    {
                        ["issuer"] = Authority, ["authorization_endpoint"] = Authority + "/authorize",
                        ["token_endpoint"] = Authority + "/token", ["jwks_uri"] = Authority + "/keys"
                    });
                case "/keys":
                    var key = _key.ExportParameters(false);
                    return Json(new Dictionary<string, object>
                    {
                        ["keys"] = new[]
                        {
                            new Dictionary<string, string>
                            {
                                ["kty"] = "RSA", ["kid"] = "test-key", ["use"] = "sig",
                                ["n"] = Base64UrlEncoder.Encode(key.Modulus), ["e"] = Base64UrlEncoder.Encode(key.Exponent)
                            }
                        }
                    });
                case "/token":
                    Exchanges++;
                    var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(ct));
                    Assert.Equal(ExpectedClientSecret, form["client_secret"].ToString());
                    Assert.Equal("site", form["client_id"].ToString());
                    Assert.Equal("synthetic-code", form["code"].ToString());
                    Assert.Equal(_codeChallenge, Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(form["code_verifier"].ToString()))));
                    var now = DateTime.UtcNow;
                    var token = new JwtSecurityToken(Authority, "site",
                        [new Claim("sub", "external-subject"), new Claim("name", "External account"), new Claim("nonce", _nonce!),
                            new Claim("iat", EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64)],
                        now.AddSeconds(-5), now.AddMinutes(5),
                        new SigningCredentials(new RsaSecurityKey(_key) { KeyId = "test-key" }, SecurityAlgorithms.RsaSha256));
                    return Json(new Dictionary<string, string>
                    {
                        ["id_token"] = new JwtSecurityTokenHandler().WriteToken(token),
                        ["access_token"] = "synthetic-access-token",
                        ["token_type"] = "Bearer"
                    });
                default:
                    throw new InvalidOperationException("Unexpected synthetic provider endpoint.");
            }
        }

        private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };

        protected override void Dispose(bool disposing)
        {
            if (disposing) _key.Dispose();
            base.Dispose(disposing);
        }
    }
}
