using System;

namespace Moonglade.Data.Entities
{
    public class LocalAccountEntity
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public DateTime? LastLoginTimeUtc { get; set; }
        public string LastLoginIp { get; set; }
        public DateTime CreateTimeUtc { get; set; }
    }
}
