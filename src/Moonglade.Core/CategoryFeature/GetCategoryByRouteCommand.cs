using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Core.CategoryFeature
{
    public class GetCategoryByRouteCommand : IRequest<Category>
    {
        public GetCategoryByRouteCommand(string routeName)
        {
            RouteName = routeName;
        }

        public string RouteName { get; set; }
    }

    public class GetCategoryByRouteCommandHandler : IRequestHandler<GetCategoryByRouteCommand, Category>
    {
        private readonly IRepository<CategoryEntity> _catRepo;

        public GetCategoryByRouteCommandHandler(IRepository<CategoryEntity> catRepo)
        {
            _catRepo = catRepo;
        }

        public Task<Category> Handle(GetCategoryByRouteCommand request, CancellationToken cancellationToken)
        {
            return _catRepo.SelectFirstOrDefaultAsync(new CategorySpec(request.RouteName), Category.EntitySelector);
        }
    }
}
