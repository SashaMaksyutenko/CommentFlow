using System.Text;
using Comments.Application.Attachments;
using Comments.Application.Validation;
using Comments.Domain.Enums;
using Comments.Tests.Fakes;

namespace Comments.Tests.Application;

public class AttachmentServiceTests
{
    private readonly FakeImageProcessor _images = new();
    private readonly FakeFileStorage _storage = new();
    private readonly AttachmentService _service;

    public AttachmentServiceTests()
    {
        _service = new AttachmentService(_images, _storage);
    }

    private static byte[] Text(string value) => Encoding.UTF8.GetBytes(value);

    private Task<FieldValidationException> SaveFails(string fileName, byte[] content) =>
        Assert.ThrowsAsync<FieldValidationException>(() =>
            _service.SaveAsync(new UploadedFile(fileName, content), CancellationToken.None));

    [Fact]
    public async Task TextFile_IsStoredUnderGeneratedName()
    {
        var content = Text("Привіт, hello!");

        var attachment = await _service.SaveAsync(new UploadedFile("notes.txt", content), CancellationToken.None);

        Assert.Equal(AttachmentType.Text, attachment.Type);
        Assert.Equal("notes.txt", attachment.OriginalFileName);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal(content.Length, attachment.Size);
        Assert.Matches("^[0-9a-f]{32}\\.txt$", attachment.StoredFileName);
        Assert.Equal(content, _storage.Files[attachment.StoredFileName]);
    }

    [Fact]
    public async Task TextFile_Exactly100Kb_IsAllowed()
    {
        var content = Text(new string('a', AttachmentService.MaxTextSize));

        var attachment = await _service.SaveAsync(new UploadedFile("big.txt", content), CancellationToken.None);

        Assert.Equal(AttachmentService.MaxTextSize, attachment.Size);
    }

    [Fact]
    public async Task TextFile_Over100Kb_IsRejected()
    {
        var ex = await SaveFails("big.txt", Text(new string('a', AttachmentService.MaxTextSize + 1)));

        Assert.Equal(AttachmentService.FieldName, ex.Field);
        Assert.Contains("100 KB", ex.Message);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task BinaryFileNamedTxt_IsRejected()
    {
        await SaveFails("program.txt", [0x4D, 0x5A, 0x00, 0x01]);

        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task TextFileNotInUtf8_IsRejected()
    {
        // 0xC3 starts a 2-byte UTF-8 character, 0x28 can't continue it
        var ex = await SaveFails("bad.txt", [0x48, 0x69, 0xC3, 0x28]);

        Assert.Contains("UTF-8", ex.Message);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("doc.pdf")]
    [InlineData("image.svg")]
    [InlineData("noextension")]
    public async Task OtherFileTypes_AreRejected(string fileName)
    {
        var ex = await SaveFails(fileName, Text("data"));

        Assert.Contains("Only JPG, GIF, PNG images and TXT files", ex.Message);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task EmptyFile_IsRejected()
    {
        await SaveFails("empty.txt", []);
    }

    [Fact]
    public async Task Image_OriginalIsKeptForProcessing_NotServedYet()
    {
        // Uploaded as .jpg, but the quick check says it's really a PNG
        var attachment = await _service.SaveAsync(new UploadedFile("photo.JPG", [9, 9, 9]), CancellationToken.None);

        Assert.Equal(AttachmentType.Image, attachment.Type);
        Assert.Equal(AttachmentStatus.Processing, attachment.Status);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.EndsWith(".png", attachment.StoredFileName);

        // Only the untouched original exists, under the name the API doesn't serve
        var storedFile = Assert.Single(_storage.Files);
        Assert.Equal(attachment.StoredFileName + ".upload", storedFile.Key);
        Assert.Equal([9, 9, 9], storedFile.Value);

        // The heavy processing was not started here
        Assert.Equal(0, _images.ProcessCount);
    }

    [Fact]
    public async Task Image_QuickCheckError_IsReturnedToUser()
    {
        _images.Format = null;

        var ex = await SaveFails("fake.png", [1, 2, 3]);

        Assert.Equal("The file is not a JPG, GIF or PNG image.", ex.Message);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Image_Over5Mb_IsRejectedWithoutEvenChecking()
    {
        await SaveFails("huge.png", new byte[AttachmentService.MaxImageSize + 1]);

        Assert.Equal(0, _images.InspectCount);
    }

    [Fact]
    public async Task DeleteFile_RemovesFinalFileAndWaitingOriginal()
    {
        var image = await _service.SaveAsync(new UploadedFile("photo.png", [9, 9, 9]), CancellationToken.None);
        var text = await _service.SaveAsync(new UploadedFile("notes.txt", Text("hi")), CancellationToken.None);
        Assert.Equal(2, _storage.Files.Count);

        await _service.DeleteFileAsync(image, CancellationToken.None);
        await _service.DeleteFileAsync(text, CancellationToken.None);

        Assert.Empty(_storage.Files);
    }

    [Theory]
    [InlineData(@"C:\Users\me\cat.txt", "cat.txt")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    public async Task OriginalName_KeepsOnlyFileName(string uploadedName, string expected)
    {
        var attachment = await _service.SaveAsync(new UploadedFile(uploadedName, Text("hi")), CancellationToken.None);

        Assert.Equal(expected, attachment.OriginalFileName);
    }
}
