namespace Moonglade.Data.Entities
{
    public class BlogThemeEntity
    {
        public int Id { get; set; }
        public string ThemeName { get; set; }
        public string CssRules { get; set; }
        public string AdditionalProps { get; set; }
        public ThemeType ThemeType { get; set; }
    }

    public enum ThemeType
    {
        System = 0,
        User = 1
    }
}
