namespace Comments.Domain.Entities;

/// <summary>
/// A comment left by a user. A comment with no parent is a top-level comment;
/// a comment with a parent is a reply. Replies can have their own replies,
/// so the comments form a tree with unlimited depth.
/// </summary>
public class Comment
{
    // Limits are kept here, in one place, so the EF configuration (database columns)
    // and the request validation (step 8) always use the same numbers.

    // Maximum length of the text the user can type. Checked by the request validation only,
    // the database column itself is nvarchar(max) (see CommentConfiguration for why).
    public const int TextMaxLength = 5000;

    // 45 characters is the longest text form of an IP address
    // (an IPv6 address with an embedded IPv4 part).
    public const int IpAddressMaxLength = 45;

    // Real browsers send User-Agent strings of 100-300 characters;
    // 512 leaves a safe margin. Longer values are cut before saving.
    public const int UserAgentMaxLength = 512;

    // The list behind the public Replies property. It is private so that code outside
    // this class cannot add or remove replies directly (encapsulation).
    // EF Core finds this field by its name (_replies -> Replies) and fills it when loading.
    private readonly List<Comment> _replies = [];

    // EF Core also uses this constructor when it loads a comment from the database:
    // it matches the parameter names (userId, parentId, ...) to the property names.
    public Comment(int userId, int? parentId, string text, string? ipAddress, string? userAgent)
    {
        // Guard clauses: a comment object can never exist in an invalid state.
        // Detailed, user-friendly validation happens earlier, in the Application layer (step 8).
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        if (parentId is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parentId.Value);
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        UserId = userId;
        ParentId = parentId;
        Text = text;
        IpAddress = ipAddress;
        UserAgent = userAgent;

        // Always UTC, so the time does not depend on the server's time zone.
        // When EF loads a comment, it overwrites this with the value stored in the database.
        CreatedAt = DateTime.UtcNow;
    }

    // Private setters: values are set only by the constructor (or by EF when loading),
    // nobody can change them later from outside.
    public int Id { get; private set; }

    // Foreign key to the author (Users table).
    public int UserId { get; private set; }

    // Foreign key to the parent comment. null means "this is a top-level comment".
    public int? ParentId { get; private set; }

    // The comment text. It will contain only the allowed HTML tags (sanitized in step 9).
    public string Text { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Client data that helps to identify who really posted the comment,
    // because the user name and e-mail can be typed in by anyone.
    // Both are optional: e.g. a reverse proxy or a test server may not provide them.
    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    // Navigation properties: EF Core fills them when a query asks for them (e.g. with Include).
    // "= null!" tells the compiler "trust me, it will not be null after loading";
    // every comment has an author, the database guarantees that with a NOT NULL foreign key.
    public User User { get; private set; } = null!;

    // The parent comment; null for top-level comments.
    public Comment? Parent { get; private set; }

    // Read-only view of the replies: callers can read them but cannot modify the list.
    public IReadOnlyCollection<Comment> Replies => _replies;
}
