namespace Moonglade.Data.Entities
{
    public class TagEntity
    {
        public TagEntity()
        {
            Posts = new HashSet<PostEntity>();
        }

        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string NormalizedName { get; set; }

        public virtual ICollection<PostEntity> Posts { get; set; }
    }
}
