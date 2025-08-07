using Windows.Media.Control;
using MusicPresence.Objects;
using WindowsMediaController;

namespace MusicPresence.Providers;

public class WindowsMediaProvider : IMusicProvider
{
    private const string NO_MEDIA_PLAYING = "No media is currently playing or no media is focused!";

    public bool IsPlaying { get; private set; }
    public string SongName { get; private set; } = String.Empty;
    public string[] SongArtists { get; private set; } = Array.Empty<string>();
    public SongIcon[] SongIconURLs => empty;
    public int? SongPosition { get; private set; }
    public int? SongLength { get; private set; }
    public string SongId { get; private set; } = String.Empty;
    public string SongUrl { get; private set; } = String.Empty;

    private MediaManager mediaManager = new();
    private MediaManager.MediaSession? focusedMedia;
    private GlobalSystemMediaTransportControlsSessionTimelineProperties? mediaTimeline;
    private SongIcon[] empty = Array.Empty<SongIcon>();
    
    // no authentication needed
    public Task<object?> Authenticate(object[] args) => null;

    public MusicInitializationStatus Initialize(ref IMusicSettings musicSettings)
    {
        mediaManager.OnFocusedSessionChanged += session => focusedMedia = session;
        mediaManager.OnAnyTimelinePropertyChanged += (session, properties) =>
        {
            if(session != focusedMedia) return;
            mediaTimeline = properties;
        };
        mediaManager.Start();
        return MusicInitializationStatus.SUCCESS;
    }

    public async void Play()
    {
        if (focusedMedia == null) throw new NullReferenceException(NO_MEDIA_PLAYING);
        await focusedMedia.ControlSession.TryPlayAsync();
    }

    public async void Pause()
    {
        if (focusedMedia == null) throw new NullReferenceException(NO_MEDIA_PLAYING);
        await focusedMedia.ControlSession.TryPauseAsync();
    }

    public async void Skip()
    {
        if (focusedMedia == null) throw new NullReferenceException(NO_MEDIA_PLAYING);
        await focusedMedia.ControlSession.TrySkipNextAsync();
    }

    public async void Rewind()
    {
        if (focusedMedia == null) throw new NullReferenceException(NO_MEDIA_PLAYING);
        await focusedMedia.ControlSession.TrySkipPreviousAsync();
    }

    public async void UpdateStatus()
    {
        focusedMedia = mediaManager.GetFocusedSession();
        if(focusedMedia == null)
        {
            SetEmpty();
            return;
        }
        GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo =
            focusedMedia.ControlSession.GetPlaybackInfo();
        GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties =
            await focusedMedia.ControlSession.TryGetMediaPropertiesAsync();
        IsPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        SongName = mediaProperties.Title;
        SongArtists = [mediaProperties.Artist];
        SongPosition = mediaTimeline == null ? null : (int) mediaTimeline.Position.TotalMilliseconds;
        SongLength = mediaTimeline == null ? null : (int) mediaTimeline.EndTime.TotalMilliseconds;
        SongId = String.Empty;
        SongUrl = String.Empty;
    }
    
    private void SetEmpty()
    {
        IsPlaying = false;
        SongName = String.Empty;
        SongArtists = Array.Empty<string>();
        SongPosition = null;
        SongLength = null;
        SongId = String.Empty;
        SongUrl = String.Empty;
    }
}