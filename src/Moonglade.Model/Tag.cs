namespace Moonglade.Model
{
    public class Tag
    {
        public int Id { get; set; }

        public string TagName { get; set; }

        public string NormalizedTagName { get; set; }
    }

    public class TagCountInfo : Tag
    {
        public int TagCount { get; set; }
    }
}
