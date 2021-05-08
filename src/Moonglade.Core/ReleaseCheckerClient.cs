using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moonglade.Utils;
using Polly;

namespace Moonglade.Core
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNotificationClient(this IServiceCollection services)
        {
            services.AddHttpClient<IReleaseCheckerClient, IReleaseCheckerClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
        }
    }

    public interface IReleaseCheckerClient
    {

    }

    public class ReleaseCheckerClient : IReleaseCheckerClient
    {
        private readonly HttpClient _httpClient;

        public ReleaseCheckerClient(IConfiguration configuration, HttpClient httpClient)
        {
            var apiAddress = configuration["ReleaseCheckApiAddress"];
            if (string.IsNullOrWhiteSpace(apiAddress) ||
                !Uri.IsWellFormedUriString(apiAddress, UriKind.RelativeOrAbsolute))
            {
                throw new InvalidOperationException($"'{apiAddress}' is not a valid API address.");
            }

            httpClient.BaseAddress = new(apiAddress);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Helper.AppVersion}");

            _httpClient = httpClient;
        }

        public async Task CheckNewRelease()
        {

        }
    }
}
