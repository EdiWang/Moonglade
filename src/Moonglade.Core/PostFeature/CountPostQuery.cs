using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.PostFeature
{
    public enum CountType
    {
        Public,
        Category,
        Tag,
        Featured
    }

    public class CountPostQuery : IRequest<int>
    {
        public CountPostQuery(CountType countType, Guid? catId = null, int? tagId = null)
        {
            CountType = countType;
            CatId = catId;
            TagId = tagId;
        }

        public CountType CountType { get; set; }

        public Guid? CatId { get; set; }

        public int? TagId { get; set; }
    }

    public class CountPostQueryHandler : IRequestHandler<CountPostQuery, int>
    {
        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        public CountPostQueryHandler(
            IRepository<PostEntity> postRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo)
        {
            _postRepo = postRepo;
            _postTagRepo = postTagRepo;
            _postCatRepo = postCatRepo;
        }

        public Task<int> Handle(CountPostQuery request, CancellationToken cancellationToken)
        {
            int count = 0;

            switch (request.CountType)
            {
                case CountType.Public:
                    count = _postRepo.Count(p => p.IsPublished && !p.IsDeleted);
                    break;

                case CountType.Category:
                    if (request.CatId == null) throw new ArgumentNullException(nameof(request.CatId));
                    count = _postCatRepo.Count(c => c.CategoryId == request.CatId.Value
                                            && c.Post.IsPublished
                                            && !c.Post.IsDeleted);
                    break;

                case CountType.Tag:
                    if (request.TagId == null) throw new ArgumentNullException(nameof(request.TagId));
                    count = _postTagRepo.Count(p => p.TagId == request.TagId.Value && p.Post.IsPublished && !p.Post.IsDeleted);
                    break;

                case CountType.Featured:
                    count = _postRepo.Count(p => p.IsFeatured && p.IsPublished && !p.IsDeleted);
                    break;
            }

            return Task.FromResult(count);
        }
    }
}
