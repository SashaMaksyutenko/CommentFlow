using Comments.Domain.Entities;

namespace Comments.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByNameAndEmailAsync(string userName, string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
