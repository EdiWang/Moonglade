using System.Net;

namespace Moonglade.Data.ExternalApi.IndexNow;

public interface IIndexNowClient
{
	Task<HttpStatusCode> SendRequestAsync(string urlToSubmit);
}
