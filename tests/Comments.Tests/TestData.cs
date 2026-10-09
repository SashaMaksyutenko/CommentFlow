using Comments.Domain.Entities;

namespace Comments.Tests;

// Ids, navigation properties and dates are normally set by EF and the DB.
// Tests set them through the private setters.
internal static class TestData
{
    public static T WithId<T>(this T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    public static User CreateUser(int id, string userName, string email)
    {
        return new User(userName, email, null).WithId(id);
    }

    public static Comment CreateTopLevelComment(int id, User user, DateTime createdAt)
    {
        return CreateComment(id, null, user, createdAt);
    }

    public static Comment CreateComment(int id, int? parentId, User user, DateTime createdAt)
    {
        var comment = new Comment(user.Id, parentId, $"Comment {id}", null, null).WithId(id);
        typeof(Comment).GetProperty(nameof(Comment.User))!.SetValue(comment, user);
        typeof(Comment).GetProperty(nameof(Comment.CreatedAt))!.SetValue(comment, createdAt);
        return comment;
    }
}
