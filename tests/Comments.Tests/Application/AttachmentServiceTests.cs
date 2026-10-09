using System.Text;
using Comments.Application.Attachments;
using Comments.Application.Validation;
using Comments.Domain.Enums;

namespace Comments.Tests.Application;

public class AttachmentServiceTests
{
    private readonly FakeImageProcessor _images = new();
    private readonly FakeStorage _storage = new();
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
    public async Task Image_IsProcessedAndStoredWithRealExtension()
    {
        _images.Result = new ProcessedImage([1, 2, 3], ".png", "image/png", 320, 240);

        // Uploaded as .jpg, but the processor says it's really a PNG
        var attachment = await _service.SaveAsync(new UploadedFile("photo.JPG", [9, 9, 9]), CancellationToken.None);

        Assert.Equal(AttachmentType.Image, attachment.Type);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.EndsWith(".png", attachment.StoredFileName);
        Assert.Equal(3, attachment.Size);
        Assert.Equal([1, 2, 3], _storage.Files[attachment.StoredFileName]);
    }

    [Fact]
    public async Task Image_ProcessorError_IsReturnedToUser()
    {
        _images.Error = "The file is not a JPG, GIF or PNG image.";

        var ex = await SaveFails("fake.png", [1, 2, 3]);

        Assert.Equal("The file is not a JPG, GIF or PNG image.", ex.Message);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Image_Over5Mb_IsRejectedWithoutProcessing()
    {
        await SaveFails("huge.png", new byte[AttachmentService.MaxImageSize + 1]);

        Assert.Equal(0, _images.CallCount);
    }

    [Theory]
    [InlineData(@"C:\Users\me\cat.txt", "cat.txt")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    public async Task OriginalName_KeepsOnlyFileName(string uploadedName, string expected)
    {
        var attachment = await _service.SaveAsync(new UploadedFile(uploadedName, Text("hi")), CancellationToken.None);

        Assert.Equal(expected, attachment.OriginalFileName);
    }

    private class FakeImageProcessor : IImageProcessor
    {
        public ProcessedImage? Result { get; set; }

        public string Error { get; set; } = "";

        public int CallCount { get; private set; }

        public bool TryProcess(byte[] content, out ProcessedImage? image, out string error)
        {
            CallCount++;
            image = Result;
            error = Error;
            return Result is not null;
        }
    }

    private class FakeStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public Task SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken)
        {
            Files[fileName] = content;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string fileName, CancellationToken cancellationToken)
        {
            Files.Remove(fileName);
            return Task.CompletedTask;
        }
    }
}
