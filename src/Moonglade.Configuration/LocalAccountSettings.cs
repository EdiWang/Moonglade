namespace Moonglade.Configuration;

public class LocalAccountSettings : IBlogSettings
{
    public string Username { get; set; }

    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }

    public static LocalAccountSettings DefaultValue =>
        new()
        {
            Username = "admin",
            PasswordHash = "bXHAa7tEsZmCh1pYcHPotNlP0gaYfzIkxKuHoJnHMt0=",
            PasswordSalt = "Hq8jxngFtTEtl3UI294K7w=="
        };
}