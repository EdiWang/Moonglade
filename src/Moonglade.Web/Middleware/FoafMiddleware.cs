using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration;
using Moonglade.FriendLink;
using Moonglade.Utils;
using Moonglade.Web.Models;

namespace Moonglade.Web.Middleware
{
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
            IFoafWriter foafWriter,
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
                var xml = await foafWriter.GetFoafData(foafDoc, requestUrl, friends);

                //[ResponseCache(Duration = 3600)]
                context.Response.ContentType = FoafWriter.ContentType;
                await context.Response.WriteAsync(xml, context.RequestAborted);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
