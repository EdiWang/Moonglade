using System;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core
{
    public interface IBlogStatistics
    {
        Task<(int Hits, int Likes)> GetStatisticAsync(Guid postId);

        Task UpdateStatisticAsync(Guid postId, int likes = 0);
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

        public async Task UpdateStatisticAsync(Guid postId, int likes = 0)
        {
            var pp = await _postExtensionRepo.GetAsync(postId);
            if (pp == null) return;

            if (likes > 0)
            {
                pp.Likes += likes;
            }
            else
            {
                pp.Hits += 1;
            }

            await _postExtensionRepo.UpdateAsync(pp);
        }
    }
}
