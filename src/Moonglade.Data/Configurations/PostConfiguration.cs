using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations
{
    public class PostConfiguration: IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.Property(e => e.CommentEnabled);
            builder.Property(e => e.ContentAbstract).HasMaxLength(1024);

            builder.Property(e => e.CreateOnUtc).HasColumnType("datetime");
            builder.Property(e => e.PostContent);

            builder.Property(e => e.Slug).HasMaxLength(128);
            builder.Property(e => e.Title).HasMaxLength(128);
        }
    }
}
