using System.Collections.Generic;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Configuration.Abstraction
{
    public interface IBlogConfigurationService
    {
        string DecryptPassword(string encryptedPassword);
        string EncryptPassword(string clearPassword);
        IDictionary<string, string> GetAllConfigurations();
        Response SaveConfiguration<T>(T moongladeSettings) where T : MoongladeSettings;
    }
}