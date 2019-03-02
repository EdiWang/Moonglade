using System;
using System.Collections.Generic;
using System.Linq;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Model;

namespace Moonglade.Configuration
{
    public class BlogConfigurationService
    {
        protected MoongladeDbContext Context;

        protected readonly ILogger<BlogConfigurationService> Logger;

        private readonly AesEncryptionService _encryptionService;

        public BlogConfigurationService(MoongladeDbContext context,
            ILogger<BlogConfigurationService> logger,
            AesEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
            if (null != context) Context = context;
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
                var cfg = Context.BlogConfiguration.FirstOrDefault(k => k.CfgKey == key.ToString());
                if (null != cfg)
                {
                    return !string.IsNullOrEmpty(cfg.CfgValue) ?
                        parseFunc(cfg.CfgValue) :
                        defaultTValueFunc();
                }

                Logger.LogWarning($"BlogConfigurationKey {key} not found in database, returning default value.");
                return default(T);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return default(T);
            }
        }

        public IDictionary<string, string> GetAllConfigurations()
        {
            try
            {
                var data = Context.BlogConfiguration.ToDictionary(c => c.CfgKey, c => c.CfgValue);
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

                var cfg = Context.BlogConfiguration.FirstOrDefault(k => k.CfgKey == key.ToString());
                if (null != cfg)
                {
                    cfg.CfgValue = value;
                    cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    var rows = Context.SaveChanges();
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
            try
            {
                foreach (var propertyInfo in emailConfiguration.GetType().GetProperties())
                {
                    var cfg = Context.BlogConfiguration.FirstOrDefault(k => k.CfgKey == $"{nameof(EmailConfiguration)}.{propertyInfo.Name}");
                    if (null != cfg)
                    {
                        cfg.CfgValue = propertyInfo.GetValue(emailConfiguration, null).ToString();
                        cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    }
                }

                var rows = Context.SaveChanges();
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveFeedConfiguration(FeedSettings feedSettings)
        {
            try
            {
                foreach (var propertyInfo in feedSettings.GetType().GetProperties())
                {
                    var cfg = Context.BlogConfiguration.FirstOrDefault(k => k.CfgKey == $"{nameof(FeedSettings)}.{propertyInfo.Name}");
                    if (null != cfg)
                    {
                        cfg.CfgValue = propertyInfo.GetValue(feedSettings, null).ToString();
                        cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    }
                }

                var rows = Context.SaveChanges();
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveWatermarkConfiguration(WatermarkSettings watermarkSettings)
        {
            try
            {
                foreach (var propertyInfo in watermarkSettings.GetType().GetProperties())
                {
                    var cfg = Context.BlogConfiguration.FirstOrDefault(k => k.CfgKey == $"{nameof(WatermarkSettings)}.{propertyInfo.Name}");
                    if (null != cfg)
                    {
                        cfg.CfgValue = propertyInfo.GetValue(watermarkSettings, null).ToString();
                        cfg.LastModifiedTimeUtc = DateTime.UtcNow;
                    }
                }

                var rows = Context.SaveChanges();
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public Response SaveGeneralSettings(BlogConfig blogConfig)
        {
            try
            {
                var r1 = SetConfiguration(nameof(blogConfig.DisharmonyWords), blogConfig.DisharmonyWords);
                var r2 = SetConfiguration(nameof(blogConfig.MetaKeyword), blogConfig.MetaKeyword);
                var r3 = SetConfiguration(nameof(blogConfig.MetaAuthor), blogConfig.MetaAuthor);
                var r4 = SetConfiguration(nameof(blogConfig.SiteTitle), blogConfig.SiteTitle);
                var r5 = SetConfiguration("ReaderView.SiteName", blogConfig.ReaderView.SiteName);

                var allSuccess = r1.IsSuccess && 
                                 r2.IsSuccess && 
                                 r3.IsSuccess && 
                                 r4.IsSuccess && 
                                 r5.IsSuccess;

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
                var r = SetConfiguration("BloggerAvatarBase64", bloggerAvatarBase64);
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
