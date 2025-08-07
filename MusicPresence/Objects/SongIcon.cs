namespace MusicPresence.Objects;

/// <summary>
/// A web song icon
/// </summary>
public struct SongIcon
{
    /// <summary>
    /// Width of the media (may be null depending on provider)
    /// </summary>
    public int? Width;
    /// <summary>
    /// Height of the media (may be null depending on provider)
    /// </summary>
    public int? Height;
    /// <summary>
    /// The Url of the media (may be null depending on provider)
    /// </summary>
    public string? Url;
}