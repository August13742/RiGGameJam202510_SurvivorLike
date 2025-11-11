using UnityEngine;

using AugustsUtility.AudioSystem;

public class BGMPlayer : MonoBehaviour
{
    [SerializeField]
    private MusicResource musicToPlay;

    void Start()
    {
        if (musicToPlay != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusicImmediate(musicToPlay, -1f);
        }
    }
}