using System.Collections.Generic;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Configuration
{
    public interface IBlogConfigurationService
    {
        string DecryptPassword(string encryptedPassword);
        string EncryptPassword(string clearPassword);
        IDictionary<string, string> GetAllConfigurations();
        Response SetConfiguration(string key, string value);
        Response SaveConfiguration<T>(T moongladeSettings) where T : MoongladeSettings;
    }
}