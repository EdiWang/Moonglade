namespace Moonglade.Configuration;

public class LocalAccountSettings : IBlogSettings<LocalAccountSettings>
{
    public string Username { get; set; }

    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public string TotpSecret { get; set; } = string.Empty;
    public bool IsTotpEnabled { get; set; }

    public static LocalAccountSettings DefaultValue =>
        new()
        {
            Username = "admin",
            PasswordHash = "bXHAa7tEsZmCh1pYcHPotNlP0gaYfzIkxKuHoJnHMt0=", // admin123
            PasswordSalt = "Hq8jxngFtTEtl3UI294K7w==",
            TotpSecret = string.Empty,
            IsTotpEnabled = false
        };
}
