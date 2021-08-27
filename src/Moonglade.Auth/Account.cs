using System;
using Moonglade.Data.Entities;

namespace Moonglade.Auth
{
    public class Account
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public DateTime? LastLoginTimeUtc { get; set; }
        public string LastLoginIp { get; set; }
        public DateTime CreateTimeUtc { get; set; }

        public Account()
        {
            
        }

        public Account(LocalAccountEntity entity)
        {
            if (null == entity) return;

            Id = entity.Id;
            CreateTimeUtc = entity.CreateTimeUtc;
            LastLoginIp = entity.LastLoginIp.Trim();
            LastLoginTimeUtc = entity.LastLoginTimeUtc.GetValueOrDefault();
            Username = entity.Username.Trim();
        }
    }
}
