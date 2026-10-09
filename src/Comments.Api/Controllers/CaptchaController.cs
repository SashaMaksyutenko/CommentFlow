using Comments.Application.Captcha;
using Microsoft.AspNetCore.Mvc;

namespace Comments.Api.Controllers;

[ApiController]
[Route("api/captcha")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;

    public CaptchaController(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<CaptchaResponse>> Get(CancellationToken cancellationToken)
    {
        var challenge = await _captchaService.CreateAsync(cancellationToken);

        // data URL can be put straight into <img src="...">
        var image = "data:image/png;base64," + Convert.ToBase64String(challenge.PngImage);

        return Ok(new CaptchaResponse(challenge.Id, image));
    }
}

public record CaptchaResponse(string Id, string Image);
