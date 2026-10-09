namespace Comments.Application.Validation;

// A validation error that can only be found in a service (e.g. wrong captcha),
// not by the attributes on the request. Turned into a 400 response.
public class FieldValidationException : Exception
{
    public FieldValidationException(string field, string message)
        : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
