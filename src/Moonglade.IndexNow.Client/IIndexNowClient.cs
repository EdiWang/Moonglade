using System.Net;

namespace Moonglade.IndexNow.Client;

public interface IIndexNowClient
{
    Task SendRequestAsync(Uri url);
}