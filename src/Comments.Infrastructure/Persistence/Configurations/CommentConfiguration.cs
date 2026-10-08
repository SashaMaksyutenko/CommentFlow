using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

/// <summary>
/// Describes how the Comment entity is stored in the "Comments" table.
/// Picked up automatically by ApplyConfigurationsFromAssembly in CommentsDbContext.
/// </summary>
public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        // Primary key. SQL Server generates the value (IDENTITY column).
        builder.HasKey(c => c.Id);

        // nvarchar(max) NOT NULL, without a length limit in the database:
        // - SQL Server allows at most nvarchar(4000); anything longer must be nvarchar(max);
        // - after sanitizing (step 9) the stored text can be longer than what the user typed
        //   (e.g. "<" becomes "&lt;"), so a hard column limit could reject valid comments.
        // The 5000-character limit (Comment.TextMaxLength) is checked on the input instead (step 8).
        builder.Property(c => c.Text)
            .IsRequired();

        // Stored as datetime2 (EF default for DateTime): precise and without the
        // 1753 year limit of the old "datetime" type.
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Optional columns (nullable), only length-limited.
        builder.Property(c => c.IpAddress)
            .HasMaxLength(Comment.IpAddressMaxLength);

        builder.Property(c => c.UserAgent)
            .HasMaxLength(Comment.UserAgentMaxLength);

        // Many comments belong to one user.
        // User does not have a "Comments" list, so WithMany() is called without arguments.
        // Restrict: the database refuses to delete a user who still has comments,
        // so comments are never lost by accident.
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-reference: a comment points to its parent comment; the parent sees it in Replies.
        // Restrict is required here: SQL Server does not allow ON DELETE CASCADE on a table
        // that references itself (it could cause cycles), so the migration would fail.
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Speeds up the main page query (step 10): "top-level comments (ParentId IS NULL),
        // newest first". It also serves the "replies of comment X" query (WHERE ParentId = X),
        // so EF does not create a separate index for the ParentId foreign key.
        builder.HasIndex(c => new { c.ParentId, c.CreatedAt });
    }
}
