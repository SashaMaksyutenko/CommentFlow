using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.OriginalFileName)
            .HasMaxLength(Attachment.OriginalFileNameMaxLength)
            .IsRequired();

        builder.Property(a => a.StoredFileName)
            .HasMaxLength(Attachment.StoredFileNameMaxLength)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(Attachment.ContentTypeMaxLength)
            .IsRequired();

        // Save enum as text ("Image"/"Text"), easier to read in the DB than 0/1
        builder.Property(a => a.Type)
            .HasConversion<string>()
            .HasMaxLength(10);

        // One-to-one: unique index on CommentId is created by EF.
        // Deleting a comment deletes its attachment row too.
        builder.HasOne(a => a.Comment)
            .WithOne(c => c.Attachment)
            .HasForeignKey<Attachment>(a => a.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.StoredFileName)
            .IsUnique();
    }
}
