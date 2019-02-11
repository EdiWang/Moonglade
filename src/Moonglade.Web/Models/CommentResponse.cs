namespace Moonglade.Web.Models
{
    public class CommentResponse
    {
        public bool IsSuccess { get; set; }

        public CommentResponseCode ResponseCode { get; set; }

        public CommentResponse(bool isSuccess, CommentResponseCode responseCode)
        {
            IsSuccess = isSuccess;
            ResponseCode = responseCode;
        }
    }

    public enum CommentResponseCode
    {
        Success,
        UnknownError,
        WrongCaptcha,
        EmailDomainBlocked,
        CommentDisabled,
        InvalidModel
    }
}
