using System.ComponentModel.DataAnnotations;

namespace Comments.Application.Validation;

// Built-in [Url] also accepts ftp:// and similar, here only http and https are allowed.
// Empty value is valid, add [Required] if the field is mandatory.
public class HttpUrlAttribute : ValidationAttribute
{
    public HttpUrlAttribute()
        : base("The {0} field must be a valid http or https URL.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not string text)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return IsHttpUrl(text);
    }

    // Also used for href in comment links
    public static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrEmpty(uri.Host);
    }
}
