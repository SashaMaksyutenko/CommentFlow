using Comments.Application.Attachments;
using Comments.Application.Captcha;
using Comments.Application.Common;
using Comments.Application.Users;
using Comments.Application.Validation;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public class CommentService : ICommentService
{
    // Fixed by the task: 25 comments per page
    public const int PageSize = 25;

    private readonly ICaptchaService _captchaService;
    private readonly IUserService _userService;
    private readonly ICommentRepository _comments;
    private readonly ICommentTextSanitizer _sanitizer;
    private readonly IAttachmentService _attachmentService;

    public CommentService(
        ICaptchaService captchaService,
        IUserService userService,
        ICommentRepository comments,
        ICommentTextSanitizer sanitizer,
        IAttachmentService attachmentService)
    {
        _captchaService = captchaService;
        _userService = userService;
        _comments = comments;
        _sanitizer = sanitizer;
        _attachmentService = attachmentService;
    }

    public async Task<CommentResponse> CreateAsync(
        CreateCommentRequest request,
        UploadedFile? file,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        // Checked before the captcha, so a typo in tags doesn't burn the captcha
        if (!_sanitizer.TrySanitize(request.Text, out var text, out var textError))
        {
            throw new FieldValidationException(nameof(CreateCommentRequest.Text), textError);
        }

        // Captcha goes before any DB work, so nothing else runs for bots
        var captchaIsValid = await _captchaService.ValidateAsync(request.CaptchaId, request.CaptchaAnswer, cancellationToken);
        if (!captchaIsValid)
        {
            throw new FieldValidationException(nameof(CreateCommentRequest.CaptchaAnswer),
                "Wrong or expired captcha. Please enter the new one.");
        }

        if (request.ParentId is not null && !await _comments.ExistsAsync(request.ParentId.Value, cancellationToken))
        {
            throw new FieldValidationException(nameof(CreateCommentRequest.ParentId),
                "The comment you are replying to does not exist.");
        }

        // After the captcha on purpose: resizing images is heavy work, bots shouldn't trigger it
        var attachment = file is null ? null : await _attachmentService.SaveAsync(file, cancellationToken);

        try
        {
            var user = await _userService.GetOrCreateAsync(request.UserName, request.Email, request.HomePage, cancellationToken);

            var comment = new Comment(
                user.Id,
                request.ParentId,
                text,
                Truncate(ipAddress, Comment.IpAddressMaxLength),
                Truncate(userAgent, Comment.UserAgentMaxLength));

            if (attachment is not null)
            {
                comment.Attach(attachment);
            }

            // Saves the comment and its attachment row together
            await _comments.AddAsync(comment, cancellationToken);

            return CommentResponse.From(comment, user);
        }
        catch
        {
            // The file is already on disk, don't leave it there without a comment
            if (attachment is not null)
            {
                await _attachmentService.DeleteFileAsync(attachment, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<PagedResponse<CommentResponse>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * PageSize;

        var (comments, totalCount) = await _comments.GetTopLevelPageAsync(
            query.SortBy, query.SortDirection, skip, PageSize, cancellationToken);

        var items = comments.Select(c => CommentResponse.From(c, c.User)).ToList();

        return new PagedResponse<CommentResponse>(items, query.Page, PageSize, totalCount);
    }

    public async Task<List<CommentResponse>?> GetRepliesAsync(int commentId, CancellationToken cancellationToken)
    {
        if (!await _comments.ExistsAsync(commentId, cancellationToken))
        {
            return null;
        }

        var replies = await _comments.GetAllRepliesAsync(commentId, cancellationToken);
        return CommentTreeBuilder.Build(commentId, replies);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
