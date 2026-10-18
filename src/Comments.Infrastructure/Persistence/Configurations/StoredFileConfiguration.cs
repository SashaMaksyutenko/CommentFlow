using Comments.Application.Attachments;
using Comments.Domain.Entities;
using Comments.Infrastructure.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    // A generated name plus ".upload" for an image that still waits for processing.
    // Not "const": the length of a string is known only when the program runs.
    public static readonly int FileNameMaxLength = Attachment.StoredFileNameMaxLength + PendingUpload.Suffix.Length;

    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        // The file name is unique already, so it is the key: no extra Id column
        builder.HasKey(f => f.FileName);

        builder.Property(f => f.FileName)
            .HasMaxLength(FileNameMaxLength);

        // varbinary(max): a picture up to 5 MB before resizing, a TXT file up to 100 KB
        builder.Property(f => f.Content)
            .IsRequired();
    }
}
