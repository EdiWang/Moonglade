using System;

namespace Moonglade.Model
{
    public class Account
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public DateTime? LastLoginTimeUtc { get; set; }
        public string LastLoginIp { get; set; }
        public DateTime CreateOnUtc { get; set; }
    }
}
