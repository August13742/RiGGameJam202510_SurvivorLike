using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewSFXResource", menuName = "Audio Resource/SFX Resource")]
public class SFXResource : ScriptableObject
{
    public string eventName;
    public AudioClip clip;
    public bool loop = false;
    public bool trackFinish = false;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}
