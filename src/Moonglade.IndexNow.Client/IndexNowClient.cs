using System.Net;
using Moonglade.Configuration;
using RestSharp;

namespace Moonglade.IndexNow.Client;

/// <summary>
/// IndexNow Implementation
/// Docs: https://www.indexnow.org/documentation
/// </summary>
public class IndexNowClient : IIndexNowClient
{
	private readonly IBlogConfig _config;

	public IndexNowClient(IBlogConfig config)
	{
		_config = config;
	}

	public async Task<HttpStatusCode> SendRequestAsync(Uri urlToSubmit)
	{
		string[] toPing = new[] { "api.indexnow.org", "www.bing.com", "search.seznam.cz", "yandex.com" };
		var host = urlToSubmit.Host;
		var apiKey = _config.GeneralSettings.IndexNowApiKey;

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
					urlToSubmit.ToString()
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
