using System.Net;
using Moonglade.Configuration;
using Moonglade.Data.ExternalApi.IndexNow;
using RestSharp;

namespace Moonglade.Data.ExternalAPI.IndexNow
{
	/// <summary>
	/// IndexNow Implementation
	/// Docs: https://www.indexnow.org/documentation
	/// </summary>
	public class IndexNowClient : IIndexNowClient
	{
		private IBlogConfig _config;

		public IndexNowClient(IBlogConfig config)
		{
			_config = config;
		}

		public async Task<HttpStatusCode> SendRequestAsync(string urlToSubmit)
		{
			string[] toPing = new[] { "api.indexnow.org", "www.bing.com", "search.seznam.cz", "yandex.com" };
			var host = new Uri(urlToSubmit).Host;
			var apiKey = _config.GeneralSettings.IndexNowAPIKey;

			foreach (var ping in toPing)
			{
				var client = new RestClient($"https://{ping}");
				var request = new RestRequest("/indexnow", Method.Post);

				request.AddHeader("ContentType", "application/json");
				request.AddHeader("Host", ping);

				var bodyobject = new
				{
					host = host,
					key = apiKey,
					keyLocation = $"https://{host}/{apiKey}.txt",
					urlList = new List<string>
					{
						urlToSubmit
					}
				};

				request.AddBody(bodyobject);

				try
				{
					var response = await client.ExecuteAsync(request);
					return response.StatusCode;
				}
				catch
				{
					throw new Exception("Request failed. See logs.");
				}
			}
			return HttpStatusCode.BadRequest;
		}
	}
}
