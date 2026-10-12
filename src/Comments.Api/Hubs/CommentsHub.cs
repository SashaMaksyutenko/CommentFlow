using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

// Browsers connect here to get live updates.
// It is empty on purpose: clients only listen, they don't call any server methods.
// New comments still go through POST /api/comments, where they are validated.
public class CommentsHub : Hub<ICommentsClient>
{
    public const string Path = "/hubs/comments";
}
