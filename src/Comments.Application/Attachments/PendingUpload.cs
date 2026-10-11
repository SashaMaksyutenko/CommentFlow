namespace Comments.Application.Attachments;

// An uploaded image waits for processing under "<stored name>.upload".
// The API serves only .jpg/.png/.gif/.txt, so nobody can download this unchecked original.
public static class PendingUpload
{
    public const string Suffix = ".upload";

    public static string NameFor(string storedFileName) => storedFileName + Suffix;
}
