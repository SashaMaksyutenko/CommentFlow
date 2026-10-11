namespace Comments.Domain.Enums;

public enum AttachmentStatus
{
    // Uploaded, waiting for the background processing (images only)
    Processing,

    // The file can be shown and downloaded
    Ready,

    // Processing failed, there is no file to show
    Failed
}
