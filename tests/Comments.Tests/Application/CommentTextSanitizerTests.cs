using Comments.Application.Comments;

namespace Comments.Tests.Application;

public class CommentTextSanitizerTests
{
    private readonly CommentTextSanitizer _sanitizer = new();

    [Theory]
    [InlineData("Hello", "Hello")]
    [InlineData("  Hello  ", "Hello")]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    [InlineData("a < b and c > d", "a &lt; b and c &gt; d")]
    [InlineData("<i>italic</i>", "<i>italic</i>")]
    [InlineData("<strong>bold</strong>", "<strong>bold</strong>")]
    [InlineData("<code>var x = 1;</code>", "<code>var x = 1;</code>")]
    [InlineData("<strong><i>both</i></strong>", "<strong><i>both</i></strong>")]
    [InlineData("<I>upper</I>", "<i>upper</i>")]
    [InlineData("<i >spaces</i >", "<i>spaces</i>")]
    [InlineData("<code>&lt;int&gt;</code>", "<code>&lt;int&gt;</code>")]
    [InlineData("<!-- not a tag -->", "&lt;!-- not a tag --&gt;")]
    public void ValidText_IsKeptOrEncoded(string input, string expected)
    {
        Assert.True(_sanitizer.TrySanitize(input, out var html, out var error), error);
        Assert.Equal(expected, html);
    }

    [Theory]
    [InlineData("<a href=\"https://example.com\">link</a>", "<a href=\"https://example.com\">link</a>")]
    [InlineData("<a href=\"https://example.com\" title=\"Example\">link</a>", "<a href=\"https://example.com\" title=\"Example\">link</a>")]
    [InlineData("<a title='Single' href='http://example.com'>link</a>", "<a href=\"http://example.com\" title=\"Single\">link</a>")]
    [InlineData("<a href=\"https://example.com\" title=\"&quot; onclick=&quot;x\">link</a>", "<a href=\"https://example.com\" title=\"&quot; onclick=&quot;x\">link</a>")]
    public void ValidLink_IsRebuiltSafely(string input, string expected)
    {
        Assert.True(_sanitizer.TrySanitize(input, out var html, out var error), error);
        Assert.Equal(expected, html);
    }

    [Theory]
    [InlineData("<b>bold</b>", "not allowed")]
    [InlineData("<script>alert(1)</script>", "not allowed")]
    [InlineData("<img src=x onerror=alert(1)>", "not allowed")]
    [InlineData("<i>not closed", "not closed")]
    [InlineData("<strong><i>inner not closed</strong>", "does not match")]
    [InlineData("<i><strong>wrong order</i></strong>", "does not match")]
    [InlineData("closing only</i>", "does not match")]
    [InlineData("<i/>", "Self-closing")]
    [InlineData("<i class=\"x\">x</i>", "can't have attributes")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "http or https")]
    [InlineData("<a>no href</a>", "http or https")]
    [InlineData("<a href=\"https://example.com\" onclick=\"alert(1)\">x</a>", "not allowed in <a>")]
    [InlineData("<a href=\"https://a.com\" href=\"https://b.com\">x</a>", "not allowed in <a>")]
    [InlineData("<a href=https://example.com>x</a>", "must look like")]
    [InlineData("<a href=\"https://a.com\"><a href=\"https://b.com\">x</a></a>", "inside other links")]
    [InlineData("<i></i>", "empty")]
    [InlineData("<i>   </i>", "empty")]
    public void InvalidText_IsRejectedWithMessage(string input, string expectedMessagePart)
    {
        Assert.False(_sanitizer.TrySanitize(input, out var html, out var error));
        Assert.Contains(expectedMessagePart, error);
        Assert.Equal("", html);
    }
}
