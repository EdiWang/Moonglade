using System.Threading.Tasks;

namespace Moonglade.Web.Authentication
{
    public interface IGetApiKeyQuery
    {
        Task<ApiKey> Execute(string providedApiKey);
    }
}