using Comments.Infrastructure.Captcha;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Comments.Tests.Infrastructure;

public class CaptchaServiceTests
{
    private readonly FakeImageGenerator _imageGenerator = new();
    private readonly CaptchaService _service;

    public CaptchaServiceTests()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _service = new CaptchaService(cache, _imageGenerator);
    }

    [Fact]
    public async Task Create_ReturnsIdAndImage_WithLatinLettersAndDigitsCode()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(challenge.Id));
        Assert.NotEmpty(challenge.PngImage);
        Assert.Equal(CaptchaService.CodeLength, _imageGenerator.LastCode.Length);
        Assert.Matches("^[A-Z0-9]+$", _imageGenerator.LastCode);
    }

    [Fact]
    public async Task Validate_CorrectAnswer_ReturnsTrue()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);

        Assert.True(await _service.ValidateAsync(challenge.Id, _imageGenerator.LastCode, CancellationToken.None));
    }

    [Fact]
    public async Task Validate_IgnoresCaseAndSpaces()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);
        var answer = $"  {_imageGenerator.LastCode.ToLowerInvariant()} ";

        Assert.True(await _service.ValidateAsync(challenge.Id, answer, CancellationToken.None));
    }

    [Fact]
    public async Task Validate_WrongAnswer_ReturnsFalse()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);

        Assert.False(await _service.ValidateAsync(challenge.Id, "WRONG1", CancellationToken.None));
    }

    [Fact]
    public async Task Validate_SameCaptchaTwice_SecondTimeFails()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);
        var code = _imageGenerator.LastCode;

        await _service.ValidateAsync(challenge.Id, code, CancellationToken.None);

        Assert.False(await _service.ValidateAsync(challenge.Id, code, CancellationToken.None));
    }

    [Fact]
    public async Task Validate_AfterWrongAnswer_CorrectAnswerFails()
    {
        var challenge = await _service.CreateAsync(CancellationToken.None);

        await _service.ValidateAsync(challenge.Id, "WRONG1", CancellationToken.None);

        Assert.False(await _service.ValidateAsync(challenge.Id, _imageGenerator.LastCode, CancellationToken.None));
    }

    [Theory]
    [InlineData("unknown-id", "ABC123")]
    [InlineData("", "ABC123")]
    [InlineData("some-id", "")]
    public async Task Validate_UnknownIdOrEmptyInput_ReturnsFalse(string id, string answer)
    {
        Assert.False(await _service.ValidateAsync(id, answer, CancellationToken.None));
    }

    // Remembers the code, so tests know the right answer
    private class FakeImageGenerator : ICaptchaImageGenerator
    {
        public string LastCode { get; private set; } = "";

        public byte[] GeneratePng(string code)
        {
            LastCode = code;
            return [1, 2, 3];
        }
    }
}
