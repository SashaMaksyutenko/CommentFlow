using Comments.Application.Attachments;
using Comments.Infrastructure.Files;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests.Api;

// Which file storage the API gets from the FileStorage:Provider setting
public class FileStorageRegistrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public FileStorageRegistrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private IFileStorage StorageWith(string? provider)
    {
        var factory = provider is null
            ? _factory
            : _factory.WithWebHostBuilder(builder => builder.UseSetting("FileStorage:Provider", provider));

        // The database storage is scoped, so it is taken from a scope, like in a request
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IFileStorage>();
    }

    [Fact]
    public void ByDefault_FilesAreOnDisk()
    {
        Assert.IsType<LocalFileStorage>(StorageWith(null));
    }

    [Theory]
    [InlineData("Database")]
    [InlineData("database")] // the value is read without regard to case
    public void Database_KeepsFilesInTheDatabase(string provider)
    {
        Assert.IsType<DatabaseFileStorage>(StorageWith(provider));
    }

    [Fact]
    public void AWrongValue_StopsTheApiAtStart()
    {
        Assert.ThrowsAny<InvalidOperationException>(() => StorageWith("Cloud"));
    }
}
