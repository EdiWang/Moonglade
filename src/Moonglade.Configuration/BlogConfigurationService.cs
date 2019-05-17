using System;
using System.Collections.Generic;
using System.Linq;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.Configuration
{
    public class BlogConfigurationService : IBlogConfigurationService
    {
        protected readonly ILogger<BlogConfigurationService> Logger;

        private readonly IAesEncryptionService _encryptionService;

        private readonly IRepository<BlogConfiguration> _blogConfigurationRepository;

        public BlogConfigurationService(
            ILogger<BlogConfigurationService> logger,
            IAesEncryptionService encryptionService,
            IRepository<BlogConfiguration> blogConfiguration)
        {
            _encryptionService = encryptionService;
            _blogConfigurationRepository = blogConfiguration;
            if (null != logger) Logger = logger;
        }

        public string DecryptPassword(string encryptedPassword)
        {
            var str = _encryptionService.Decrypt(encryptedPassword);
            return str;
        }

        public string EncryptPassword(string clearPassword)
        {
            var str = _encryptionService.Encrypt(clearPassword);
            return str;
        }

        public IDictionary<string, string> GetAllConfigurations()
        {
            try
            {
                var data = _blogConfigurationRepository.Get();
                var dic = data.ToDictionary(c => c.CfgKey, c => c.CfgValue);
                return dic;
            }
            catch (Exception e)
            {
                Logger.LogCritical(e, $"Error {nameof(GetAllConfigurations)}");
                throw;
            }
        }

        public Response SaveConfiguration<T>(T moongladeSettings) where T : MoongladeSettings
        {
            void SetConfiguration(string key, string value)
            {
                var cfg = _blogConfigurationRepository.Get(k => k.CfgKey == key.ToString());
                if (null != cfg)
                {
                    cfg.CfgValue = value;
                    cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    _blogConfigurationRepository.Update(cfg);
                }

                var msg = $@"{nameof(BlogConfiguration.CfgKey)} ""{key}"" is not found, can not set value.";
                Logger.LogError(msg);
                throw new KeyNotFoundException(key);
            }

            try
            {
                var json = moongladeSettings.GetJson();
                SetConfiguration(typeof(T).Name, json);
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }
    }
}
