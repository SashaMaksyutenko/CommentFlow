using Comments.Application.Comments;
using Comments.Domain.Entities;

namespace Comments.Tests.Application;

public class CommentTreeBuilderTests
{
    private static readonly User Author = TestData.CreateUser(1, "Sasha1", "sasha@example.com");
    private static readonly DateTime Start = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Comment Reply(int id, int parentId, int minutes) =>
        TestData.CreateComment(id, parentId, Author, Start.AddMinutes(minutes));

    [Fact]
    public void Build_NestsEachReplyUnderItsParent()
    {
        // 1 (root)
        // ├── 2
        // │   └── 3
        // │       └── 4
        // └── 5
        var replies = new[] { Reply(2, 1, 1), Reply(3, 2, 2), Reply(4, 3, 3), Reply(5, 1, 4) };

        var tree = CommentTreeBuilder.Build(1, replies);

        Assert.Equal([2, 5], tree.Select(r => r.Id));
        var level2 = Assert.Single(tree[0].Replies);
        Assert.Equal(3, level2.Id);
        Assert.Equal(4, Assert.Single(level2.Replies).Id);
        Assert.Empty(tree[1].Replies);
    }

    [Fact]
    public void Build_SortsRepliesOldestFirst_EvenIfInputIsShuffled()
    {
        var replies = new[] { Reply(7, 1, 30), Reply(5, 1, 10), Reply(6, 1, 20) };

        var tree = CommentTreeBuilder.Build(1, replies);

        Assert.Equal([5, 6, 7], tree.Select(r => r.Id));
    }

    [Fact]
    public void Build_VeryDeepChain_KeepsAllLevels()
    {
        // 2 is a reply to 1, 3 to 2, ... 101 to 100
        var replies = Enumerable.Range(2, 100).Select(id => Reply(id, id - 1, id)).ToList();

        var tree = CommentTreeBuilder.Build(1, replies);

        var depth = 0;
        var level = tree;
        while (level.Count > 0)
        {
            depth++;
            level = level[0].Replies;
        }
        Assert.Equal(100, depth);
    }

    [Fact]
    public void Build_NoReplies_ReturnsEmptyList()
    {
        Assert.Empty(CommentTreeBuilder.Build(1, []));
    }
}
