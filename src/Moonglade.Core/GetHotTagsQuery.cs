using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public class GetHotTagsQuery : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>
    {
        public GetHotTagsQuery(int top)
        {
            Top = top;
        }

        public int Top { get; set; }
    }

    public class GetHotTagsQueryHandler : IRequestHandler<GetHotTagsQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
    {
        private readonly IRepository<TagEntity> _tagRepo;

        public GetHotTagsQueryHandler(IRepository<TagEntity> tagRepo)
        {
            _tagRepo = tagRepo;
        }

        public async Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetHotTagsQuery request, CancellationToken cancellationToken)
        {
            if (!_tagRepo.Any()) return new List<KeyValuePair<Tag, int>>();

            var spec = new TagSpec(request.Top);
            var tags = await _tagRepo.SelectAsync(spec, t =>
                new KeyValuePair<Tag, int>(new()
                {
                    Id = t.Id,
                    DisplayName = t.DisplayName,
                    NormalizedName = t.NormalizedName
                }, t.Posts.Count));

            return tags;
        }
    }
}
