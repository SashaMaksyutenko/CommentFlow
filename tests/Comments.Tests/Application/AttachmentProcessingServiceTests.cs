using Comments.Application.Attachments;
using Comments.Application.Attachments.Events;
using Comments.Domain.Entities;
using Comments.Domain.Enums;
using Comments.Tests.Fakes;

namespace Comments.Tests.Application;

public class AttachmentProcessingServiceTests
{
    private const string StoredName = "abc.png";
    private const string PendingName = "abc.png.upload";

    private readonly FakeAttachmentRepository _attachments = new();
    private readonly FakeFileStorage _storage = new();
    private readonly FakeImageProcessor _images = new();
    private readonly FakePublisher _publisher = new();
    private readonly AttachmentProcessingService _service;

    public AttachmentProcessingServiceTests()
    {
        _service = new AttachmentProcessingService(_attachments, _storage, _images, _publisher);
    }

    // An uploaded image that waits for processing, with its original on "disk"
    private Attachment AddWaitingImage(int id = 1)
    {
        var attachment = new Attachment("cat.png", StoredName, "image/png", 5000, AttachmentType.Image, AttachmentStatus.Processing).WithId(id);
        _attachments.Items.Add(attachment);
        _storage.Files[PendingName] = [9, 9, 9, 9, 9];
        return attachment;
    }

    [Fact]
    public async Task Process_GoodImage_SavesFinalFileAndMarksReady()
    {
        var attachment = AddWaitingImage();
        _images.Processed = new ProcessedImage([1, 2, 3], ".png", "image/png", 320, 240);

        await _service.ProcessAsync(1, CancellationToken.None);

        Assert.Equal(AttachmentStatus.Ready, attachment.Status);
        Assert.Equal(3, attachment.Size);
        Assert.Equal([1, 2, 3], _storage.Files[StoredName]);
        Assert.False(_storage.Files.ContainsKey(PendingName)); // the original is removed
        Assert.Equal(1, _attachments.SaveCount);

        var published = Assert.IsType<AttachmentProcessed>(Assert.Single(_publisher.Published));
        Assert.Equal("Ready", published.Attachment.Status);
        Assert.Equal(3, published.Attachment.Size);
        Assert.Equal("/uploads/" + StoredName, published.Attachment.Url);
    }

    [Fact]
    public async Task Process_ImageCanNotBeDecoded_MarksFailedAndLeavesNoFiles()
    {
        var attachment = AddWaitingImage();
        _images.Processed = null;

        await _service.ProcessAsync(1, CancellationToken.None);

        Assert.Equal(AttachmentStatus.Failed, attachment.Status);
        Assert.Empty(_storage.Files);
        Assert.Equal(1, _attachments.SaveCount);

        // A failure is also announced, clients must stop showing "processing"
        var published = Assert.IsType<AttachmentProcessed>(Assert.Single(_publisher.Published));
        Assert.Equal("Failed", published.Attachment.Status);
    }

    [Fact]
    public async Task Process_OriginalIsMissing_MarksFailed()
    {
        var attachment = AddWaitingImage();
        _storage.Files.Clear();

        await _service.ProcessAsync(1, CancellationToken.None);

        Assert.Equal(AttachmentStatus.Failed, attachment.Status);
        Assert.Equal(0, _images.ProcessCount);
    }

    [Fact]
    public async Task Process_CalledTwice_SecondCallDoesNothing()
    {
        AddWaitingImage();
        await _service.ProcessAsync(1, CancellationToken.None);

        await _service.ProcessAsync(1, CancellationToken.None);

        Assert.Equal(1, _images.ProcessCount);
        Assert.Equal(1, _attachments.SaveCount);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Process_UnknownId_DoesNothing()
    {
        await _service.ProcessAsync(999, CancellationToken.None);

        Assert.Equal(0, _images.ProcessCount);
        Assert.Equal(0, _attachments.SaveCount);
        Assert.Empty(_publisher.Published);
    }

    private class FakeAttachmentRepository : IAttachmentRepository
    {
        public List<Attachment> Items { get; } = [];

        public int SaveCount { get; private set; }

        public Task<Attachment?> FindAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
