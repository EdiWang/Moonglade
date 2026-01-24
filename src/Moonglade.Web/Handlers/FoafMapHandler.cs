using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http.Extensions;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting;
using Moonglade.Widgets;
using Moonglade.Widgets.Types;
using System.Text.Json;

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
            var widgets = await queryMediator.QueryAsync(new ListWidgetsQuery());
            var linksJson = widgets.Where(p => p.WidgetType == WidgetType.LinkList && !string.IsNullOrWhiteSpace(p.ContentCode)).Select(p => p.ContentCode).ToList();

            var links = new List<LinkListItem>();
            foreach (var json in linksJson)
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<LinkListItem>>(json, MoongladeJsonSerializerOptions.Default);
                    if (items != null) links.AddRange(items);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize link list widget content, skipping.");
                }
            }

            var foafDoc = new FoafDoc(
                Name: general.OwnerName,
                BlogUrl: UrlHelper.ResolveRootUrl(httpContext, general.CanonicalPrefix, preferCanonical: true),
                Email: general.OwnerEmail,
                PhotoUrl: linkGenerator.GetUriByAction(httpContext, "Avatar", "Assets") ?? string.Empty
            );

            // Use DisplayUrl for more robust URL representation
            var requestUrl = httpContext.Request.GetDisplayUrl();
            var xml = await commandMediator.SendAsync(new WriteFoafCommand(foafDoc, requestUrl, links));

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
    public string LastName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string PhotoUrl { get; init; } = string.Empty;
    public string Rdf { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}
