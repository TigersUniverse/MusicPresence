# MusicPresence

A C# wrapper that provides simple media information for Spotify, lastfm, and more.

## Getting Started

To get started, you'll need to know which provider you want to pull from. You can do this however you need to, but this will likely be user input. You can see an example console application at [Program.cs](https://github.com/TigersUniverse/MusicPresence/blob/main/MusicPresence.Test/Program.cs#L12).

Once you have the provider, you need to instantiate the provider. For this example, we'll assume the WindowsMediaProvider.

```cs
IMusicProvider provider = new WindowsMediaProvider();
```

Then, create the MusicObject, using the provider as the constructor.

```cs
IMusicProvider provider = new WindowsMediaProvider();
MusicObject music = new MusicObject(provider);
```

Once you have the MusicObject, and you've properly initialized your provider, you can then initialize the MusicObject and check the initialization status.

```cs
IMusicProvider provider = new WindowsMediaProvider();
MusicObject music = new MusicObject(provider);
MusicInitializationStatus status = music.Initialize();

switch(status)
{
    case MusicInitializationStatus.NOT_AUTHORIZED:
        throw new Exception("Failed to authenticate!")
    default:
        // success!
        break;
}
```

## Updating Status

Status of the Provider must be updated manually, this can be done as often as you need, but should **NOT** happen every frame. For this example, we use a separate Task with a pause of 1 second.

```cs
CancellationTokenSource cts = new();
Task.Factory.StartNew(async () => {
    while(!cts.IsCancellationRequested)
    {
        // Update your provider
        music.Provider.UpdateStatus();
        // Now, access the provider's data here
        if(music.Provider.IsPlaying)
        {
            string songName = music.Provider.SongName;
            string[] songArtists = music.Provider.SongArtists;
        }
        // Wait for 1 second before updating again
        Thread.Sleep(1000);
    }
})
// To stop the loop, simply do cts.Cancel()
```

## Usage and Contributing

For more information on using and contributing to MusicPresence, see the [Wiki](https://github.com/TigersUniverse/MusicPresence/wiki).