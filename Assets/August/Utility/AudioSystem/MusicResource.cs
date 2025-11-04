using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewMusicResource", menuName = "Audio Resource/Music Resource")]
public class MusicResource : ScriptableObject
{
    public AudioClip clip;
    public float volume = 1.0f;
    public bool loop = false;
    public float fadeInSeconds = 1f;
    public float fadeOutSeconds = 1f;

}
