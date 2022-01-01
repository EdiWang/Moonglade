using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth;

public class DeleteAccountQuery : IRequest
{
    public DeleteAccountQuery(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; set; }
}

public class DeleteAccountQueryHandler : IRequestHandler<DeleteAccountQuery>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public DeleteAccountQueryHandler(IRepository<LocalAccountEntity> accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public async Task<Unit> Handle(DeleteAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepo.GetAsync(request.Id);
        if (account is null)
        {
            throw new InvalidOperationException($"LocalAccountEntity with Id '{request.Id}' not found.");
        }

        await _accountRepo.DeleteAsync(request.Id);

        return Unit.Value;
    }
}