using Comments.Application.Attachments;
using Comments.Domain.Entities;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Comments.Tests.Infrastructure;

public class StoredFileConfigurationTests
{
    // Builds the EF model only, no DB connection
    private static IEntityType GetStoredFileEntity()
    {
        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseSqlServer("Server=unused")
            .Options;
        using var context = new CommentsDbContext(options);

        return context.Model.FindEntityType(typeof(StoredFile))!;
    }

    [Fact]
    public void FileName_IsTheKey()
    {
        var key = GetStoredFileEntity().FindPrimaryKey()!;

        Assert.Equal(nameof(StoredFile.FileName), key.Properties.Single().Name);
    }

    [Fact]
    public void FileName_FitsTheLongestName_OfAnImageWaitingForProcessing()
    {
        var longestName = PendingUpload.NameFor(new string('a', Attachment.StoredFileNameMaxLength));

        var maxLength = GetStoredFileEntity().FindProperty(nameof(StoredFile.FileName))!.GetMaxLength();

        Assert.Equal(longestName.Length, maxLength);
    }

    [Fact]
    public void Content_IsRequired()
    {
        var content = GetStoredFileEntity().FindProperty(nameof(StoredFile.Content))!;

        Assert.False(content.IsNullable);
    }
}
