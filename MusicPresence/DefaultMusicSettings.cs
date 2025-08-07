using IF.Lastfm.Core.Objects;
using Newtonsoft.Json;
using SpotifyAPI.Web;

namespace MusicPresence;

public class DefaultMusicSettings : IMusicSettings
{
    public PKCETokenResponse? SpotifyAccessToken { get; set; }
    public string SpotifyClientId { get; set; } = String.Empty;
    public string LastfmKey { get; set; } = String.Empty;
    public string LastfmSecret { get; set; } = String.Empty;
    public LastUserSession? LastfmUser { get; set; }

    private string fileLocation;
    
    [JsonConstructor] private DefaultMusicSettings(){}
    private DefaultMusicSettings(string fileLocation) => this.fileLocation = fileLocation;

    public void SaveMusicSettings() => File.WriteAllText(fileLocation, JsonConvert.SerializeObject(this));

    /// <summary>
    /// Loads an existing Default Music Configuration
    /// </summary>
    /// <param name="fileLocation">The location of the file to load from</param>
    /// <returns>The new configuration object</returns>
    /// <exception cref="NullReferenceException">The file does not exist</exception>
    public static DefaultMusicSettings New(string fileLocation)
    {
        if (!File.Exists(fileLocation)) return new DefaultMusicSettings(fileLocation);
        string fileText = File.ReadAllText(fileLocation);
        DefaultMusicSettings? musicSettings = JsonConvert.DeserializeObject<DefaultMusicSettings>(fileText);
        if (musicSettings == null) throw new NullReferenceException();
        musicSettings.fileLocation = fileLocation;
        return musicSettings;
    }
}