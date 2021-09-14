using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Threading.Tasks;

namespace Moonglade.Auth
{
    public interface ILocalAccountService
    {
        int Count();
        Task<Guid> CreateAsync(string username, string clearPassword);
    }

    public class LocalAccountService : ILocalAccountService
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;
        private readonly IBlogAudit _audit;

        public LocalAccountService(
            IRepository<LocalAccountEntity> accountRepo,
            IBlogAudit audit)
        {
            _accountRepo = accountRepo;
            _audit = audit;
        }

        public int Count()
        {
            return _accountRepo.Count();
        }

        public async Task<Guid> CreateAsync(string username, string clearPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username), "value must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(clearPassword))
            {
                throw new ArgumentNullException(nameof(clearPassword), "value must not be empty.");
            }

            var uid = Guid.NewGuid();
            var account = new LocalAccountEntity
            {
                Id = uid,
                CreateTimeUtc = DateTime.UtcNow,
                Username = username.ToLower().Trim(),
                PasswordHash = Helper.HashPassword(clearPassword.Trim())
            };

            await _accountRepo.AddAsync(account);
            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsAccountCreated, $"Account '{account.Id}' created.");

            return uid;
        }
    }
}
