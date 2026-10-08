namespace Comments.Domain.Entities;

public class Comment
{
    // Input limit only, the DB column is nvarchar(max)
    public const int TextMaxLength = 5000;

    // Max length of an IPv6 address as text
    public const int IpAddressMaxLength = 45;

    public const int UserAgentMaxLength = 512;

    // EF fills this field by name convention (_replies -> Replies)
    private readonly List<Comment> _replies = [];

    public Comment(int userId, int? parentId, string text, string? ipAddress, string? userAgent)
    {
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
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public int UserId { get; private set; }

    // null = top-level comment, otherwise a reply
    public int? ParentId { get; private set; }

    public string Text { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Name and e-mail can be anything, so also keep where the request came from
    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    // Set by EF when loading
    public User User { get; private set; } = null!;

    public Comment? Parent { get; private set; }

    public IReadOnlyCollection<Comment> Replies => _replies;

    public Attachment? Attachment { get; private set; }

    // Only one file per comment
    public void Attach(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        if (Attachment is not null)
        {
            throw new InvalidOperationException("Comment already has an attachment.");
        }

        Attachment = attachment;
    }
}
