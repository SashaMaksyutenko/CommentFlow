namespace Comments.Infrastructure.Files;

// One uploaded file kept in the database instead of on disk.
// Used on hosts whose disk is wiped on every restart (see DatabaseFileStorage).
public class StoredFile
{
    public StoredFile(string fileName, byte[] content)
    {
        FileName = fileName;
        Content = content;
    }

    // The generated name, the same as on disk: "<id>.png", or "<id>.png.upload" while waiting for processing
    public string FileName { get; private set; }

    // Saving a file under the same name again replaces its content, like writing a file on disk
    public byte[] Content { get; set; }
}
