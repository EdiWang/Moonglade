namespace Moonglade.Model
{
    public class Tag
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string NormalizedName { get; set; }
    }

    public class DegreeTag : Tag
    {
        public int Degree { get; set; }
    }
}
