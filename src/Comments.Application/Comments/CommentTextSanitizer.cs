using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Comments.Application.Validation;

namespace Comments.Application.Comments;

// Allows only <a href title>, <code>, <i>, <strong>. Every tag must be closed in the right order.
// The output is built from scratch: allowed tags are written again by us, all other text is encoded.
public class CommentTextSanitizer : ICommentTextSanitizer
{
    private static readonly string[] AllowedTags = ["a", "code", "i", "strong"];

    // Timeout protects from very slow regex on bad input
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // <tag ...> or </tag>. "a < b" is not a tag and stays plain text.
    private static readonly Regex TagRegex =
        new(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)([^<>]*)>", RegexOptions.None, RegexTimeout);

    // name="value" or name='value'
    private static readonly Regex AttributeRegex =
        new(@"\s+([a-zA-Z-]+)\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.None, RegexTimeout);

    public bool TrySanitize(string input, out string html, out string error)
    {
        var output = new StringBuilder();
        error = Sanitize(input.Trim(), output) ?? "";
        html = error == "" ? output.ToString() : "";
        return error == "";
    }

    // Writes safe HTML to output. Returns an error message, or null if the text is valid.
    private static string? Sanitize(string input, StringBuilder output)
    {
        var openTags = new Stack<string>();
        var hasText = false;
        var position = 0;

        foreach (Match tag in TagRegex.Matches(input))
        {
            hasText |= AppendText(output, input[position..tag.Index]);
            position = tag.Index + tag.Length;

            var isClosing = tag.Groups[1].Value == "/";
            var name = tag.Groups[2].Value.ToLowerInvariant();
            var attributes = tag.Groups[3].Value;

            if (!AllowedTags.Contains(name))
            {
                return $"Tag <{name}> is not allowed. Allowed tags: <a>, <code>, <i>, <strong>.";
            }

            if (isClosing)
            {
                if (openTags.Count == 0 || openTags.Peek() != name || !string.IsNullOrWhiteSpace(attributes))
                {
                    return $"Closing tag </{name}> does not match any open tag.";
                }

                openTags.Pop();
                output.Append($"</{name}>");
                continue;
            }

            if (attributes.TrimEnd().EndsWith('/'))
            {
                return $"Self-closing tag <{name}/> is not allowed, use <{name}></{name}>.";
            }

            if (name == "a")
            {
                if (openTags.Contains("a"))
                {
                    return "Links can't be inside other links.";
                }

                var linkError = AppendLinkTag(output, attributes);
                if (linkError is not null)
                {
                    return linkError;
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(attributes))
                {
                    return $"Tag <{name}> can't have attributes.";
                }

                output.Append($"<{name}>");
            }

            openTags.Push(name);
        }

        hasText |= AppendText(output, input[position..]);

        if (openTags.Count > 0)
        {
            return $"Tag <{openTags.Peek()}> is not closed.";
        }

        return hasText ? null : "Comment text is empty.";
    }

    // Decode first, so "&lt;" typed by the user stays "&lt;" and not "&amp;lt;"
    private static bool AppendText(StringBuilder output, string text)
    {
        var decoded = WebUtility.HtmlDecode(text);
        output.Append(WebUtility.HtmlEncode(decoded));
        return !string.IsNullOrWhiteSpace(decoded);
    }

    private static string? AppendLinkTag(StringBuilder output, string attributes)
    {
        string? href = null;
        string? title = null;

        foreach (Match attribute in AttributeRegex.Matches(attributes))
        {
            var name = attribute.Groups[1].Value.ToLowerInvariant();
            var value = attribute.Groups[2].Success ? attribute.Groups[2].Value : attribute.Groups[3].Value;
            value = WebUtility.HtmlDecode(value);

            if (name == "href" && href is null)
            {
                href = value;
            }
            else if (name == "title" && title is null)
            {
                title = value;
            }
            else
            {
                return $"Attribute \"{name}\" is not allowed in <a> (only href and title, once each).";
            }
        }

        // Anything left that is not name="value", e.g. href=https://x.com without quotes
        if (AttributeRegex.Replace(attributes, "").Trim() != "")
        {
            return "Attributes in <a> must look like href=\"...\".";
        }

        if (href is null || !HttpUrlAttribute.IsHttpUrl(href))
        {
            return "Link <a> needs href with an http or https address.";
        }

        output.Append($"<a href=\"{WebUtility.HtmlEncode(href.Trim())}\"");
        if (title is not null)
        {
            output.Append($" title=\"{WebUtility.HtmlEncode(title)}\"");
        }
        output.Append('>');

        return null;
    }
}
