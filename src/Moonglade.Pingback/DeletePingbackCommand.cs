using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Pingback
{
    public class DeletePingbackCommand : IRequest
    {
        public DeletePingbackCommand(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
    }

    public class DeletePingbackCommandHandler : IRequestHandler<DeletePingbackCommand>
    {
        private readonly IRepository<PingbackEntity> _pingbackRepo;

        public DeletePingbackCommandHandler(IRepository<PingbackEntity> pingbackRepo)
        {
            _pingbackRepo = pingbackRepo;
        }

        public async Task<Unit> Handle(DeletePingbackCommand request, CancellationToken cancellationToken)
        {
            await _pingbackRepo.DeleteAsync(request.Id);
            return Unit.Value;
        }
    }
}
