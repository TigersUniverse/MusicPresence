namespace MusicPresence;

/// <summary>
/// Describes the success or error behind a provider's initialization phase
/// </summary>
public enum MusicInitializationStatus
{
    /// <summary>
    /// The provider was initialized successfully
    /// </summary>
    SUCCESS = 0x00,
    /// <summary>
    /// The provider failed to authenticate with their services
    /// </summary>
    NOT_AUTHORIZED = 0x01
}