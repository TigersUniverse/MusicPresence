using MusicPresence.Objects;

namespace MusicPresence.Providers;

/// <summary>
/// Provides a way to interact with a specific streaming platform
/// </summary>
public interface IMusicProvider
{
    /// <summary>
    /// If the player is currently playing media
    /// </summary>
    public bool IsPlaying { get; }
    /// <summary>
    /// The name of the media being played
    /// </summary>
    public string SongName { get; }
    /// <summary>
    /// All the artists who created the media
    /// </summary>
    public string[] SongArtists { get; }
    /// <summary>
    /// Any web icons available for the media
    /// </summary>
    public SongIcon[] SongIconURLs { get; }
    /// <summary>
    /// The current position of the song in milliseconds
    /// May be null depending on if the provider supports this feature and if a song is playing
    /// </summary>
    public int? SongPosition { get; }
    /// <summary>
    /// The length of the song in milliseconds
    /// May be null depending on if the provider supports this feature and if a song is playing
    /// </summary>
    public int? SongLength { get; }
    /// <summary>
    /// An identifier for a song
    /// May be empty depending on provider support
    /// </summary>
    public string SongId { get; }
    /// <summary>
    /// A URI for a song
    /// This should be used if a SongId is not provided
    /// </summary>
    public string SongUrl { get; }

    /// <summary>
    /// Authenticates a Provider if necessary
    /// </summary>
    /// <param name="args">Array of parameters for authentication, different for every provider</param>
    /// <returns>Optional object for saving to MusicSettings</returns>
    public Task<object?> Authenticate(object[] args);
    /// <summary>
    /// Initializes a Provider
    /// </summary>
    /// <param name="musicSettings">Reference to the current MusicObject's Settings</param>
    /// <returns>Initialization Status for if the Provider was initialized successfully</returns>
    public MusicInitializationStatus Initialize(ref IMusicSettings musicSettings);

    /// <summary>
    /// Resumes the current media
    /// </summary>
    public void Play();
    /// <summary>
    /// Pauses the current media
    /// </summary>
    public void Pause();
    /// <summary>
    /// Skips to the next media in the queue
    /// </summary>
    public void Skip();
    /// <summary>
    /// Rewinds to the previously playing media
    /// </summary>
    public void Rewind();
    /// <summary>
    /// Updates all information for a Provider
    /// This should be used sparingly because API calls happen here and you don't want to be rate-limited!
    /// </summary>
    public void UpdateStatus();
}