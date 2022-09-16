using Moonglade.FriendLink;

namespace Moonglade.Web.Middleware;

public class FoafMiddleware
{
    private readonly RequestDelegate _next;

    public FoafMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext context,
        IBlogConfig blogConfig,
        IMediator mediator,
        LinkGenerator linkGenerator)
    {
        if (context.Request.Path == "/foaf.xml")
        {
            static Uri GetUri(HttpRequest request)
            {
                return new(string.Concat(
                    request.Scheme,
                    "://",
                    request.Host.HasValue
                        ? (request.Host.Value.IndexOf(",", StringComparison.Ordinal) > 0
                            ? "MULTIPLE-HOST"
                            : request.Host.Value)
                        : "UNKNOWN-HOST",
                    request.Path.HasValue ? request.Path.Value : string.Empty,
                    request.QueryString.HasValue ? request.QueryString.Value : string.Empty));
            }

            var friends = await mediator.Send(new GetAllLinksQuery());
            var foafDoc = new FoafDoc
            {
                Name = blogConfig.GeneralSettings.OwnerName,
                BlogUrl = Helper.ResolveRootUrl(context, blogConfig.GeneralSettings.CanonicalPrefix, true),
                Email = blogConfig.GeneralSettings.OwnerEmail,
                PhotoUrl = linkGenerator.GetUriByAction(context, "Avatar", "Assets")
            };
            var requestUrl = GetUri(context.Request).ToString();
            var xml = await mediator.Send(new WriteFoafCommand(foafDoc, requestUrl, friends));

            //[ResponseCache(Duration = 3600)]
            context.Response.ContentType = WriteFoafCommand.ContentType;
            await context.Response.WriteAsync(xml, context.RequestAborted);
        }
        else
        {
            await _next(context);
        }
    }
}

public record FoafDoc
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string BlogUrl { get; set; }

    public string PhotoUrl { get; set; }
}

public record FoafPerson(string Id)
{
    public string Birthday { get; set; } = string.Empty;
    public string Blog { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public List<FoafPerson> Friends { get; set; }
    public string Homepage { get; set; } = string.Empty;
    public string Id { get; set; } = Id;
    public string Image { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public string Rdf { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}