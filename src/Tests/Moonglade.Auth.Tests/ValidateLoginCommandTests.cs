using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;

namespace Moonglade.Auth.Tests;

public class ValidateLoginCommandTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthenticateLocalAsync_AbsentOrProfileOnly_RejectsLegacyCredentials(bool profileOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        const string legacy = """{"username":"admin","passwordHash":"inert-private-marker","passwordSalt":"inert-salt"}""";
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "LocalAccountSettings", CfgValue = legacy });
        await store.Db.SaveChangesAsync(ct);
        if (profileOnly) await store.Accounts.CreateProfileAsync(OwnerAccountTestStore.Email, ct);

        Assert.Null(await store.Accounts.AuthenticateLocalAsync("admin", "admin123", ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct));
        Assert.Equal(legacy, (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthenticateLocalAsync_CurrentEmailAndPassword_ReturnsStableIndependentIdentity(bool whitespace)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var initialized = await store.InitializeAsync(ct);

        var result = await store.Accounts.AuthenticateLocalAsync(
            whitespace ? " owner@EXAMPLE.test " : OwnerAccountTestStore.Email,
            whitespace ? $" {OwnerAccountTestStore.Password} " : OwnerAccountTestStore.Password, ct);

        Assert.Equal(initialized, result);
        Assert.Equal(OwnerAccountTestStore.Email, result!.Email);
        Assert.True(result.IsLocalInitialized);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthenticateLocalAsync_WrongEmailOrPassword_DoesNotAuthenticateOrMutateAccount(bool wrongEmail)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var initialized = await store.InitializeAsync(ct);

        var result = await store.Accounts.AuthenticateLocalAsync(
            wrongEmail ? "other@example.test" : OwnerAccountTestStore.Email,
            wrongEmail ? OwnerAccountTestStore.Password : "WrongPassword123!", ct);

        Assert.Null(result);
        Assert.Equal(initialized, await store.Accounts.ReadAsync(ct));
    }

    [Fact]
    public async Task AuthenticateLocalAsync_MalformedLegacyRow_DoesNotOverrideIndependentAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var initialized = await store.InitializeAsync(ct);
        store.Db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "LocalAccountSettings", CfgValue = "not-json-private-marker" });
        await store.Db.SaveChangesAsync(ct);

        Assert.Equal(initialized, await store.Accounts.AuthenticateLocalAsync(OwnerAccountTestStore.Email, OwnerAccountTestStore.Password, ct));
        Assert.Null(await store.Accounts.AuthenticateLocalAsync("admin", "admin123", ct));
        Assert.Equal("not-json-private-marker", (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);
    }
}
