using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        // No max length here: nvarchar can't go above 4000, and sanitized HTML
        // can be longer than the input. The 5000 limit is checked on input.
        builder.Property(c => c.Text)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.IpAddress)
            .HasMaxLength(Comment.IpAddressMaxLength);

        builder.Property(c => c.UserAgent)
            .HasMaxLength(Comment.UserAgentMaxLength);

        // Can't delete a user who still has comments
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server doesn't allow cascade delete on a self-referencing table
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // For "top-level comments, newest first" and "replies of comment X"
        builder.HasIndex(c => new { c.ParentId, c.CreatedAt });
    }
}
