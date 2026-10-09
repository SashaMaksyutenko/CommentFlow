using System.Net;
using Comments.Application.Captcha;
using Comments.Application.Users;
using Comments.Application.Validation;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public class CommentService : ICommentService
{
    private readonly ICaptchaService _captchaService;
    private readonly IUserService _userService;
    private readonly ICommentRepository _comments;

    public CommentService(ICaptchaService captchaService, IUserService userService, ICommentRepository comments)
    {
        _captchaService = captchaService;
        _userService = userService;
        _comments = comments;
    }

    public async Task<CommentResponse> CreateAsync(
        CreateCommentRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        // Captcha goes first, so nothing else runs for bots
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

        var user = await _userService.GetOrCreateAsync(request.UserName, request.Email, request.HomePage, cancellationToken);

        // Encode all HTML for now, until the allowed tags whitelist is added
        var text = WebUtility.HtmlEncode(request.Text.Trim());

        var comment = new Comment(
            user.Id,
            request.ParentId,
            text,
            Truncate(ipAddress, Comment.IpAddressMaxLength),
            Truncate(userAgent, Comment.UserAgentMaxLength));

        await _comments.AddAsync(comment, cancellationToken);

        return CommentResponse.From(comment, user);
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
