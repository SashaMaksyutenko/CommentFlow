using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comments.Tests.Infrastructure;

public class UserConfigurationTests
{
    [Fact]
    public void UserNameAndEmail_HaveUniqueIndex()
    {
        // Builds the EF model only, no DB connection
        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseSqlServer("Server=unused")
            .Options;
        using var context = new CommentsDbContext(options);

        var userEntity = context.Model.FindEntityType(typeof(User))!;
        var index = userEntity.GetIndexes()
            .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(User.UserName), nameof(User.Email)]));

        Assert.True(index.IsUnique);
    }
}
