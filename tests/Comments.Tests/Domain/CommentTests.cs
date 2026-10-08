using Comments.Domain.Entities;

namespace Comments.Tests.Domain;

public class CommentTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        // Remember the time before creating, to check CreatedAt is "now" in UTC.
        var before = DateTime.UtcNow;

        var comment = new Comment(userId: 1, parentId: 7, "Hello", "127.0.0.1", "Mozilla/5.0");

        Assert.Equal(1, comment.UserId);
        Assert.Equal(7, comment.ParentId);
        Assert.Equal("Hello", comment.Text);
        Assert.Equal("127.0.0.1", comment.IpAddress);
        Assert.Equal("Mozilla/5.0", comment.UserAgent);
        Assert.Equal(DateTimeKind.Utc, comment.CreatedAt.Kind);
        Assert.InRange(comment.CreatedAt, before, DateTime.UtcNow);
        Assert.Empty(comment.Replies);
    }

    [Fact]
    public void Constructor_WithoutParent_CreatesTopLevelComment()
    {
        var comment = new Comment(userId: 1, parentId: null, "Hello", ipAddress: null, userAgent: null);

        Assert.Null(comment.ParentId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() => new Comment(1, null, text, null, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidUserId_Throws(int userId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Comment(userId, null, "Hello", null, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidParentId_Throws(int parentId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Comment(1, parentId, "Hello", null, null));
    }
}
