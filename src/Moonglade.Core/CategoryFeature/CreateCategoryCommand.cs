using MediatR;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.CategoryFeature
{
    public class CreateCategoryCommand : IRequest
    {
        public CreateCategoryCommand(EditCategoryRequest request)
        {
            Request = request;
        }

        public EditCategoryRequest Request { get; set; }
    }

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand>
    {
        private readonly IRepository<CategoryEntity> _catRepo;
        private readonly IBlogAudit _audit;
        private readonly IBlogCache _cache;

        public CreateCategoryCommandHandler(IRepository<CategoryEntity> catRepo, IBlogAudit audit, IBlogCache cache)
        {
            _catRepo = catRepo;
            _audit = audit;
            _cache = cache;
        }

        public async Task<Unit> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var exists = _catRepo.Any(c => c.RouteName == request.Request.RouteName);
            if (exists) return Unit.Value;

            var category = new CategoryEntity
            {
                Id = Guid.NewGuid(),
                RouteName = request.Request.RouteName.Trim(),
                Note = request.Request.Note?.Trim(),
                DisplayName = request.Request.DisplayName.Trim()
            };

            await _catRepo.AddAsync(category);
            _cache.Remove(CacheDivision.General, "allcats");

            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CategoryCreated, $"Category '{category.RouteName}' created");
            return Unit.Value;
        }
    }
}
