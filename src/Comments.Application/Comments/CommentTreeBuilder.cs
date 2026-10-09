using Comments.Domain.Entities;

namespace Comments.Application.Comments;

public static class CommentTreeBuilder
{
    // Turns a flat list of all replies under rootId into a tree.
    // Replies are sorted oldest first, like a normal conversation.
    public static List<CommentResponse> Build(int rootId, IEnumerable<Comment> replies)
    {
        var sorted = replies
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .ToList();

        var nodesById = sorted.ToDictionary(c => c.Id, c => CommentResponse.From(c, c.User));
        var result = new List<CommentResponse>();

        foreach (var comment in sorted)
        {
            var node = nodesById[comment.Id];

            if (comment.ParentId == rootId)
            {
                result.Add(node);
            }
            else if (comment.ParentId is int parentId && nodesById.TryGetValue(parentId, out var parent))
            {
                parent.Replies.Add(node);
            }
        }

        return result;
    }
}
