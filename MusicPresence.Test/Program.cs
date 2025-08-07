using System.Text;
using IF.Lastfm.Core.Objects;
using MusicPresence;
using MusicPresence.Providers;
using MusicPresence.Test;
using SpotifyAPI.Web;

const string CONTROLS = "--=== s: Skip Song, r: Rewind Song, p: Pause Song, u: Unpause (Play) Song, c: Exit ===--";

bool awaitingInput = false;
MusicObject? musicObject = null;
Console.WriteLine("Please input a provider (Spotify/lastfm/Media)");
ProviderIdentifier providerIdentifier = (Console.ReadLine() ?? String.Empty).ToProvider();
switch (providerIdentifier)
{
    case ProviderIdentifier.Spotify:
        musicObject = new MusicObject(new SpotifyProvider());
        if (string.IsNullOrEmpty(musicObject.Settings.SpotifyClientId))
        {
            Console.WriteLine("Enter your Spotify ClientId");
            string? clientId = Console.ReadLine();
            if (string.IsNullOrEmpty(clientId)) throw new NullReferenceException();
            musicObject.Settings.SpotifyClientId = clientId;
            musicObject.Settings.SaveMusicSettings();
        }
        if (musicObject.Settings.SpotifyAccessToken == null)
        {
            PKCETokenResponse accessToken = (PKCETokenResponse) (await musicObject.Provider.Authenticate([
                musicObject.Settings.SpotifyClientId
            ]))!;
            musicObject.Settings.SpotifyAccessToken = accessToken;
            musicObject.Settings.SaveMusicSettings();
        }
        break;
    case ProviderIdentifier.lastfm:
        musicObject = new MusicObject(new LastfmProvider());
        string? key = null;
        string? secret = null;
        string? username = null;
        string? password = null;
        if (string.IsNullOrEmpty(musicObject.Settings.LastfmKey) ||
            string.IsNullOrEmpty(musicObject.Settings.LastfmSecret) ||
            musicObject.Settings.LastfmUser == null)
        {
            Console.WriteLine("Enter your Lastfm Key");
            key = Console.ReadLine();
            Console.WriteLine("Enter your Lastfm Secret");
            secret = Console.ReadLine();
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret)) throw new NullReferenceException();
            musicObject.Settings.LastfmKey = key;
            musicObject.Settings.LastfmSecret = secret;
            musicObject.Settings.SaveMusicSettings();
        }
        if (musicObject.Settings.LastfmUser == null)
        {
            Console.WriteLine("Enter your Lastfm Username");
            username = Console.ReadLine();
            Console.WriteLine("Enter your Lastfm Password");
            password = Console.ReadLine();
        }
        if(key != null && secret != null && username != null && password != null)
        {
            LastUserSession lastUserSession =
                (LastUserSession) (await musicObject.Provider.Authenticate([key, secret, username, password]))!;
            musicObject.Settings.LastfmUser = lastUserSession;
            musicObject.Settings.SaveMusicSettings();
        }
        break;
    case ProviderIdentifier.Media:
        musicObject = new MusicObject(new WindowsMediaProvider());
        break;
}
if(musicObject == null) return;
Console.Clear();
MusicInitializationStatus status = musicObject.Initialize();
if (status == MusicInitializationStatus.NOT_AUTHORIZED)
{
    Console.WriteLine("Failed to authenticate!");
    Console.ReadKey();
    Environment.Exit(0);
}
Task.Factory.StartNew(() =>
{
    while (true)
    {
        if(awaitingInput) continue;
        musicObject.Provider.UpdateStatus();
        Console.Clear();
        Console.WriteLine("IsPlaying: " + musicObject.Provider.IsPlaying);
        if (musicObject.Provider.IsPlaying)
        {
            Console.WriteLine("Song Name: " + musicObject.Provider.SongName);
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < musicObject.Provider.SongArtists.Length; i++)
            {
                string artist = musicObject.Provider.SongArtists[i];
                b.Append(artist);
                if(i >= musicObject.Provider.SongArtists.Length - 1) continue;
                b.Append(", ");
            }
            Console.WriteLine("Song Artists: " + b);
            int? songPositionMs = musicObject.Provider.SongPosition;
            int? songLengthMs = musicObject.Provider.SongLength;
            if(songPositionMs.HasValue && songLengthMs.HasValue)
                Console.WriteLine($"{Format(songPositionMs.Value)} / {Format(songLengthMs.Value)}");
            else if(songLengthMs.HasValue)
                Console.WriteLine(Format(songLengthMs.Value));
            Console.WriteLine(musicObject.Provider.SongId);
            Console.WriteLine(musicObject.Provider.SongUrl);
        }
        Console.WriteLine('\n');
        Console.WriteLine(CONTROLS);
        Thread.Sleep(1000);
    }
});
ReadKeyInput();

void ReadKeyInput()
{
    ConsoleKeyInfo key = Console.ReadKey();
    switch (key.KeyChar)
    {
        case 's':
            musicObject.Provider.Skip();
            break;
        case 'r':
            musicObject.Provider.Rewind();
            break;
        case 'p':
            musicObject.Provider.Pause();
            break;
        case 'u':
            musicObject.Provider.Play();
            break;
        case 'd':
            if(musicObject.Provider.GetType() != typeof(SpotifyProvider)) break;
            awaitingInput = true;
            Console.Clear();
            Console.WriteLine("Enter the Song Id to Play");
            string? id = Console.ReadLine();
            if(id == null)
            {
                awaitingInput = false;
                break;
            }
            ((SpotifyProvider) musicObject.Provider).PlaySong(id);
            awaitingInput = false;
            break;
        case 'c':
            Environment.Exit(0);
            break;
    }
    ReadKeyInput();
}

string Format(int ms)
{
    TimeSpan t = TimeSpan.FromMilliseconds(ms);
    return $"{t.Minutes:D2}m:{t.Seconds:D2}s";
}
