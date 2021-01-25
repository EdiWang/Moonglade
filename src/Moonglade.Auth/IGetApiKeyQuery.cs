using System.Threading.Tasks;

namespace Moonglade.Auth
{
    public interface IGetApiKeyQuery
    {
        Task<ApiKey> Execute(string providedApiKey);
    }
}