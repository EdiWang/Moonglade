using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class CreateAccountCommand : IRequest
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

public class CreateAccountCommandHandler : AsyncRequestHandler<CreateAccountCommand>
{
    private readonly IRepository<LocalAccountEntity> _accountRepo;
    public CreateAccountCommandHandler(IRepository<LocalAccountEntity> accountRepo) => _accountRepo = accountRepo;

    protected override Task Handle(CreateAccountCommand request, CancellationToken ct)
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
        var salt = Helper.GenerateSalt();
        var hash = Helper.HashPassword2(request.Password.Trim(), salt);

        var account = new LocalAccountEntity
        {
            Id = uid,
            CreateTimeUtc = DateTime.UtcNow,
            Username = request.Username.ToLower().Trim(),
            PasswordSalt = salt,
            PasswordHash = hash
        };

        return _accountRepo.AddAsync(account, ct);
    }
}