using MusicPresence.Errors;
using MusicPresence.Objects;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace MusicPresence.Providers;

public class SpotifyProvider : IMusicProvider
{
    private const string NO_PREMIUM = "You must have Spotify Premium to Play Songs.";
    
    public bool IsPlaying { get; private set; }
    public string SongName { get; private set; } = String.Empty;
    public string[] SongArtists { get; private set; } = Array.Empty<string>();
    public SongIcon[] SongIconURLs { get; private set; } = Array.Empty<SongIcon>();
    public int? SongPosition { get; private set; }
    public int? SongLength { get; private set; }
    public string SongId { get; private set; } = String.Empty;
    public string SongUrl { get; private set; } = String.Empty;

    /// <summary>
    /// Checks if the authenticated user has Spotify Premium
    /// </summary>
    public bool HasPremium => user?.Product.Contains("premium") ?? false;
    /// <summary>
    /// Plays a song by Adding the song to the current queue, then skipping to the song as opposed to playing the song directly.
    /// </summary>
    public bool UseQueueSkipForPlaying { get; set; }

    public Action<PKCETokenResponse> OnTokenResponse = token => { };

    private SpotifyClientConfig spotifyClientConfig = SpotifyClientConfig.CreateDefault();
    private SpotifyClient? client;
    private TaskCompletionSource<PKCETokenResponse>? authCompletion;
    private EmbedIOAuthServer? server;
    private PrivateUser? user;

    public async Task<object?> Authenticate(object[] args)
    {
        string clientId = (string) args[0];
        Uri uri = new Uri($"http://127.0.0.1:{MusicObject.AUTHORIZATION_PORT}/callback");
        server = new EmbedIOAuthServer(uri, MusicObject.AUTHORIZATION_PORT);
        authCompletion = new TaskCompletionSource<PKCETokenResponse>();
        var (verifier, challenge) = PKCEUtil.GenerateCodes();
        await server.Start();
        server.AuthorizationCodeReceived += async (_, response) =>
        {
            await server.Stop();
            PKCETokenResponse token = await new OAuthClient().RequestToken(new PKCETokenRequest(clientId, response.Code,
                server.BaseUri, verifier));
            authCompletion.SetResult(token);
        };
        LoginRequest loginRequest = new LoginRequest(server.BaseUri, clientId, LoginRequest.ResponseType.Code)
        {
            CodeChallenge = challenge,
            CodeChallengeMethod = "S256",
            Scope = new List<string> {Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState, Scopes.UserReadPrivate}
        };
        MusicObject.OpenInBrowser.Invoke(loginRequest.ToUri());
        return await authCompletion.Task;
    }

