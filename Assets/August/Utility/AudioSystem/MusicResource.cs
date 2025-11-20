using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
[CreateAssetMenu(fileName = "NewMusicResource", menuName = "Audio Resource/Music Resource")]
public class MusicResource : ScriptableObject
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;
    public float fadeTime = 1.5f;
    public AudioMixerGroup mixerGroup;
}