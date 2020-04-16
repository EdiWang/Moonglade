namespace Moonglade.Model
{
    public enum ResponseFailureCode
    {
        None = 0,
        GeneralException = 1,
        InvalidParameter = 3,
        InvalidModelState = 4,
        ApiError = 5,

        // post
        PostNotFound = 100,

        // comment
        CommentDisabled = 200,
        CommentNotFound = 201,
        EmailDomainBlocked = 202,

        // tag
        TagNotFound = 300,

        // category
        CategoryNotFound = 400,

        // email
        EmailSendingDisabled = 500,

        // pingback
        PingbackRecordNotFound = 600,

        // friendlink
        FriendLinkNotFound = 800
    }
}