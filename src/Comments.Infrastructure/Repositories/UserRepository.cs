using Comments.Application.Users;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CommentsDbContext _dbContext;

    public UserRepository(CommentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // DB collation is case-insensitive, so "Sasha" and "sasha" match
    public Task<User?> FindByNameAndEmailAsync(string userName, string email, CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == userName && u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
