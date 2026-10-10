using Comments.Domain.Entities;

namespace Comments.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByNameAndEmailAsync(string userName, string email, CancellationToken cancellationToken);

    // Returns false if a user with the same name + e-mail already exists
    // (another request created it a moment ago)
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
