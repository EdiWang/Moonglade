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
        Success = 100,
        SuccessNonReview = 101,
        UnknownError = 200,
        WrongCaptcha = 300,
        EmailDomainBlocked = 400,
        CommentDisabled = 500,
        InvalidModel = 600
    }
}