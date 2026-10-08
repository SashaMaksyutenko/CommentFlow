namespace Comments.Domain.Entities;

/// <summary>
/// The author of comments. Identified by the pair of user name and e-mail.
/// </summary>
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
}
