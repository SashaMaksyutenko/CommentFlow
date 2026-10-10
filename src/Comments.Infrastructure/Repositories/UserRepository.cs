using Comments.Application.Users;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
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

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueIndexViolation(ex))
        {
            // Two requests tried to create the same user at the same time, the other one won.
            // Forget our copy, or EF would try to insert it again on the next save.
            _dbContext.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    // SQL Server error numbers for "duplicate key" in a unique index / unique constraint
    private static bool IsUniqueIndexViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
