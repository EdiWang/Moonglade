using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.FriendLink;

namespace Moonglade.Web.Handlers;

public class FoafMapHandler
{
    public static Delegate Handler => Handle;

    public static async Task Handle(
        HttpContext httpContext, IBlogConfig blogConfig, ICommandMediator commandMediator, IQueryMediator queryMediator, LinkGenerator linkGenerator)
    {
        var general = blogConfig.GeneralSettings ?? throw new InvalidOperationException("GeneralSettings is null.");

        var friends = await queryMediator.QueryAsync(new GetAllLinksQuery());
        var foafDoc = new FoafDoc
        (
            Name: general.OwnerName,
            BlogUrl: Helper.ResolveRootUrl(httpContext, general.CanonicalPrefix, true),
            Email: general.OwnerEmail,
            PhotoUrl: linkGenerator.GetUriByAction(httpContext, "Avatar", "Assets")
        );

        var requestUrl = GetUri(httpContext.Request).ToString();
        var xml = await commandMediator.SendAsync(new WriteFoafCommand(foafDoc, requestUrl, friends));

        httpContext.Response.Headers.Append("Cache-Control", "public, max-age=3600");
        httpContext.Response.ContentType = WriteFoafCommand.ContentType;

        await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);

        static Uri GetUri(HttpRequest request)
        {
            var host = request.Host.HasValue
                ? (request.Host.Value.Contains(',') ? "MULTIPLE-HOST" : request.Host.Value)
                : "UNKNOWN-HOST";
            return new Uri($"{request.Scheme}://{host}{request.Path}{request.QueryString}");
        }
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
