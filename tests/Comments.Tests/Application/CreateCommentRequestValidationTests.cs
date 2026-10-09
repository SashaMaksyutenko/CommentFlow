using System.ComponentModel.DataAnnotations;
using Comments.Application.Comments;

namespace Comments.Tests.Application;

public class CreateCommentRequestValidationTests
{
    private static CreateCommentRequest ValidRequest() => new()
    {
        UserName = "Sasha1",
        Email = "sasha@example.com",
        HomePage = "https://example.com",
        Text = "Hello <i>world</i>",
        ParentId = null,
        CaptchaId = "abc123",
        CaptchaAnswer = "AB12CD"
    };

    // Same check ASP.NET runs for [ApiController] models; returns names of invalid fields
    private static List<string> Validate(CreateCommentRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).ToList();
    }

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(ValidRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Sasha 1")]
    [InlineData("Саша")]
    [InlineData("sasha_1")]
    [InlineData("<script>")]
    public void UserName_NotLatinLettersOrDigits_IsInvalid(string userName)
    {
        var request = ValidRequest();
        request.UserName = userName;

        Assert.Contains(nameof(CreateCommentRequest.UserName), Validate(request));
    }

    [Fact]
    public void UserName_TooLong_IsInvalid()
    {
        var request = ValidRequest();
        request.UserName = new string('a', 51);

        Assert.Contains(nameof(CreateCommentRequest.UserName), Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("sasha@")]
    [InlineData("sasha@localhost")]
    [InlineData("sasha@example")]
    [InlineData("sasha@example.c")]
    [InlineData("sa sha@example.com")]
    [InlineData("sasha@exa mple.com")]
    public void Email_WrongFormat_IsInvalid(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        Assert.Contains(nameof(CreateCommentRequest.Email), Validate(request));
    }

    [Theory]
    [InlineData("sasha@example.com")]
    [InlineData("first.last+tag@sub.example.co.uk")]
    [InlineData("UPPER@EXAMPLE.COM")]
    public void Email_CommonFormats_AreValid(string email)
    {
        var request = ValidRequest();
        request.Email = email;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/page?x=1")]
    public void HomePage_EmptyOrHttpUrl_IsValid(string? homePage)
    {
        var request = ValidRequest();
        request.HomePage = homePage;

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("/relative/path")]
    [InlineData("http://")]
    public void HomePage_NotHttpUrl_IsInvalid(string homePage)
    {
        var request = ValidRequest();
        request.HomePage = homePage;

        Assert.Contains(nameof(CreateCommentRequest.HomePage), Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Text_Empty_IsInvalid(string text)
    {
        var request = ValidRequest();
        request.Text = text;

        Assert.Contains(nameof(CreateCommentRequest.Text), Validate(request));
    }

    [Fact]
    public void Text_LongerThan5000_IsInvalid()
    {
        var request = ValidRequest();
        request.Text = new string('a', 5001);

        Assert.Contains(nameof(CreateCommentRequest.Text), Validate(request));
    }

    [Fact]
    public void Text_Exactly5000_IsValid()
    {
        var request = ValidRequest();
        request.Text = new string('a', 5000);

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ParentId_NotPositive_IsInvalid(int parentId)
    {
        var request = ValidRequest();
        request.ParentId = parentId;

        Assert.Contains(nameof(CreateCommentRequest.ParentId), Validate(request));
    }

    [Fact]
    public void Captcha_Missing_IsInvalid()
    {
        var request = ValidRequest();
        request.CaptchaId = "";
        request.CaptchaAnswer = "";

        var errors = Validate(request);

        Assert.Contains(nameof(CreateCommentRequest.CaptchaId), errors);
        Assert.Contains(nameof(CreateCommentRequest.CaptchaAnswer), errors);
    }
}
