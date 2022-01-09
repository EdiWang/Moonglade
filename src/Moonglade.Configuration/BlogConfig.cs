using Dapper;
using System.Data;


namespace Moonglade.Configuration;

public interface IBlogSettings
{
}

public interface IBlogConfig
{
    GeneralSettings GeneralSettings { get; set; }
    ContentSettings ContentSettings { get; set; }
    NotificationSettings NotificationSettings { get; set; }
    FeedSettings FeedSettings { get; set; }
    ImageSettings ImageSettings { get; set; }
    AdvancedSettings AdvancedSettings { get; set; }
    CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }

    Task SaveAsync<T>(T blogSettings) where T : IBlogSettings;
}

public class BlogConfig : IBlogConfig
{
    private readonly IDbConnection _dbConnection;

    public GeneralSettings GeneralSettings { get; set; }

    public ContentSettings ContentSettings { get; set; }

    public NotificationSettings NotificationSettings { get; set; }

    public FeedSettings FeedSettings { get; set; }

    public ImageSettings ImageSettings { get; set; }

    public AdvancedSettings AdvancedSettings { get; set; }

    public CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }

    private bool _hasInitialized;

    public BlogConfig(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;

        ContentSettings = new();
        GeneralSettings = new();
        NotificationSettings = new();
        FeedSettings = new();
        ImageSettings = new();
        AdvancedSettings = new();
        CustomStyleSheetSettings = new();

        Initialize();
    }

    private void Initialize()
    {
        if (_hasInitialized) return;

        var config = GetAllConfigurations();
        GeneralSettings = config[nameof(GeneralSettings)].FromJson<GeneralSettings>();
        ContentSettings = config[nameof(ContentSettings)].FromJson<ContentSettings>();
        NotificationSettings = config[nameof(NotificationSettings)].FromJson<NotificationSettings>();
        FeedSettings = config[nameof(FeedSettings)].FromJson<FeedSettings>();
        ImageSettings = config[nameof(ImageSettings)].FromJson<ImageSettings>();
        AdvancedSettings = config[nameof(AdvancedSettings)].FromJson<AdvancedSettings>();
        CustomStyleSheetSettings = config[nameof(CustomStyleSheetSettings)].FromJson<CustomStyleSheetSettings>();

        _hasInitialized = true;
    }

    public async Task SaveAsync<T>(T blogSettings) where T : IBlogSettings
    {
        async Task SetConfiguration(string key, string value)
        {
            var sql = $"UPDATE {nameof(BlogConfiguration)} " +
                      $"SET {nameof(BlogConfiguration.CfgValue)} = @value, " +
                      $"{nameof(BlogConfiguration.LastModifiedTimeUtc)} = @lastModifiedTimeUtc " +
                      $"WHERE {nameof(BlogConfiguration.CfgKey)} = @key";

            await _dbConnection.ExecuteAsync(sql, new { key, value, lastModifiedTimeUtc = DateTime.UtcNow });
        }

        var json = blogSettings.ToJson();
        var task = SetConfiguration(typeof(T).Name, json);

        await task;
        Dirty();
    }

    protected void Dirty()
    {
        _hasInitialized = false;
    }

    private IDictionary<string, string> GetAllConfigurations()
    {
        var sql = $"SELECT {nameof(BlogConfiguration.CfgKey)}, " +
                  $"{nameof(BlogConfiguration.CfgValue)} " +
                  $"FROM {nameof(BlogConfiguration)}";

        var data = _dbConnection.Query<(string CfgKey, string CfgValue)>(sql);
        var dic = data.ToDictionary(c => c.CfgKey, c => c.CfgValue);
        return dic;
    }
}