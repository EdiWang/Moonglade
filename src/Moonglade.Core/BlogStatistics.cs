using System;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core
{
    public interface IBlogStatistics
    {
        Task<(int Hits, int Likes)> GetStatisticAsync(Guid postId);

        Task UpdateStatisticAsync(Guid postId, bool isLike);
    }

    public class BlogStatistics : IBlogStatistics
    {
        private readonly IRepository<PostExtensionEntity> _postExtensionRepo;

        public BlogStatistics(IRepository<PostExtensionEntity> postExtensionRepo)
        {
            _postExtensionRepo = postExtensionRepo;
        }

        public async Task<(int Hits, int Likes)> GetStatisticAsync(Guid postId)
        {
            var pp = await _postExtensionRepo.GetAsync(postId);
            return (pp.Hits, pp.Likes);
        }

        public async Task UpdateStatisticAsync(Guid postId, bool isLike)
        {
            var pp = await _postExtensionRepo.GetAsync(postId);
            if (pp is null) return;

            if (isLike)
            {
                if (pp.Likes >= int.MaxValue) return;
                pp.Likes += 1;
            }
            else
            {
                if (pp.Hits >= int.MaxValue) return;
                pp.Hits += 1;
            }

            await _postExtensionRepo.UpdateAsync(pp);
        }
    }
}
