using System.ComponentModel.DataAnnotations;
using Comments.Application.Validation;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public class CreateCommentRequest
{
    [Required]
    [MaxLength(User.UserNameMaxLength)]
    [RegularExpression("^[A-Za-z0-9]+$", ErrorMessage = "User name can contain only Latin letters and digits.")]
    public string UserName { get; set; } = "";

    // Stricter than [EmailAddress]: the domain needs a dot and a 2+ letter ending
    [Required]
    [MaxLength(User.EmailMaxLength)]
    [RegularExpression(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", ErrorMessage = "E-mail format is invalid.")]
    public string Email { get; set; } = "";

    [MaxLength(User.HomePageMaxLength)]
    [HttpUrl]
    public string? HomePage { get; set; }

    [Required]
    [MaxLength(Comment.TextMaxLength)]
    public string Text { get; set; } = "";

    // null for a top-level comment
    [Range(1, int.MaxValue)]
    public int? ParentId { get; set; }

    [Required]
    [MaxLength(64)]
    public string CaptchaId { get; set; } = "";

    [Required]
    [MaxLength(20)]
    public string CaptchaAnswer { get; set; } = "";
}
