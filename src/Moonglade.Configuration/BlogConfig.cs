using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Model;
using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class BlogConfig : IBlogConfig
    {
        private readonly ILogger<BlogConfig> _logger;

        private IConfiguration Configuration { get; }

        private readonly IAesEncryptionService _encryptionService;

        public BlogOwnerSettings BlogOwnerSettings { get; set; }

        public GeneralSettings GeneralSettings { get; set; }

        public ContentSettings ContentSettings { get; set; }

        public EmailConfiguration EmailConfiguration { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig(
            ILogger<BlogConfig> logger,
            IAesEncryptionService encryptionService,
            IConfiguration configuration)
        {
            _encryptionService = encryptionService;
            Configuration = configuration;
            _logger = logger;

            BlogOwnerSettings = new BlogOwnerSettings();
            ContentSettings = new ContentSettings();
            GeneralSettings = new GeneralSettings();
            EmailConfiguration = new EmailConfiguration();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();
        }

        public void Initialize()
        {
            if (!_hasInitialized)
            {
                var cfgDic = GetAllConfigurations();

                BlogOwnerSettings = JsonConvert.DeserializeObject<BlogOwnerSettings>(cfgDic[nameof(BlogOwnerSettings)]);
                GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(cfgDic[nameof(GeneralSettings)]);
                ContentSettings = JsonConvert.DeserializeObject<ContentSettings>(cfgDic[nameof(ContentSettings)]);

                EmailConfiguration = JsonConvert.DeserializeObject<EmailConfiguration>(cfgDic[nameof(EmailConfiguration)]);
                if (!string.IsNullOrWhiteSpace(EmailConfiguration.SmtpPassword))
                {
                    EmailConfiguration.SmtpClearPassword = DecryptPassword(EmailConfiguration.SmtpPassword);
                }

                FeedSettings = JsonConvert.DeserializeObject<FeedSettings>(cfgDic[nameof(FeedSettings)]);
                WatermarkSettings = JsonConvert.DeserializeObject<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)]);

                _hasInitialized = true;
            }
        }

        public Response SaveConfiguration<T>(T moongladeSettings) where T : MoongladeSettings
        {
            void SetConfiguration(string key, string value)
            {
                var connStr = Configuration.GetConnectionString(Constants.DbConnectionName);
                using (var conn = new SqlConnection(connStr))
                {
                    string sql = $"UPDATE {nameof(BlogConfiguration)} " +
                                 $"SET {nameof(BlogConfiguration.CfgValue)} = @value, {nameof(BlogConfiguration.LastModifiedTimeUtc)} = @lastModifiedTimeUtc " +
                                 $"WHERE {nameof(BlogConfiguration.CfgKey)} = @key";

                    conn.Execute(sql, new { key, value, lastModifiedTimeUtc = DateTime.UtcNow });
                }
            }

            try
            {
                var json = moongladeSettings.GetJson();
                SetConfiguration(typeof(T).Name, json);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public string EncryptPassword(string clearPassword)
        {
            var str = _encryptionService.Encrypt(clearPassword);
            return str;
        }

        public void RequireRefresh()
        {
            _hasInitialized = false;
        }

        private string DecryptPassword(string encryptedPassword)
        {
            var str = _encryptionService.Decrypt(encryptedPassword);
            return str;
        }

        private IDictionary<string, string> GetAllConfigurations()
        {
            try
            {
                var connStr = Configuration.GetConnectionString(Constants.DbConnectionName);
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