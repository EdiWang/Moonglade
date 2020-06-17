using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.ImageStorage
{
    public interface IBlogImageStorage
    {
        string Name { get; }

        Task<Response<string>> InsertAsync(string fileName, byte[] imageBytes);

        Task<Response<ImageInfo>> GetAsync(string fileName);

        Task<Response> DeleteAsync(string fileName);
    }
}
