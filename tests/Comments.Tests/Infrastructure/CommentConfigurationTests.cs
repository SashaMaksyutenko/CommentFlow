using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Comments.Tests.Infrastructure;

public class CommentConfigurationTests
{
    // Builds the EF model only, no DB connection
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

        Assert.False(parentForeignKey.IsRequired);
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
