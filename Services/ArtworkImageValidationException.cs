namespace ARTISTO.Services;

public sealed class ArtworkImageValidationException : Exception
{
    public ArtworkImageValidationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
