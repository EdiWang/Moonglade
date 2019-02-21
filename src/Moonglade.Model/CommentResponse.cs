namespace Moonglade.Model
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
}
