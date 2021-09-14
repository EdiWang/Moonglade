using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auth
{
    public interface ILocalAccountService
    {
        int Count();
    }

    public class LocalAccountService : ILocalAccountService
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;

        public LocalAccountService(IRepository<LocalAccountEntity> accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public int Count()
        {
            return _accountRepo.Count();
        }
    }
}
