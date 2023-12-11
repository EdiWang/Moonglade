using System.Net;

namespace Moonglade.Data.ExternalAPI.IndexNow;

public interface IIndexNowClient
{
	Task<HttpStatusCode> SendRequestAsync(string urlToSubmit);
}
