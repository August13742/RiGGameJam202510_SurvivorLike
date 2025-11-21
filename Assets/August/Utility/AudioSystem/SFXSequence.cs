using System.Collections.Generic;
using UnityEngine;


public enum SFXSequenceMode
{
    Sequential,         // 1 -> 2 -> 3 -> 1
    RandomNoRepeat,     // Random, but won't pick the same one twice in a row
    RandomPure          // Pure random
}

[CreateAssetMenu(fileName = "NewSFXSequence", menuName = "Audio Resource/SFX Sequence")]
public class SFXSequence : ScriptableObject
{
    public SFXSequenceMode mode = SFXSequenceMode.RandomNoRepeat;
    public List<SFXResource> steps = new List<SFXResource>();

    [Tooltip("Global volume multiplier for this sequence.")]
    [Range(0f, 1f)] public float sequenceVolume = 1f;
}
