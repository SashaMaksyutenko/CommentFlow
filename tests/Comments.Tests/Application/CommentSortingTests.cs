using System.ComponentModel.DataAnnotations;
using Comments.Application.Comments;
using Comments.Application.Common;
using Comments.Domain.Entities;

namespace Comments.Tests.Application;

public class CommentSortingTests
{
    private static readonly DateTime Day = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    // Ids 1..4: created one hour apart, so id order = time order
    private static IQueryable<Comment> Comments()
    {
        var anna = TestData.CreateUser(1, "anna", "zed@example.com");
        var bob = TestData.CreateUser(2, "bob", "amy@example.com");
        var carl = TestData.CreateUser(3, "carl", "mike@example.com");

        return new List<Comment>
        {
            TestData.CreateTopLevelComment(1, bob, Day),
            TestData.CreateTopLevelComment(2, anna, Day.AddHours(1)),
            TestData.CreateTopLevelComment(3, carl, Day.AddHours(2)),
            TestData.CreateTopLevelComment(4, anna, Day.AddHours(3)),
        }.AsQueryable();
    }

    private static int[] SortedIds(CommentSortField sortBy, SortDirection direction)
    {
        return Comments().ApplySorting(sortBy, direction).Select(c => c.Id).ToArray();
    }

    [Fact]
    public void Default_IsNewestFirst()
    {
        var query = new GetCommentsQuery();

        Assert.Equal([4, 3, 2, 1], SortedIds(query.SortBy, query.SortDirection));
    }

    [Fact]
    public void CreatedAt_Ascending_IsOldestFirst()
    {
        Assert.Equal([1, 2, 3, 4], SortedIds(CommentSortField.CreatedAt, SortDirection.Asc));
    }

    [Fact]
    public void UserName_Ascending_EqualNamesOrderedById()
    {
        // anna (2, 4), bob (1), carl (3)
        Assert.Equal([2, 4, 1, 3], SortedIds(CommentSortField.UserName, SortDirection.Asc));
    }

    [Fact]
    public void UserName_Descending()
    {
        Assert.Equal([3, 1, 4, 2], SortedIds(CommentSortField.UserName, SortDirection.Desc));
    }

    [Fact]
    public void Email_Ascending()
    {
        // amy (bob: 1), mike (carl: 3), zed (anna: 2, 4)
        Assert.Equal([1, 3, 2, 4], SortedIds(CommentSortField.Email, SortDirection.Asc));
    }

    [Fact]
    public void Email_Descending()
    {
        Assert.Equal([4, 2, 3, 1], SortedIds(CommentSortField.Email, SortDirection.Desc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void Query_PageOutOfRange_IsInvalid(int page)
    {
        var query = new GetCommentsQuery { Page = page };

        Assert.False(Validator.TryValidateObject(query, new ValidationContext(query), null, validateAllProperties: true));
    }

    [Fact]
    public void Query_UnknownSortField_IsInvalid()
    {
        var query = new GetCommentsQuery { SortBy = (CommentSortField)7 };

        Assert.False(Validator.TryValidateObject(query, new ValidationContext(query), null, validateAllProperties: true));
    }
}
