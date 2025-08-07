using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.FriendLink;

namespace Moonglade.Web.Handlers;

public class FoafMapHandler
{
    public static Delegate Handler => Handle;

    public static async Task Handle(
        ILogger<FoafMapHandler> logger,
        HttpContext httpContext,
        IBlogConfig blogConfig,
        ICommandMediator commandMediator,
        IQueryMediator queryMediator,
        LinkGenerator linkGenerator)
    {
        var general = blogConfig.GeneralSettings ?? throw new InvalidOperationException("GeneralSettings is null.");

        try
        {
            var friends = await queryMediator.QueryAsync(new GetAllLinksQuery());

            var foafDoc = new FoafDoc(
                Name: general.OwnerName,
                BlogUrl: Helper.ResolveRootUrl(httpContext, general.CanonicalPrefix, preferCanonical: true),
                Email: general.OwnerEmail,
                PhotoUrl: linkGenerator.GetUriByAction(httpContext, "Avatar", "Assets") ?? string.Empty
            );

            var requestUrl = GetRequestUri(httpContext.Request).ToString();
            var xml = await commandMediator.SendAsync(new WriteFoafCommand(foafDoc, requestUrl, friends));

            SetResponseHeaders(httpContext.Response);
            await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "An error occurred while generating FOAF document.");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsync("An error occurred while generating FOAF document.", httpContext.RequestAborted);
        }
    }

    private static void SetResponseHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "public, max-age=3600";
        response.ContentType = WriteFoafCommand.ContentType;
    }

    private static Uri GetRequestUri(HttpRequest request)
    {
        var host = GetSafeHost(request);
        var scheme = request.Scheme;
        var path = request.Path.Value ?? string.Empty;
        var queryString = request.QueryString.Value ?? string.Empty;

        return new Uri($"{scheme}://{host}{path}{queryString}");
    }

    private static string GetSafeHost(HttpRequest request)
    {
        if (!request.Host.HasValue)
        {
            return "localhost"; // Fallback to localhost instead of "UNKNOWN-HOST"
        }

        var hostValue = request.Host.Value;

        // Handle multiple hosts by taking the first one
        if (hostValue.Contains(','))
        {
            var firstHost = hostValue.Split(',')[0].Trim();
            return string.IsNullOrWhiteSpace(firstHost) ? "localhost" : firstHost;
        }

        return hostValue;
    }
}

public record FoafDoc(
    string Name,
    string BlogUrl,
    string Email,
    string PhotoUrl
);

public record FoafPerson(string Id)
{
    public string Birthday { get; init; } = string.Empty;
    public string Blog { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public List<FoafPerson> Friends { get; init; } = new();
    public string Homepage { get; init; } = string.Empty;
    public string Image { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string PhotoUrl { get; init; } = string.Empty;
    public string Rdf { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}
