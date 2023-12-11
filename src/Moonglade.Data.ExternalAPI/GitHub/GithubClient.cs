using Moonglade.Configuration;

using RestSharp;

using ContentType = RestSharp.ContentType;

namespace Moonglade.Data.ExternalAPI.GitHub
{
    public class GithubClient : IGithubClient
    {
        public IBlogConfig _config;


        /// <summary>
        /// Initializes a new instance of the <see cref="GithubClient"/> class.
        /// </summary>
        /// <param name="config">The configuration object that contains settings for the Github client.</param>
        public GithubClient(IBlogConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Helper function to send an HTTP request through the requests.
        /// </summary>
        /// <returns>
        /// The response from the HTTP request.
        /// </returns>
        /// <param name="method">HTTP request method (POST, GET, PUT, DELETE, etc.)</param>
        /// <param name="endpoint">Endpoint for the request.</param>
        /// <param name="body">Payload for the request (if it applies).</param>
        /// <exception cref="Exception">Throwed in case of not getting a HttpStatusCode.OK.</exception>
        public async Task<RestResponse> SendRequest(Method method, string endpoint, string? body)
        {
            var baseUrl = $"https://api.github.com";
            var bodyContentType = "application/vnd.github+json";
            var finalurl = endpoint + "?Accept=" + bodyContentType + "&Authorization Bearer=" + _config.GeneralSettings.GithubPAT + "&X-GitHub-Api-Version=2022-11-28";

            var source = new CancellationTokenSource();
            var token = source.Token;

            var client = new RestClient(baseUrl);
            var request = new RestRequest(finalurl, method);

            request.AddHeader("Accept", bodyContentType);
            request.AddHeader("Authorization", "Bearer " + _config.GeneralSettings.GithubPAT);
            request.AddHeader("X-GitHub-Api-Version", "2022-11-28");

            if (method == Method.Post || method == Method.Patch)
            {
                if (body != null)
                {
                    request.AddStringBody(body, ContentType.Json);
                }
            }

            try
            {
                var response = await client.ExecuteGetAsync(request, token);
                return response;
            }
            catch
            {
                throw new Exception("Request failed. See logs.");
            }
        }
    }
}
