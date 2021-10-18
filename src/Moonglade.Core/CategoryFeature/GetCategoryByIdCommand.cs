using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.CategoryFeature
{
    public class GetCategoryByIdCommand : IRequest<Category>
    {
        public GetCategoryByIdCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class GetCategoryByIdCommandHandler : IRequestHandler<GetCategoryByIdCommand, Category>
    {
        private readonly IRepository<CategoryEntity> _catRepo;

        public GetCategoryByIdCommandHandler(IRepository<CategoryEntity> catRepo)
        {
            _catRepo = catRepo;
        }

        public Task<Category> Handle(GetCategoryByIdCommand request, CancellationToken cancellationToken)
        {
            return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(request.Id), Category.EntitySelector);
        }
    }
}
