using System.Collections.Generic;
using UnityEngine;

public enum SFXPlaylistMode
{
    Sequential,
    PairedAlternate
}

[CreateAssetMenu(menuName = "Audio/SFX Playlist")]
public class SFXPlaylist : ScriptableObject
{
    public SFXPlaylistMode mode = SFXPlaylistMode.Sequential;

    public List<SFXResource> clips = new();      // used in Sequential
    public List<SFXResource> leftBin = new();    // used in PairedAlternate
    public List<SFXResource> rightBin = new();

    [Header("Global Modifiers")]
    [Range(0f, 2f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float volJitter = 0.05f;
    [Range(0f, 0.5f)] public float pitchJitter = 0.02f;

    [Tooltip("Chance to skip to next index when in Sequential (adds subtle irregularity).")]
    [Range(0f, 0.4f)] public float skipChance = 0.1f;
}
