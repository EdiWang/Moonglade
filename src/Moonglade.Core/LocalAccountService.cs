using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

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
    }
}
