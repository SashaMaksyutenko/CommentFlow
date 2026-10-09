using System.ComponentModel.DataAnnotations;
using Comments.Application.Common;

namespace Comments.Application.Comments;

// Defaults give LIFO: newest comments first
public class GetCommentsQuery
{
    // Upper limit keeps (page - 1) * 25 far from int overflow
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    // [EnumDataType] rejects numbers that are not in the enum, like sortBy=7
    [EnumDataType(typeof(CommentSortField))]
    public CommentSortField SortBy { get; set; } = CommentSortField.CreatedAt;

    [EnumDataType(typeof(SortDirection))]
    public SortDirection SortDirection { get; set; } = SortDirection.Desc;
}
