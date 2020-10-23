using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;

namespace Moonglade.Core
{
    public class LocalAccountService : BlogService
    {
        private readonly IRepository<LocalAccountEntity> _accountRepository;
        private readonly IBlogAudit _blogAudit;

        public LocalAccountService(
            ILogger<LocalAccountService> logger,
            IRepository<LocalAccountEntity> accountRepository,
            IBlogAudit blogAudit) : base(logger)
        {
            _accountRepository = accountRepository;
            _blogAudit = blogAudit;
        }

        public async Task<Account> GetAsync(Guid id)
        {
            var entity = await _accountRepository.GetAsync(id);
            var item = EntityToAccountModel(entity);
            return item;
        }

        public Task<IReadOnlyList<Account>> GetAllAsync()
        {
            var list = _accountRepository.SelectAsync(p => new Account
            {
                Id = p.Id,
                CreateOnUtc = p.CreateOnUtc,
                LastLoginIp = p.LastLoginIp,
                LastLoginTimeUtc = p.LastLoginTimeUtc,
                PasswordHash = p.PasswordHash,
                Username = p.Username
            });

            return list;
        }

        public async Task DeleteAsync(Guid id)
        {
            var account = await _accountRepository.GetAsync(id);
            if (null == account)
            {
                throw new InvalidOperationException($"LocalAccountEntity with Id '{id}' not found.");
            }

            _accountRepository.Delete(id);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsDeleteAccount, $"Account '{id}' deleted.");
        }

        public static string HashPassword(string plainMessage)
        {
            if (string.IsNullOrWhiteSpace(plainMessage))
            {
                return string.Empty;
            }

            var data = Encoding.UTF8.GetBytes(plainMessage);
            using HashAlgorithm sha = new SHA256Managed();
            sha.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(sha.Hash);
        }

        private static Account EntityToAccountModel(LocalAccountEntity entity)
        {
            if (null == entity)
            {
                return null;
            }

            return new Account
            {
                Id = entity.Id,
                CreateOnUtc = entity.CreateOnUtc,
                LastLoginIp = entity.LastLoginIp.Trim(),
                LastLoginTimeUtc = entity.LastLoginTimeUtc,
                PasswordHash = entity.PasswordHash,
                Username = entity.Username.Trim()
            };
        }
    }
}
