using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Model;
using Newtonsoft.Json;

namespace Moonglade.AzureApplicationInsights
{
    public class MetricsReader
    {
        public string AzureAppInsightApiEndpointAddress { get; set; }
        public string AzureAppInsightAppId { get; set; }
        public string AzureAppInsightApiKey { get; set; }

        public MetricsReader(string azureAppInsightApiEndpointAddress, string azureAppInsightAppId, string azureAppInsightApiKey)
        {
            AzureAppInsightApiEndpointAddress = azureAppInsightApiEndpointAddress;
            AzureAppInsightAppId = azureAppInsightAppId;
            AzureAppInsightApiKey = azureAppInsightApiKey;
        }

        public async Task<dynamic> GetP1DMetrics(string query, string aggregation)
        {
            var response = await GetAppInsightDataAsync(query, $"timespan=P1D&aggregation={aggregation}");
            if (response.IsSuccess)
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(response.Item);
                return obj;
            }

            return null;
        }

        public async Task<Response<string>> GetAppInsightDataAsync(string queryPath, string parameterString)
        {
            return await GetAppInsightDataAsync(AzureAppInsightAppId, AzureAppInsightApiKey, "metrics", queryPath, parameterString);
        }

        private async Task<Response<string>> GetAppInsightDataAsync
            (string appid, string apikey, string queryType, string queryPath, string parameterString)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", apikey);

                var req = string.Format(AzureAppInsightApiEndpointAddress, appid, queryType, queryPath, parameterString);
                var response = client.GetAsync(req).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return new FailedResponse<string>((int)ResponseFailureCode.RemoteApiFailure)
                    {
                        Message = response.ReasonPhrase
                    };
                }

                var result = await response.Content.ReadAsStringAsync();
                return new SuccessResponse<string>(result);
            }
        }
    }
}
