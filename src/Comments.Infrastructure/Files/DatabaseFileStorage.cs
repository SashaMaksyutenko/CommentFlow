using Comments.Application.Attachments;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Files;

// Keeps uploaded files in the StoredFiles table. For hosts whose disk is wiped on every restart
// (free Render): files on disk would disappear there, rows in the database stay.
public class DatabaseFileStorage : IFileStorage
{
    private readonly DbContextOptions<CommentsDbContext> _options;

    public DatabaseFileStorage(DbContextOptions<CommentsDbContext> options)
    {
        _options = options;
    }

    // Every call uses its own short-lived DbContext, not the one of the request. The request's one
    // can hold changes that are not saved yet (a comment that failed to save, a status being changed),
    // and SaveChanges here must not save them together with the file.
    public async Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
    {
        await using var db = new CommentsDbContext(_options);

        var file = await db.StoredFiles.FindAsync([fileName], cancellationToken);
        if (file is null)
        {
            db.StoredFiles.Add(new StoredFile(fileName, content));
        }
        else
        {
            file.Content = content;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]?> ReadAsync(string fileName, CancellationToken cancellationToken)
    {
        await using var db = new CommentsDbContext(_options);

        return await db.StoredFiles
            .Where(f => f.FileName == fileName)
            .Select(f => f.Content)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        await using var db = new CommentsDbContext(_options);

        // One DELETE statement; does nothing if there is no such file, like deleting on disk
        await db.StoredFiles
            .Where(f => f.FileName == fileName)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
