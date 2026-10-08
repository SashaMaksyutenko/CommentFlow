using Comments.Domain.Entities;

namespace Comments.Application.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<User> GetOrCreateAsync(string userName, string email, string? homePage, CancellationToken cancellationToken)
    {
        userName = userName.Trim();
        email = email.Trim();
        homePage = string.IsNullOrWhiteSpace(homePage) ? null : homePage.Trim();

        var user = await _users.FindByNameAndEmailAsync(userName, email, cancellationToken);

        if (user is null)
        {
            user = new User(userName, email, homePage);
            await _users.AddAsync(user, cancellationToken);
            return user;
        }

        // Keep the old home page if the new one is empty
        if (homePage is not null && homePage != user.HomePage)
        {
            user.ChangeHomePage(homePage);
            await _users.SaveChangesAsync(cancellationToken);
        }

        return user;
    }
}