    public MusicInitializationStatus Initialize(ref IMusicSettings musicSettings)
    {
        if (musicSettings.SpotifyAccessToken == null) return MusicInitializationStatus.NOT_AUTHORIZED;
        PKCEAuthenticator authenticator =
            new PKCEAuthenticator(musicSettings.SpotifyClientId, musicSettings.SpotifyAccessToken);
        authenticator.TokenRefreshed += (_, response) => OnTokenResponse.Invoke(response);
        spotifyClientConfig = spotifyClientConfig.WithAuthenticator(authenticator);
        client = new SpotifyClient(spotifyClientConfig);
        try
        {
            // do something to get it to error
            PlayerCurrentlyPlayingRequest request =
                new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track);
            CurrentlyPlaying _ = client.Player.GetCurrentlyPlaying(request).Result;
        }
        catch (APIUnauthorizedException)
        {
            musicSettings.SpotifyAccessToken = null;
            musicSettings.SaveMusicSettings();
            return MusicInitializationStatus.NOT_AUTHORIZED;
        }
        catch (AggregateException e)
        {
            if (e.Message.Contains("invalid_grant"))
                return RefreshToken(ref musicSettings);
            throw;
        }
        catch (APIException e)
        {
            if (e.Message.Contains("invalid_grant"))
                return RefreshToken(ref musicSettings);
            throw;
        }
        user = client.UserProfile.Current().Result;
        return MusicInitializationStatus.SUCCESS;
    }

    public void Play()
    {
        if (client == null) throw new MusicNotInitializedException();
        if (!HasPremium) throw new ProviderNotImplementedException(NO_PREMIUM);
        client.Player.ResumePlayback();
    }

    public void Pause()
    {
        if (client == null) throw new MusicNotInitializedException();
        if (!HasPremium) throw new ProviderNotImplementedException(NO_PREMIUM);
        client.Player.PausePlayback();
    }

    public void Skip()
    {
        if (client == null) throw new MusicNotInitializedException();
        if (!HasPremium) throw new ProviderNotImplementedException(NO_PREMIUM);
        client.Player.SkipNext();
    }

    public void Rewind()
    {
        if (client == null) throw new MusicNotInitializedException();
        if (!HasPremium) throw new ProviderNotImplementedException(NO_PREMIUM);
        client.Player.SkipPrevious();
    }

    public async void UpdateStatus()
    {
        if (client == null) throw new MusicNotInitializedException();
        PlayerCurrentlyPlayingRequest request =
            new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track);
        CurrentlyPlaying currentlyPlaying = await client.Player.GetCurrentlyPlaying(request);
        // This can be null, it's just not annotated
        if(currentlyPlaying == null)
        {
            SetEmpty();
            return;
        }
        IsPlaying = currentlyPlaying.IsPlaying;
        if (!IsPlaying)
        {
            SetEmpty();
            return;
        }
        FullTrack episode = (FullTrack) currentlyPlaying.Item;
        SongName = episode.Name;
        SongArtists = episode.Artists.Select(x => x.Name).ToArray();
        SongIconURLs = episode.Album.Images.Select(img => new SongIcon
        {
            Width = img.Width,
            Height = img.Height,
            Url = img.Url
        }).ToArray();
        SongPosition = currentlyPlaying.ProgressMs ?? null;
        SongLength = episode.DurationMs;
        SongId = episode.Id;
        SongUrl = episode.Uri;
    }

    /// <summary>
    /// Plays a song from the song's uri
    /// </summary>
    /// <param name="uri">The song uri to play</param>
    /// <exception cref="MusicNotInitializedException">The provider has not been initialized</exception>
    /// <exception cref="ProviderNotImplementedException">The authenticated user does not have Spotify Premium</exception>
    public async void PlaySong(string uri)
    {
        if (client == null || user == null) throw new MusicNotInitializedException();
        if (!HasPremium) throw new ProviderNotImplementedException(NO_PREMIUM);
        if(UseQueueSkipForPlaying)
        {
            PlayerAddToQueueRequest playerAddToQueueRequest = new PlayerAddToQueueRequest(uri);
            bool s = await client.Player.AddToQueue(playerAddToQueueRequest);
            if (!s) return;
            await client.Player.SkipNext();
            return;
        }
        PlayerResumePlaybackRequest playbackRequest = new PlayerResumePlaybackRequest
        {
            Uris = new List<string> {uri}
        };
        await client.Player.ResumePlayback(playbackRequest);
    }
    
    private void SetEmpty()
    {
        SongName = String.Empty;
        SongArtists = Array.Empty<string>();
        SongIconURLs = Array.Empty<SongIcon>();
        SongPosition = null;
        SongLength = null;
        SongId = String.Empty;
        SongUrl = String.Empty;
    }

    private MusicInitializationStatus RefreshToken(ref IMusicSettings musicSettings)
    {
        if (musicSettings.SpotifyAccessToken == null) return MusicInitializationStatus.NOT_AUTHORIZED;
        PKCETokenResponse newResponse = new OAuthClient().RequestToken(
            new PKCETokenRefreshRequest(musicSettings.SpotifyClientId, musicSettings.SpotifyAccessToken.RefreshToken)
        ).Result;
        musicSettings.SpotifyAccessToken = newResponse;
        musicSettings.SaveMusicSettings();
        client = new SpotifyClient(newResponse.AccessToken);
        return MusicInitializationStatus.SUCCESS;
    }
}