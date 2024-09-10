using System.Net;

namespace Moonglade.IndexNow.Client
{
	public interface IIndexNowClient
	{
		Task<HttpStatusCode> SendRequestAsync(string urlToSubmit);
	}
}
