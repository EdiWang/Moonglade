using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Menus;

public record GetMenuQuery(Guid Id) : IRequest<Menu>;

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, Menu>
{
    private readonly IRepository<MenuEntity> _repo;

    public GetMenuQueryHandler(IRepository<MenuEntity> repo) => _repo = repo;

    public async Task<Menu> Handle(GetMenuQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(request.Id, ct);
        if (null == entity) return null;

        var item = new Menu(entity);
        return item;
    }
}