using MusicPresence.Providers;
using SpotifyAPI.Web.Auth;

namespace MusicPresence;

/// <summary>
/// The MusicObject easily interfaces with MusicProviders providing common utilities and easy initialization
/// </summary>
public class MusicObject
{
    /// <summary>
    /// An authorization port for all providers to adhere by (if possible) for callback authentication
    /// </summary>
    public const int AUTHORIZATION_PORT = 1643;
    private const string SETTINGS_FILE_NAME = "musicpresence.settings";

    /// <summary>
    /// Optional override for opening a URL in the browser
    /// </summary>
    public static Action<Uri> OpenInBrowser = BrowserUtil.Open;
    
    /// <summary>
    /// The music provider
    /// </summary>
    public IMusicProvider Provider { get; private set; }
    /// <summary>
    /// The current settings for Providers to pull configuration from
    /// </summary>
    public IMusicSettings Settings => _settings;

    private IMusicSettings _settings;

    /// <summary>
    /// Creates a MusicObject with the default configuration implementation
    /// </summary>
    /// <param name="provider">The Music Provider to use</param>
    /// <param name="workingDirectory">The directory to save the default config to (working directory if left empty)</param>
    /// <exception cref="DirectoryNotFoundException">The specified directory does not exist</exception>
    public MusicObject(IMusicProvider provider, string workingDirectory = "")
    {
        if (!string.IsNullOrEmpty(workingDirectory) && !Directory.Exists(workingDirectory)) throw new DirectoryNotFoundException();
        string settingsFilePath = Path.Combine(workingDirectory, SETTINGS_FILE_NAME);
        _settings = DefaultMusicSettings.New(settingsFilePath);
        Provider = provider;
    }

    /// <summary>
    /// Creates a MusicObject with a custom configuration implementation
    /// </summary>
    /// <param name="provider">The Music Provider to use</param>
    /// <param name="settings">The custom Settings implementation</param>
    public MusicObject(IMusicProvider provider, IMusicSettings settings)
    {
        _settings = settings;
        Provider = provider;
    }

    /// <summary>
    /// Initializes the current MusicProvider
    /// This method automatically renews expired tokens (if supported by the provider)
    /// </summary>
    /// <returns>The initialization status for the Provider</returns>
    public MusicInitializationStatus Initialize()
    {
        if (Provider.GetType() == typeof(SpotifyProvider))
            ((SpotifyProvider) Provider).OnTokenResponse += token =>
            {
                Settings.SpotifyAccessToken = token;
                Settings.SaveMusicSettings();
            };
        MusicInitializationStatus status = Provider.Initialize(ref _settings);
        if (status == MusicInitializationStatus.NOT_AUTHORIZED) status = Provider.Initialize(ref _settings);
        return status;
    }
}