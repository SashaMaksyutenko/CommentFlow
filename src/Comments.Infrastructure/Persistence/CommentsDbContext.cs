using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Persistence;

public class CommentsDbContext : DbContext
{
    public CommentsDbContext(DbContextOptions<CommentsDbContext> options)
        : base(options)
    {
    }
}
