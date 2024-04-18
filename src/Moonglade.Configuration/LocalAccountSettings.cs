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
            PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
            PasswordSalt = "" // TODO
        };
}