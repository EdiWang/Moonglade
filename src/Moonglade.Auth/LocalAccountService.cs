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
        Task<Guid> ValidateAsync(string username, string inputPassword);
        bool Exist(string username);
        Task<Guid> CreateAsync(string username, string clearPassword);
        Task UpdatePasswordAsync(Guid id, string clearPassword);
        Task DeleteAsync(Guid id);
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

        public async Task<Guid> ValidateAsync(string username, string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username), "value must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(inputPassword))
            {
                throw new ArgumentNullException(nameof(inputPassword), "value must not be empty.");
            }

            var account = await _accountRepo.GetAsync(p => p.Username == username);
            if (account is null) return Guid.Empty;

            var valid = account.PasswordHash == Helper.HashPassword(inputPassword.Trim());
            return valid ? account.Id : Guid.Empty;
        }

        public bool Exist(string username)
        {
            var exist = _accountRepo.Any(p => p.Username == username.ToLower());
            return exist;
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

        public async Task UpdatePasswordAsync(Guid id, string clearPassword)
        {
            if (string.IsNullOrWhiteSpace(clearPassword))
            {
                throw new ArgumentNullException(nameof(clearPassword), "value must not be empty.");
            }

            var account = await _accountRepo.GetAsync(id);
            if (account is null)
            {
                throw new InvalidOperationException($"LocalAccountEntity with Id '{id}' not found.");
            }

            account.PasswordHash = Helper.HashPassword(clearPassword);
            await _accountRepo.UpdateAsync(account);

            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsAccountPasswordUpdated, $"Account password for '{id}' updated.");
        }

        public async Task DeleteAsync(Guid id)
        {
            var account = await _accountRepo.GetAsync(id);
            if (account is null)
            {
                throw new InvalidOperationException($"LocalAccountEntity with Id '{id}' not found.");
            }

            await _accountRepo.DeleteAsync(id);
            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsDeleteAccount, $"Account '{id}' deleted.");
        }
    }
}
