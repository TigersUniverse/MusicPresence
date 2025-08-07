using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Helpers;
using IF.Lastfm.Core.Objects;
using MusicPresence.Errors;
using MusicPresence.Objects;

namespace MusicPresence.Providers;

public class LastfmProvider : IMusicProvider
{
    private const string FEATURE_MISSING = "Lastfm does not provide an API endpoint for this feature.";
    
    public bool IsPlaying { get; private set; }
    public string SongName { get; private set; } = String.Empty;
    public string[] SongArtists => songArtists;
    public SongIcon[] SongIconURLs => songIcons;
    public int? SongPosition => null;
    public int? SongLength { get; private set; }
    public string SongId { get; private set; } = String.Empty;
    public string SongUrl { get; private set; } = String.Empty;

    private LastfmClient? client;
    private string[] songArtists = Array.Empty<string>();
    private SongIcon[] songIcons = Array.Empty<SongIcon>();
    private string lastTrackName = String.Empty;
    private string lastTrackArtist = String.Empty;
    private LastTrack? lastTrackInfo;
    
    public async Task<object?> Authenticate(object[] args)
    {
        string key = (string) args[0];
        string secret = (string) args[1];
        string username = (string) args[2];
        string password = (string) args[3];
        client ??= new LastfmClient(key, secret);
        await client.Auth.GetSessionTokenAsync(username, password);
        return client.Auth.UserSession;
    }

    public MusicInitializationStatus Initialize(ref IMusicSettings musicSettings)
    {
        if (musicSettings.LastfmUser == null) return MusicInitializationStatus.NOT_AUTHORIZED;
        client ??= new LastfmClient(musicSettings.LastfmKey, musicSettings.LastfmSecret);
        bool s = client.Auth.LoadSession(musicSettings.LastfmUser);
        if (!s)
        {
            musicSettings.LastfmUser = null;
            musicSettings.SaveMusicSettings();
        }
        return !s ? MusicInitializationStatus.NOT_AUTHORIZED : MusicInitializationStatus.SUCCESS;
    }

    public void Play() => throw new ProviderNotImplementedException(FEATURE_MISSING);

    public void Pause() => throw new ProviderNotImplementedException(FEATURE_MISSING);

    public void Skip() => throw new ProviderNotImplementedException(FEATURE_MISSING);

    public void Rewind() => throw new ProviderNotImplementedException(FEATURE_MISSING);

    public async void UpdateStatus()
    {
        if (client == null) throw new MusicNotInitializedException();
        PageResponse<LastTrack> tracks = await client.User.GetRecentScrobbles(client.Auth.UserSession.Username,count:1);
        if (tracks.Content.Count <= 0)
        {
            SetEmpty();
            return;
        }
        LastTrack lastTrack = tracks.Content[0];
        if (!(lastTrack.IsNowPlaying ?? false))
        {
            SetEmpty();
            return;
        }
        IsPlaying = true;
        SongName = lastTrack.Name;
        songArtists = [lastTrack.ArtistName];
        songIcons = lastTrack.Images.Select(x => new SongIcon
        {
            Width = null,
            Height = null,
            Url = x.ToString()
        }).ToArray();
        if (lastTrackInfo == null || string.IsNullOrEmpty(lastTrackName) || string.IsNullOrEmpty(lastTrackArtist) ||
            lastTrackName != lastTrack.Name || lastTrackArtist != lastTrack.ArtistName)
        {
            LastResponse<LastTrack> lastResponseTrack =
                await client.Track.GetInfoAsync(lastTrack.Name, lastTrack.ArtistName);
            lastTrackInfo = lastResponseTrack.Content;
            lastTrackName = lastTrack.Name;
            lastTrackArtist = lastTrack.ArtistName;
        }
        SongLength = lastTrackInfo != null ? lastTrackInfo.Duration == null ? null : (int) lastTrackInfo.Duration.Value.TotalMilliseconds : null;
        SongId = lastTrack.Id;
        SongUrl = lastTrack.Url.ToString();
    }
    
    private void SetEmpty()
    {
        IsPlaying = false;
        SongName = String.Empty;
        songArtists = Array.Empty<string>();
        songIcons = Array.Empty<SongIcon>();
        SongLength = null;
        SongId = String.Empty;
        SongUrl = String.Empty;
    }
}