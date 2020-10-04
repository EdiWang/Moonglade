using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
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

        public NotificationSettings NotificationSettings { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        public FriendLinksSettings FriendLinksSettings { get; set; }

        public AdvancedSettings AdvancedSettings { get; set; }

        public SecuritySettings SecuritySettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig(
            ILogger<BlogConfig> logger,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;

            ContentSettings = new ContentSettings();
            GeneralSettings = new GeneralSettings();
            NotificationSettings = new NotificationSettings();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();
            FriendLinksSettings = new FriendLinksSettings();
            AdvancedSettings = new AdvancedSettings();
            SecuritySettings = new SecuritySettings();

            Initialize();
        }

        private void Initialize()
        {
            if (_hasInitialized) return;

            var cfgDic = GetAllConfigurations();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            GeneralSettings = JsonSerializer.Deserialize<GeneralSettings>(cfgDic[nameof(GeneralSettings)], jsonOptions);
            ContentSettings = JsonSerializer.Deserialize<ContentSettings>(cfgDic[nameof(ContentSettings)], jsonOptions);
            NotificationSettings = JsonSerializer.Deserialize<NotificationSettings>(cfgDic[nameof(NotificationSettings)], jsonOptions);
            FeedSettings = JsonSerializer.Deserialize<FeedSettings>(cfgDic[nameof(FeedSettings)], jsonOptions);
            WatermarkSettings = JsonSerializer.Deserialize<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)], jsonOptions);
            FriendLinksSettings = JsonSerializer.Deserialize<FriendLinksSettings>(cfgDic[nameof(FriendLinksSettings)], jsonOptions);
            AdvancedSettings = JsonSerializer.Deserialize<AdvancedSettings>(cfgDic[nameof(AdvancedSettings)], jsonOptions);
            SecuritySettings = JsonSerializer.Deserialize<SecuritySettings>(cfgDic[nameof(SecuritySettings)], jsonOptions);

            _hasInitialized = true;
        }

        public async Task SaveConfigurationAsync<T>(T blogSettings) where T : BlogSettings
        {
            async Task SetConfiguration(string key, string value)
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);
                var sql = $"UPDATE {nameof(BlogConfiguration)} " +
                          $"SET {nameof(BlogConfiguration.CfgValue)} = @value, " +
                          $"{nameof(BlogConfiguration.LastModifiedTimeUtc)} = @lastModifiedTimeUtc " +
                          $"WHERE {nameof(BlogConfiguration.CfgKey)} = @key";

                await conn.ExecuteAsync(sql, new { key, value, lastModifiedTimeUtc = DateTime.UtcNow });
            }

            try
            {
                var json = JsonSerializer.Serialize(blogSettings);
                await SetConfiguration(typeof(T).Name, json);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
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