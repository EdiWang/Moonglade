using MediatR;
using Moonglade.FriendLink;

namespace Moonglade.Web.Middleware;

public class FoafMiddleware
{
    private readonly RequestDelegate _next;

    public FoafMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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
            var xml = await mediator.Send(new WriterFoafCommand(foafDoc, requestUrl, friends));

            //[ResponseCache(Duration = 3600)]
            context.Response.ContentType = WriterFoafCommand.ContentType;
            await context.Response.WriteAsync(xml, context.RequestAborted);
        }
        else
        {
            await _next(context);
        }
    }
}

public class FoafDoc
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string BlogUrl { get; set; }

    public string PhotoUrl { get; set; }
}

public class FoafPerson
{
    public FoafPerson(string id)
    {
        Birthday = string.Empty;
        Blog = string.Empty;
        Email = string.Empty;
        FirstName = string.Empty;
        Homepage = string.Empty;
        Image = string.Empty;
        LastName = string.Empty;
        Name = string.Empty;
        Phone = string.Empty;
        PhotoUrl = string.Empty;
        Rdf = string.Empty;
        Title = string.Empty;
        Id = id;
    }

    public FoafPerson(
        string id,
        string name,
        string title,
        string email,
        string homepage,
        string blog,
        string rdf,
        string firstName,
        string lastName,
        string image,
        string birthday,
        string phone)
    {
        PhotoUrl = string.Empty;
        Id = id;
        Name = name;
        Title = title;
        Email = email;
        Homepage = homepage;
        Blog = blog;
        Rdf = rdf;
        FirstName = firstName;
        LastName = lastName;
        Image = image;
        Birthday = birthday;
        Phone = phone;
    }

    public string Birthday { get; set; }
    public string Blog { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public List<FoafPerson> Friends { get; set; }
    public string Homepage { get; set; }
    public string Id { get; set; }
    public string Image { get; set; }
    public string LastName { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
    public string PhotoUrl { get; set; }
    public string Rdf { get; set; }
    public string Title { get; set; }
}