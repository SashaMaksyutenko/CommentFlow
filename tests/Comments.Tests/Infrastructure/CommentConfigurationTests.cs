using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Comments.Tests.Infrastructure;

public class CommentConfigurationTests
{
    // Builds the EF model in memory and returns the Comment entity metadata.
    // No database connection is opened, so these tests do not need SQL Server.
    private static IEntityType GetCommentEntity()
    {
        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseSqlServer("Server=unused")
            .Options;
        using var context = new CommentsDbContext(options);

        return context.Model.FindEntityType(typeof(Comment))!;
    }

    [Fact]
    public void ParentRelationship_IsOptionalAndDoesNotCascadeDelete()
    {
        var parentForeignKey = GetCommentEntity().GetForeignKeys()
            .Single(fk => fk.Properties.Single().Name == nameof(Comment.ParentId));

        // Optional: top-level comments have no parent.
        Assert.False(parentForeignKey.IsRequired);
        // SQL Server forbids cascade delete on a self-referencing table.
        Assert.Equal(DeleteBehavior.Restrict, parentForeignKey.DeleteBehavior);
    }

    [Fact]
    public void UserRelationship_IsRequiredAndDoesNotCascadeDelete()
    {
        var userForeignKey = GetCommentEntity().GetForeignKeys()
            .Single(fk => fk.Properties.Single().Name == nameof(Comment.UserId));

        Assert.True(userForeignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, userForeignKey.DeleteBehavior);
    }

    [Fact]
    public void ParentIdAndCreatedAt_HaveIndexForTopLevelListing()
    {
        var hasIndex = GetCommentEntity().GetIndexes()
            .Any(i => i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(Comment.ParentId), nameof(Comment.CreatedAt)]));

        Assert.True(hasIndex);
    }
}
