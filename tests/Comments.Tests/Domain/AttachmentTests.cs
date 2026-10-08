using Comments.Domain.Entities;
using Comments.Domain.Enums;

namespace Comments.Tests.Domain;

public class AttachmentTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var attachment = new Attachment("cat.png", "3f2a9c.png", "image/png", 2048, AttachmentType.Image);

        Assert.Equal("cat.png", attachment.OriginalFileName);
        Assert.Equal("3f2a9c.png", attachment.StoredFileName);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Equal(2048, attachment.Size);
        Assert.Equal(AttachmentType.Image, attachment.Type);
    }

    [Theory]
    [InlineData("", "a.txt", "text/plain")]
    [InlineData("a.txt", "", "text/plain")]
    [InlineData("a.txt", "a.txt", " ")]
    public void Constructor_WithEmptyName_Throws(string originalName, string storedName, string contentType)
    {
        Assert.Throws<ArgumentException>(() =>
            new Attachment(originalName, storedName, contentType, 10, AttachmentType.Text));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithInvalidSize_Throws(long size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Attachment("a.txt", "b.txt", "text/plain", size, AttachmentType.Text));
    }
}
