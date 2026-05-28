using System.Net;
using System.Text;

namespace Moonglade.BackgroundServices.Tests;

public class GitHubReleaseClientTests
{
    [Fact]
    public async Task GetLatestReleaseTagAsync_ReturnsTagNameFromJson()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"tag_name\":\"v15.8.0\"}", Encoding.UTF8, "application/json")
        }))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var client = new GitHubReleaseClient(httpClient);

        var result = await client.GetLatestReleaseTagAsync(TestContext.Current.CancellationToken);

        Assert.Equal("v15.8.0", result);
    }

    [Fact]
    public async Task GetLatestReleaseTagAsync_MissingTagName_ThrowsInvalidOperationException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"Release\"}", Encoding.UTF8, "application/json")
        }))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var client = new GitHubReleaseClient(httpClient);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => client.GetLatestReleaseTagAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLatestReleaseTagAsync_NonSuccessStatus_ThrowsHttpRequestException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)))
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        var client = new GitHubReleaseClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetLatestReleaseTagAsync(TestContext.Current.CancellationToken));
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api.github.com/repos/EdiWang/Moonglade/releases/latest", request.RequestUri!.ToString());
            return Task.FromResult(response);
        }
    }
}
