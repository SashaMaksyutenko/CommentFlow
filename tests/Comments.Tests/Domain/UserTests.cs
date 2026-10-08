using Comments.Domain.Entities;

namespace Comments.Tests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var user = new User("Sasha1", "sasha@example.com", "https://example.com");

        Assert.Equal("Sasha1", user.UserName);
        Assert.Equal("sasha@example.com", user.Email);
        Assert.Equal("https://example.com", user.HomePage);
    }

    [Fact]
    public void Constructor_WithoutHomePage_LeavesHomePageNull()
    {
        var user = new User("Sasha1", "sasha@example.com", null);

        Assert.Null(user.HomePage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyUserName_Throws(string userName)
    {
        Assert.Throws<ArgumentException>(() => new User(userName, "sasha@example.com", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEmail_Throws(string email)
    {
        Assert.Throws<ArgumentException>(() => new User("Sasha1", email, null));
    }
}
