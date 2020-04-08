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

        public GeneralSettings GeneralSettings { get; set; }

        public ContentSettings ContentSettings { get; set; }

        public EmailSettings EmailSettings { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        public FriendLinksSettings FriendLinksSettings { get; set; }

        public AdvancedSettings AdvancedSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig(
            ILogger<BlogConfig> logger,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;

            ContentSettings = new ContentSettings();
            GeneralSettings = new GeneralSettings();
            EmailSettings = new EmailSettings();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();
            FriendLinksSettings = new FriendLinksSettings();
            AdvancedSettings = new AdvancedSettings();

            Initialize();
        }

        private void Initialize()
        {
            if (_hasInitialized) return;

            var cfgDic = GetAllConfigurations();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            GeneralSettings = JsonSerializer.Deserialize<GeneralSettings>(cfgDic[nameof(GeneralSettings)], jsonOptions);
            ContentSettings = JsonSerializer.Deserialize<ContentSettings>(cfgDic[nameof(ContentSettings)], jsonOptions);
            EmailSettings = JsonSerializer.Deserialize<EmailSettings>(cfgDic[nameof(EmailSettings)], jsonOptions);
            FeedSettings = JsonSerializer.Deserialize<FeedSettings>(cfgDic[nameof(FeedSettings)], jsonOptions);
            WatermarkSettings = JsonSerializer.Deserialize<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)], jsonOptions);
            FriendLinksSettings = JsonSerializer.Deserialize<FriendLinksSettings>(cfgDic[nameof(FriendLinksSettings)], jsonOptions);
            AdvancedSettings = JsonSerializer.Deserialize<AdvancedSettings>(cfgDic[nameof(AdvancedSettings)], jsonOptions);

            _hasInitialized = true;
        }

        public async Task<Response> SaveConfigurationAsync<T>(T moongladeSettings) where T : MoongladeSettings
        {
            async Task<int> SetConfiguration(string key, string value)
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);
                var sql = $"UPDATE {nameof(BlogConfiguration)} " +
                          $"SET {nameof(BlogConfiguration.CfgValue)} = @value, " +
                          $"{nameof(BlogConfiguration.LastModifiedTimeUtc)} = @lastModifiedTimeUtc " +
                          $"WHERE {nameof(BlogConfiguration.CfgKey)} = @key";

                return await conn.ExecuteAsync(sql, new { key, value, lastModifiedTimeUtc = DateTime.UtcNow });
            }

            try
            {
                var json = JsonSerializer.Serialize(moongladeSettings);
                var rows = await SetConfiguration(typeof(T).Name, json);
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
                using var conn = new SqlConnection(connStr);
                var sql = $"SELECT {nameof(BlogConfiguration.CfgKey)}, " +
                          $"{nameof(BlogConfiguration.CfgValue)} " +
                          $"FROM {nameof(BlogConfiguration)}";

                var data = conn.Query<(string CfgKey, string CfgValue)>(sql);
                var dic = data.ToDictionary(c => c.CfgKey, c => c.CfgValue);
                return dic;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, $"Error {nameof(GetAllConfigurations)}");
                throw;
            }
        }
    }
}