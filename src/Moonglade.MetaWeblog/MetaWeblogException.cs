namespace Moonglade.MetaWeblog;

public class MetaWeblogException : Exception
{
    public int Code { get; private set; }

    public MetaWeblogException(string message, int code = 1) : base(message)
    {
        Code = code;
    }
}