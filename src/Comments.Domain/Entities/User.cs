namespace Comments.Domain.Entities;

// A user is identified by user name + e-mail
public class User
{
    public const int UserNameMaxLength = 50;
    public const int EmailMaxLength = 254;
    public const int HomePageMaxLength = 2048;

    public User(string userName, string email, string? homePage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        UserName = userName;
        Email = email;
        HomePage = homePage;
    }

    public int Id { get; private set; }

    public string UserName { get; private set; }

    public string Email { get; private set; }

    public string? HomePage { get; private set; }

    public void ChangeHomePage(string? homePage)
    {
        HomePage = homePage;
    }
}
