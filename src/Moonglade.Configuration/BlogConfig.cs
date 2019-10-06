using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Edi.Practice.RequestResponseModel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;

namespace Moonglade.Configuration
{
    public class BlogConfig : IBlogConfig
    {
        private readonly ILogger<BlogConfig> _logger;

        private readonly IConfiguration _configuration;

        public BlogOwnerSettings BlogOwnerSettings { get; set; }

        public GeneralSettings GeneralSettings { get; set; }

        public ContentSettings ContentSettings { get; set; }

        public EmailSettings EmailSettings { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig(
            ILogger<BlogConfig> logger,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;

            BlogOwnerSettings = new BlogOwnerSettings();
            ContentSettings = new ContentSettings();
            GeneralSettings = new GeneralSettings();
            EmailSettings = new EmailSettings();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();

            Initialize();
        }

        private void Initialize()
        {
            if (!_hasInitialized)
            {
                var cfgDic = GetAllConfigurations();

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                BlogOwnerSettings = JsonSerializer.Deserialize<BlogOwnerSettings>(cfgDic[nameof(BlogOwnerSettings)], jsonOptions);
                GeneralSettings = JsonSerializer.Deserialize<GeneralSettings>(cfgDic[nameof(GeneralSettings)], jsonOptions);
                ContentSettings = JsonSerializer.Deserialize<ContentSettings>(cfgDic[nameof(ContentSettings)], jsonOptions);
                EmailSettings = JsonSerializer.Deserialize<EmailSettings>(cfgDic[nameof(EmailSettings)], jsonOptions);
                FeedSettings = JsonSerializer.Deserialize<FeedSettings>(cfgDic[nameof(FeedSettings)], jsonOptions);
                WatermarkSettings = JsonSerializer.Deserialize<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)], jsonOptions);

                _hasInitialized = true;
            }
        }

        public async Task<Response> SaveConfigurationAsync<T>(T moongladeSettings) where T : MoongladeSettings
        {
            async Task<int> SetConfiguration(string key, string value)
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                using (var conn = new SqlConnection(connStr))
                {
                    string sql = $"UPDATE {nameof(BlogConfiguration)} " +
                                 $"SET {nameof(BlogConfiguration.CfgValue)} = @value, {nameof(BlogConfiguration.LastModifiedTimeUtc)} = @lastModifiedTimeUtc " +
                                 $"WHERE {nameof(BlogConfiguration.CfgKey)} = @key";

                    return await conn.ExecuteAsync(sql, new { key, value, lastModifiedTimeUtc = DateTime.UtcNow });
                }
            }

            try
            {
                var json = JsonSerializer.Serialize(moongladeSettings);
                int rows = await SetConfiguration(typeof(T).Name, json);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public void RequireRefresh()
        {
            _hasInitialized = false;
        }

        private IDictionary<string, string> GetAllConfigurations()
        {
            try
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                using (var conn = new SqlConnection(connStr))
                {
                    string sql = $"SELECT CfgKey, CfgValue FROM {nameof(BlogConfiguration)}";
                    var data = conn.Query<(string CfgKey, string CfgValue)>(sql);
                    var dic = data.ToDictionary(c => c.CfgKey, c => c.CfgValue);
                    return dic;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Error {nameof(GetAllConfigurations)}");
                throw;
            }
        }
    }
}