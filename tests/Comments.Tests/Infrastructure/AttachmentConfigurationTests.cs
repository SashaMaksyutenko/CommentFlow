using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Comments.Tests.Infrastructure;

public class AttachmentConfigurationTests
{
    // Builds the EF model only, no DB connection
    private static IEntityType GetAttachmentEntity()
    {
        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseSqlServer("Server=unused")
            .Options;
        using var context = new CommentsDbContext(options);

        return context.Model.FindEntityType(typeof(Attachment))!;
    }

    [Fact]
    public void CommentRelationship_IsOneToOneWithCascadeDelete()
    {
        var foreignKey = GetAttachmentEntity().GetForeignKeys().Single();

        Assert.Equal(nameof(Attachment.CommentId), foreignKey.Properties.Single().Name);
        Assert.True(foreignKey.IsUnique);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Type_IsStoredAsString()
    {
        var typeProperty = GetAttachmentEntity().FindProperty(nameof(Attachment.Type))!;

        Assert.Equal(typeof(string), typeProperty.GetProviderClrType());
    }
}
