using Comments.Application.Attachments;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

// Gives out uploaded files: /uploads/<generated name>. The files are read through IFileStorage,
// so this works the same whether they are kept on disk or in the database.
[ApiController]
[Route(AttachmentResponse.UrlPrefix)]
public class UploadsController : ControllerBase
{
    // Only our four file types. Anything else returns 404, for example an image
    // that is still waiting for processing ("<name>.png.upload").
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        // Text files are saved as UTF-8, say so or Cyrillic may look broken
        [".txt"] = "text/plain; charset=utf-8",
    };

    private readonly IFileStorage _storage;

    public UploadsController(IFileStorage storage)
    {
        _storage = storage;
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> Get(string fileName, CancellationToken cancellationToken)
    {
        // Only a plain name of an allowed type: "../appsettings.json" and the like are not our files
        if (Path.GetFileName(fileName) != fileName ||
            !ContentTypes.TryGetValue(Path.GetExtension(fileName), out var contentType))
        {
            return NotFound();
        }

        var content = await _storage.ReadAsync(fileName, cancellationToken);
        if (content is null)
        {
            return NotFound();
        }

        // A file never changes under its generated name, so the browser may keep it for a year
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(content, contentType);
    }
}
