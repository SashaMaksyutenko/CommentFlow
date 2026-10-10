using Comments.Application.Users;
using Comments.Domain.Entities;

namespace Comments.Tests.Application;

public class UserServiceTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_repository);
    }

    [Fact]
    public async Task GetOrCreate_NewUser_AddsUser()
    {
        var user = await _service.GetOrCreateAsync("Sasha1", "sasha@example.com", "https://example.com", CancellationToken.None);

        Assert.Single(_repository.Users);
        Assert.Equal("Sasha1", user.UserName);
        Assert.Equal("https://example.com", user.HomePage);
    }

    [Fact]
    public async Task GetOrCreate_ExistingUser_ReturnsSameUser()
    {
        var existing = new User("Sasha1", "sasha@example.com", null);
        _repository.Users.Add(existing);

        var user = await _service.GetOrCreateAsync("Sasha1", "sasha@example.com", null, CancellationToken.None);

        Assert.Same(existing, user);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task GetOrCreate_TrimsInput()
    {
        var user = await _service.GetOrCreateAsync("  Sasha1 ", " sasha@example.com ", "   ", CancellationToken.None);

        Assert.Equal("Sasha1", user.UserName);
        Assert.Equal("sasha@example.com", user.Email);
        Assert.Null(user.HomePage);
    }

    [Fact]
    public async Task GetOrCreate_ExistingUserWithNewHomePage_UpdatesHomePage()
    {
        _repository.Users.Add(new User("Sasha1", "sasha@example.com", "https://old.com"));

        var user = await _service.GetOrCreateAsync("Sasha1", "sasha@example.com", "https://new.com", CancellationToken.None);

        Assert.Equal("https://new.com", user.HomePage);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task GetOrCreate_ExistingUserWithoutHomePage_KeepsOldHomePage()
    {
        _repository.Users.Add(new User("Sasha1", "sasha@example.com", "https://old.com"));

        var user = await _service.GetOrCreateAsync("Sasha1", "sasha@example.com", null, CancellationToken.None);

        Assert.Equal("https://old.com", user.HomePage);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task GetOrCreate_SameUserCreatedByParallelRequest_ReturnsThatUser()
    {
        var winner = new User("Sasha1", "sasha@example.com", null);
        _repository.CreatedByOtherRequest = winner;

        var user = await _service.GetOrCreateAsync("Sasha1", "sasha@example.com", null, CancellationToken.None);

        Assert.Same(winner, user);
        Assert.Single(_repository.Users);
    }

    // Simple in-memory repository, so the test doesn't need a database
    private class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public int SaveCount { get; private set; }

        public Task<User?> FindByNameAndEmailAsync(string userName, string email, CancellationToken cancellationToken)
        {
            var user = Users.FirstOrDefault(u =>
                string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(user);
        }

        // Set it to pretend another request inserts this user right before our insert
        public User? CreatedByOtherRequest { get; set; }

        public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
        {
            if (CreatedByOtherRequest is not null)
            {
                Users.Add(CreatedByOtherRequest);
                return Task.FromResult(false);
            }

            Users.Add(user);
            SaveCount++;
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
