using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Menus
{
    public class DeleteMenuCommand : IRequest
    {
        public DeleteMenuCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand>
    {
        private readonly IRepository<MenuEntity> _menuRepo;
        private readonly IBlogAudit _audit;

        public DeleteMenuCommandHandler(IRepository<MenuEntity> menuRepo, IBlogAudit audit)
        {
            _menuRepo = menuRepo;
            _audit = audit;
        }

        public async Task<Unit> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
        {
            var menu = await _menuRepo.GetAsync(request.Id);
            if (menu is null)
            {
                throw new InvalidOperationException($"MenuEntity with Id '{request.Id}' not found.");
            }

            await _menuRepo.DeleteAsync(request.Id);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CategoryDeleted, $"Menu '{request.Id}' deleted.");

            return Unit.Value;
        }
    }
}
