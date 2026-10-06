namespace ARTISTO.Services;

public sealed class ArtworkImageValidationException : Exception
{
    public ArtworkImageValidationException(string message)
        : base(message)
    {
    }
}
