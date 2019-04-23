using System;
using System.Collections.Generic;
using System.Linq;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class BlogConfigurationService
    {
        protected readonly ILogger<BlogConfigurationService> Logger;

        private readonly AesEncryptionService _encryptionService;

        private readonly IRepository<BlogConfiguration> _blogConfigurationRepository;

        public BlogConfigurationService(
            ILogger<BlogConfigurationService> logger,
            AesEncryptionService encryptionService, 
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

        public T GetConfiguration<T>(Func<string, T> parseFunc, Func<T> defaultTValueFunc, string key)
        {
            try
            {
                var cfg = _blogConfigurationRepository.Get(k => k.CfgKey == key.ToString());
                if (null != cfg)
                {
                    return !string.IsNullOrEmpty(cfg.CfgValue) ?
                        parseFunc(cfg.CfgValue) :
                        defaultTValueFunc();
                }

                Logger.LogWarning($"BlogConfigurationKey {key} not found in database, returning default value.");
                return default;
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return default;
            }
        }

        public IDictionary<string, string> GetAllConfigurations()
        {
            try
            {
                var data = _blogConfigurationRepository.Get().ToDictionary(c => c.CfgKey, c => c.CfgValue);
                return data;
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new Dictionary<string, string>();
            }
        }

        public Response SetConfiguration(string key, string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return new FailedResponse((int)ResponseFailureCode.InvalidParameter, "value can not be empty.");
                }

                var cfg = _blogConfigurationRepository.Get(k => k.CfgKey == key.ToString());
                if (null != cfg)
                {
                    cfg.CfgValue = value;
                    cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    int rows = _blogConfigurationRepository.Update(cfg);
                    return new Response(rows > 0);
                }

                var msg = $"BlogConfigurationKey {key} not found in database, can not set value.";
                Logger.LogError(msg);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, msg);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveEmailConfiguration(EmailConfiguration emailConfiguration)
        {
            emailConfiguration.SmtpPassword = EncryptPassword(emailConfiguration.SmtpPassword);
            return SaveObjectConfiguration(emailConfiguration);
        }

        public Response SaveGeneralSettings(BlogConfig blogConfig)
        {
            try
            {
                var r = SetConfiguration(nameof(blogConfig.BloggerName), blogConfig.BloggerName);
                var allSuccess = r.IsSuccess;

                return new Response(allSuccess);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveBloggerAvatar(string bloggerAvatarBase64)
        {
            try
            {
                var r = SetConfiguration(nameof(BlogConfig.BloggerAvatarBase64), bloggerAvatarBase64);
                return new Response(r.IsSuccess);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveObjectConfiguration<T>(T obj) where T : class
        {
            try
            {
                var json = JsonConvert.SerializeObject(obj);
                var r = SetConfiguration(typeof(T).Name, json);
                return new Response(r.IsSuccess);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }
    }
}
