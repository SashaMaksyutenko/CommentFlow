using Comments.Application.Validation;
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
        homePage = NormalizeHomePage(homePage);

        var user = await _users.FindByNameAndEmailAsync(userName, email, cancellationToken);

        if (user is null)
        {
            user = new User(userName, email, homePage);
            if (await _users.TryAddAsync(user, cancellationToken))
            {
                return user;
            }

            // Another request created the same user between our "find" and "add", use that one
            user = await _users.FindByNameAndEmailAsync(userName, email, cancellationToken)
                ?? throw new InvalidOperationException("User exists but could not be loaded.");
        }

        // Keep the old home page if the new one is empty
        if (homePage is not null && homePage != user.HomePage)
        {
            user.ChangeHomePage(homePage);
            await _users.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    // Saves the URL in its escaped form: quotes, spaces and <> become %22, %20, %3C...
    // So even a careless client can't get broken HTML out of a home page address.
    private static string? NormalizeHomePage(string? homePage)
    {
        if (string.IsNullOrWhiteSpace(homePage) || !Uri.TryCreate(homePage.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        // Escaping can make the address longer than the column allows
        if (uri.AbsoluteUri.Length > User.HomePageMaxLength)
        {
            throw new FieldValidationException("HomePage", "Home page address is too long.");
        }

        return uri.AbsoluteUri;
    }
}
