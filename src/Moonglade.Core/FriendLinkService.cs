using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class FriendLinkService : MoongladeService
    {
        public FriendLinkService(MoongladeDbContext context,
            ILogger<FriendLinkService> logger, IOptions<AppSettings> settings) : base(context, logger, settings)
        {
        }

        public Response<FriendLink> GetFriendLink(Guid id)
        {
            try
            {
                var item = Context.FriendLink.Find(id);
                return new SuccessResponse<FriendLink>(item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetFriendLink)}");
                return new FailedResponse<FriendLink>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<List<FriendLink>> GetAllFriendLinks()
        {
            try
            {
                var item = Context.FriendLink.ToList();
                return new SuccessResponse<List<FriendLink>>(item);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetAllFriendLinks)}");
                return new FailedResponse<List<FriendLink>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response AddFriendLink(string title, string linkUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(title)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(linkUrl))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(linkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(linkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(linkUrl)} is not a valid url.");
                }

                var fdLink = new FriendLink
                {
                    Id = Guid.NewGuid(),
                    LinkUrl = linkUrl,
                    Title = title
                };

                Context.FriendLink.Add(fdLink);
                var rows = Context.SaveChanges();
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(AddFriendLink)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response DeleteFriendLink(Guid id)
        {
            try
            {
                var fdlink = Context.FriendLink.Find(id);
                if (null != fdlink)
                {
                    Context.FriendLink.Remove(fdlink);
                    var rows = Context.SaveChanges();
                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.FriendLinkNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(DeleteFriendLink)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response UpdateFriendLink(Guid id, string newTitle, string newLinkUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newTitle)} can not be empty.");
                }

                if (string.IsNullOrWhiteSpace(newLinkUrl))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newLinkUrl)} can not be empty.");
                }

                if (!Uri.IsWellFormedUriString(newLinkUrl, UriKind.Absolute))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, $"{nameof(newLinkUrl)} is not a valid url.");
                }

                var fdlink = Context.FriendLink.Find(id);
                if (null != fdlink)
                {
                    fdlink.Title = newTitle;
                    fdlink.LinkUrl = newLinkUrl;

                    var rows = Context.SaveChanges();
                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.FriendLinkNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(UpdateFriendLink)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }
    }
}
