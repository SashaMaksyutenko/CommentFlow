using System.ComponentModel.DataAnnotations;
using Comments.Domain.Entities;

namespace Comments.Application.Comments;

// A preview needs only the text, the same limits as for a real comment
public class PreviewCommentRequest
{
    [Required]
    [MaxLength(Comment.TextMaxLength)]
    public string Text { get; set; } = "";
}
