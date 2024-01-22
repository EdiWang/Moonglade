namespace Moonglade.MetaWeblog;

public class MetaWeblogException(string message, int code = 1) : Exception(message)
{
    public int Code { get; private set; } = code;
}