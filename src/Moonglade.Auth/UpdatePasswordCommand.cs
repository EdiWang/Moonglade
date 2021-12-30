using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;

namespace Moonglade.Auth;

public class UpdatePasswordCommand : IRequest
{
    public UpdatePasswordCommand(Guid id, string clearPassword)
    {
        Id = id;
        ClearPassword = clearPassword;
    }

    public Guid Id { get; set; }
    public string ClearPassword { get; set; }
}

public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public UpdatePasswordCommandHandler(
        IRepository<LocalAccountEntity> accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public async Task<Unit> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClearPassword))
        {
            throw new ArgumentNullException(nameof(request.ClearPassword), "value must not be empty.");
        }

        var account = await _accountRepo.GetAsync(request.Id);
        if (account is null)
        {
            throw new InvalidOperationException($"LocalAccountEntity with Id '{request.Id}' not found.");
        }

        account.PasswordHash = Helper.HashPassword(request.ClearPassword);
        await _accountRepo.UpdateAsync(account);

        return Unit.Value;
    }
}