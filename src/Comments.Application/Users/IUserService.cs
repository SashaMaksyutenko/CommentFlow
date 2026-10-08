using Comments.Domain.Entities;

namespace Comments.Application.Users;

public interface IUserService
{
    Task<User> GetOrCreateAsync(string userName, string email, string? homePage, CancellationToken cancellationToken);
}
