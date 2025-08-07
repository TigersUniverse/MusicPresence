namespace MusicPresence.Test;

public enum ProviderIdentifier
{
    Spotify,
    lastfm,
    Media
}

public static class ProviderExtensions
{
    public static ProviderIdentifier ToProvider(this string providerString)
    {
        switch (providerString.ToLower())
        {
            case "spotify":
                return ProviderIdentifier.Spotify;
            case "lastfm":
                return ProviderIdentifier.lastfm;
        }
        return ProviderIdentifier.Media;
    }
}