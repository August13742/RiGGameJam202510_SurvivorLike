using System.Collections.Generic;
using UnityEngine;

public enum PlaybackMode
{
    SEQUENTIAL,
    SHUFFLE
}

[CreateAssetMenu(menuName = "Audio Resource/Music Playlist")]
public class MusicPlaylist : ScriptableObject
{
    public List<MusicResource> tracks = new();
    public PlaybackMode playbackMode = PlaybackMode.SHUFFLE;
}
