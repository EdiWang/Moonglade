using Moonglade.Data.Entities;

namespace Moonglade.Auth;

public class Account
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public DateTime CreateTimeUtc { get; set; }

    public Account()
    {

    }

    public Account(LocalAccountEntity entity)
    {
        if (null == entity) return;

        Id = entity.Id;
        CreateTimeUtc = entity.CreateTimeUtc;
        Username = entity.Username.Trim();
    }
}