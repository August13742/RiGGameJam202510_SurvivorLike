using UnityEngine;
using UnityEngine.Audio;

// The Basic Atom: Handles Variations & Randomization
[CreateAssetMenu(fileName = "NewSFX", menuName = "Audio Resource/SFX Resource")]
public class SFXResource : ScriptableObject
{
    [Header("Content")]
    [Tooltip("The system will pick one random clip from this array each time.")]
    public AudioClip[] clips;

    [Header("Mixing & Priority")]
    

    [Tooltip("0 = Highest Priority (Critical), 256 = Lowest (Background). Used for voice stealing.")]
    [Range(0, 256)] public int priority = 128;

    [Header("Volume & Pitch")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 0.5f)] public float volumeVariance = 0.1f;

    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchVariance = 0.1f;

    [Header("Spatial Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 1 = 3D, 0 = 2D
    public float minDistance = 1f;
    public float maxDistance = 25f;
    [Tooltip("If true, this sound ignores the Manager's spatial API and always plays 2D.")]
    public bool bypassSpatial = false;

    public bool useSpatialCoalescing = true;
    [Tooltip("If another sound is within x unit this frame, skip it")]
    public float minSpatialSeparation = 5.0f; 

    [Header("Optional Override")]
    [Tooltip("Optional: Override the default SFX mixer group.")]
    public AudioMixerGroup mixerGroup;
}


