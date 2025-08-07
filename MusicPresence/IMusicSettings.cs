using IF.Lastfm.Core.Objects;
using SpotifyAPI.Web;

namespace MusicPresence;

/// <summary>
/// The settings for MusicProviders to use
/// </summary>
public interface IMusicSettings
{
    /// <summary>
    /// The current authenticated user's token information for Spotify
    /// </summary>
    public PKCETokenResponse? SpotifyAccessToken { get; set; }
    /// <summary>
    /// The client id for the Spotify app to use
    /// </summary>
    public string SpotifyClientId { get; set; }
    /// <summary>
    /// The lastfm application key
    /// </summary>
    public string LastfmKey { get; set; }
    /// <summary>
    /// The lastfm application secret
    /// </summary>
    public string LastfmSecret { get; set; }
    /// <summary>
    /// The current authenticated user's token information for lastfm
    /// </summary>
    public LastUserSession? LastfmUser { get; set; }

    /// <summary>
    /// Saves the current configuration
    /// </summary>
    public void SaveMusicSettings();
}