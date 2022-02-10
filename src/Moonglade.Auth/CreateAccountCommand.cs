using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class CreateAccountCommand : IRequest<Guid>
{
    [Required]
    [Display(Name = "Username")]
    [MinLength(2), MaxLength(32)]
    [RegularExpression("[a-z0-9]+")]
    public string Username { get; set; }

    [Required]
    [Display(Name = "Password")]
    [MinLength(8), MaxLength(32)]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string Password { get; set; }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;

    public CreateAccountCommandHandler(
        IRepository<LocalAccountEntity> accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ArgumentNullException(nameof(request.Username), "value must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentNullException(nameof(request.Password), "value must not be empty.");
        }

        var uid = Guid.NewGuid();
        var account = new LocalAccountEntity
        {
            Id = uid,
            CreateTimeUtc = DateTime.UtcNow,
            Username = request.Username.ToLower().Trim(),
            PasswordHash = Helper.HashPassword(request.Password.Trim())
        };

        await _accountRepo.AddAsync(account);

        return uid;
    }
}