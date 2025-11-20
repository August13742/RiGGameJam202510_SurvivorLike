using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    #endregion

    #region Inspector Config
    [Header("Configuration")]
    [SerializeField] private int voicePoolSize = 96;
    [SerializeField] private AudioMixerGroup defaultSfxGroup;
    [SerializeField] private AudioMixerGroup defaultMusicGroup;

    [Header("Mixer & Volume Control")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterVolParam = "MasterVolume";
    [SerializeField] private string musicVolParam = "MusicVolume";
    [SerializeField] private string sfxVolParam = "SFXVolume";


    [Header("Debug")]
    [SerializeField, ReadOnly] private int activeVoiceCount = 0;
    #endregion

    #region Public API: Volume Control

    /// <summary>
    /// Sets the Master volume using a linear 0-1 scale.
    /// Converts automatically to Decibels for the Mixer.
    /// </summary>
    public void SetMasterVolume(float linear) => SetBusVolume(masterVolParam, linear);

    /// <summary>
    /// Sets the Music volume using a linear 0-1 scale.
    /// </summary>
    public void SetMusicVolume(float linear) => SetBusVolume(musicVolParam, linear);

    /// <summary>
    /// Sets the SFX volume using a linear 0-1 scale.
    /// </summary>
    public void SetSFXVolume(float linear) => SetBusVolume(sfxVolParam, linear);

    public void ToggleMute(bool isMuted)
    {
        if (mixer == null) return;
        float target = isMuted ? -80f : 0f;
        // Note: A better approach often involves a separate "Mute" snapshot, 
        // but this works for simple setups.
        mixer.SetFloat(masterVolParam, isMuted ? -80f : GetLastVolume(masterVolParam));
    }

    private void SetBusVolume(string paramName, float linear)
    {
        if (mixer == null) return;

        // Convert Linear (0-1) to Decibel (-80 to 0)
        // We clamp at 0.0001 to avoid Log10(0) = -Infinity
        float db = linear > 0.0001f ? 20f * Mathf.Log10(linear) : -80f;

        mixer.SetFloat(paramName, db);
    }

    // Helper to retrieve current volume if you need to restore after unmute
    private float GetLastVolume(string paramName)
    {
        if (mixer.GetFloat(paramName, out float val)) return val;
        return 0f;
    }

    #endregion

    #region Public API: Global Utilities

    /// <summary>
    /// Pauses all currently playing SFX voices. Useful when opening a Pause Menu.
    /// Does NOT pause Music.
    /// </summary>
    public void PauseAllSFX()
    {
        for (int i = 0; i < _voicePool.Length; i++)
        {
            var voice = _voicePool[i];
            if (voice.isClaimed && voice.source.isPlaying)
            {
                voice.source.Pause();
            }
        }
    }

    /// <summary>
    /// Resumes all paused SFX voices.
    /// </summary>
    public void ResumeAllSFX()
    {
        for (int i = 0; i < _voicePool.Length; i++)
        {
            var voice = _voicePool[i];
            if (voice.isClaimed)
            {
                // using UnPause() so it continues where it left off
                voice.source.UnPause();
            }
        }
    }

    /// <summary>
    /// Panic button: Immediately stops all SFX. 
    /// Useful for Scene Transitions or "Game Over" screens.
    /// </summary>
    public void StopAllSFX()
    {
        for (int i = 0; i < _voicePool.Length; i++)
        {
            var voice = _voicePool[i];
            if (voice.isClaimed)
            {
                voice.source.Stop();

                // Manually clean up state since the Coroutine might take a frame to catch up
                if (voice.routine != null) StopCoroutine(voice.routine);
                voice.isClaimed = false;
                voice.followTarget = null;
                voice.routine = null;
            }
        }
        activeVoiceCount = 0;
    }

    /// <summary>
    /// Stops all Music immediately (no fade).
    /// </summary>
    public void StopMusicImmediate()
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicSourceA.Stop();
        _musicSourceB.Stop();
    }

    #endregion

    #region Internal Structures
    private class ActiveVoice
    {
        public AudioSource source;
        public int poolIndex;    // The fixed index of this voice in the list
        public int id;           // The Generation ID (increments on use)
        public int priority;
        public Transform followTarget;
        public bool isClaimed;
        public Coroutine routine;
    }

    // Changed to Array for slightly faster index access than List
    private ActiveVoice[] _voicePool;
    private int _globalVoiceIdCounter = 0;

    // Music Internals
    private AudioSource _musicSourceA;
    private AudioSource _musicSourceB;
    private bool _isUsingMusicA = true;
    private Coroutine _musicFadeRoutine;

    // Sequence Internals
    private Dictionary<int, int> _sequenceIndices = new Dictionary<int, int>();
    // Coalescing Buffer
    // Key: The SFXResource (Asset)
    // Value: List of positions where this sound has played THIS FRAME
    private Dictionary<SFXResource, List<Vector3>> _coalesceBuffer = new Dictionary<SFXResource, List<Vector3>>();

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitialisePool();
        InitialiseMusic();
    }
    private void LateUpdate()
    {
        // RESET BUFFER EVERY FRAME
        foreach (var list in _coalesceBuffer.Values)
        {
            list.Clear();
        }
    }



    #endregion

    #region Initialisation
    private void InitialisePool()
    {
        _voicePool = new ActiveVoice[voicePoolSize]; // Array is faster for direct indexing

        GameObject poolRoot = new GameObject("SFX_Pool");
        poolRoot.transform.SetParent(transform);

        for (int i = 0; i < voicePoolSize; i++)
        {
            GameObject go = new GameObject($"Voice_{i}");
            go.transform.SetParent(poolRoot.transform);

            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;

            _voicePool[i] = new ActiveVoice
            {
                source = src,
                poolIndex = i, // Memorise its own index
                isClaimed = false,
                id = -1
            };
        }
    }

    private void InitialiseMusic()
    {
        GameObject musicRoot = new GameObject("Music_System");
        musicRoot.transform.SetParent(transform);

        _musicSourceA = musicRoot.AddComponent<AudioSource>();
        _musicSourceB = musicRoot.AddComponent<AudioSource>();

        _musicSourceA.playOnAwake = false;
        _musicSourceB.playOnAwake = false;
        _musicSourceA.loop = true;
        _musicSourceB.loop = true;

        _musicSourceA.outputAudioMixerGroup = defaultMusicGroup;
        _musicSourceB.outputAudioMixerGroup = defaultMusicGroup;
    }
    #endregion

    #region Public API: SFX

    public AudioHandle PlaySFX(SFXResource data, Vector3 position = default, Transform followTarget = null)
    {
        if (data == null) return AudioHandle.Invalid;

        bool is2D = data.bypassSpatial || (position == Vector3.zero && followTarget == null);
        Vector3 startPos = followTarget != null ? followTarget.position : position;

        // --- SPATIAL COALESCING LOGIC START
        if (!is2D && data.useSpatialCoalescing) // Only coalesce spatial sounds
        {
            if (!_coalesceBuffer.ContainsKey(data))
            {
                _coalesceBuffer[data] = new List<Vector3>();
            }

            var playedPositions = _coalesceBuffer[data];
            float sqrThreshold = data.minSpatialSeparation * data.minSpatialSeparation;

            // Check if a sound is already playing near this location
            for (int i = 0; i < playedPositions.Count; i++)
            {
                if (Vector3.SqrMagnitude(playedPositions[i] - startPos) < sqrThreshold)
                {
                    // TOO CLOSE to an existing sound of the same type.
                    // Return Invalid handle (sound absorbed).
                    return AudioHandle.Invalid;
                }
            }

            // If we get here, we are allowed to play. Register position.
            playedPositions.Add(startPos);
        }
        // --- SPATIAL COALESCING LOGIC END

        return PlayInternal(data, startPos, followTarget, is2D, 1f);
    }

    public AudioHandle PlaySequence(SFXSequence seq, Vector3 position = default, Transform followTarget = null)
    {
        if (seq == null || seq.steps.Count == 0) return AudioHandle.Invalid;

        int idHash = seq.GetHashCode();
        int indexToPlay = 0;

        // Logic resolution
        if (seq.mode == SFXSequenceMode.RandomPure)
        {
            indexToPlay = Random.Range(0, seq.steps.Count);
        }
        else if (seq.mode == SFXSequenceMode.RandomNoRepeat)
        {
            if (seq.steps.Count > 1)
            {
                int lastIndex = _sequenceIndices.ContainsKey(idHash) ? _sequenceIndices[idHash] : -1;
                do { indexToPlay = Random.Range(0, seq.steps.Count); } while (indexToPlay == lastIndex);
            }
            else indexToPlay = 0;
        }
        else // Sequential
        {
            int lastIndex = _sequenceIndices.ContainsKey(idHash) ? _sequenceIndices[idHash] : -1;
            indexToPlay = (lastIndex + 1) % seq.steps.Count;
        }

        _sequenceIndices[idHash] = indexToPlay;
        SFXResource step = seq.steps[indexToPlay];

        bool is2D = step.bypassSpatial || (position == Vector3.zero && followTarget == null);
        Vector3 startPos = followTarget != null ? followTarget.position : position;

        return PlayInternal(step, startPos, followTarget, is2D, seq.sequenceVolume);
    }

    #endregion

    #region Public API: Music

    public void PlayMusic(MusicResource music)
    {
        if (music == null || music.clip == null) return;

        AudioSource active = _isUsingMusicA ? _musicSourceA : _musicSourceB;

        // If requesting the same song that is already playing, do nothing
        if (active.isPlaying && active.clip == music.clip) return;

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(music));
    }

    public void StopMusic(float fadeOutDuration = 1.0f)
    {
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(StopMusicRoutine(fadeOutDuration));
    }

    #endregion

    #region Internal Logic (SFX)

    private AudioHandle PlayInternal(SFXResource data, Vector3 pos, Transform follow, bool force2D, float volumeMult)
    {
        if (data.clips == null || data.clips.Length == 0) return AudioHandle.Invalid;

        // 1. Get Voice
        ActiveVoice voice = GetBestVoice(data.priority);
        if (voice == null) return AudioHandle.Invalid;

        // 2. Configure
        AudioSource src = voice.source;
        AudioClip clip = data.clips[Random.Range(0, data.clips.Length)];
        src.clip = clip;
        src.outputAudioMixerGroup = data.mixerGroup != null ? data.mixerGroup : defaultSfxGroup;

        float finalVol = Mathf.Clamp01((data.volume + Random.Range(-data.volumeVariance, data.volumeVariance)) * volumeMult);
        float finalPitch = Mathf.Clamp(data.pitch + Random.Range(-data.pitchVariance, data.pitchVariance), 0.1f, 3f);

        src.volume = finalVol;
        src.pitch = finalPitch;

        if (force2D)
        {
            src.spatialBlend = 0f;
            src.transform.localPosition = Vector3.zero;
        }
        else
        {
            src.spatialBlend = data.spatialBlend;
            src.minDistance = data.minDistance;
            src.maxDistance = data.maxDistance;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.dopplerLevel = 0f;
            src.transform.position = pos;
        }

        src.Play();

        // 3. State Update
        voice.isClaimed = true;
        voice.priority = data.priority;
        voice.id = ++_globalVoiceIdCounter; // Increment Generation ID
        voice.followTarget = follow;
        activeVoiceCount++;

        if (voice.routine != null) StopCoroutine(voice.routine);
        voice.routine = StartCoroutine(VoiceLifecycle(voice, clip.length / finalPitch));

        // RETURN HANDLE WITH INDEX + GENERATION ID
        return new AudioHandle(this, voice.poolIndex, voice.id);
    }

    private ActiveVoice GetBestVoice(int priority)
    {
        ActiveVoice bestCandidate = null;
        int lowestPriorityFound = -1;

        // Iterate array (fast)
        for (int i = 0; i < _voicePool.Length; i++)
        {
            var v = _voicePool[i];
            if (!v.isClaimed) return v; // Found free

            // Check for stealing
            if (v.priority > priority)
            {
                if (v.priority > lowestPriorityFound)
                {
                    lowestPriorityFound = v.priority;
                    bestCandidate = v;
                }
            }
        }

        if (bestCandidate != null)
        {
            bestCandidate.source.Stop();
            return bestCandidate;
        }
        return null;
    }

    private IEnumerator VoiceLifecycle(ActiveVoice voice, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (!voice.isClaimed || voice.source == null || !voice.source.isPlaying) break;
            if (voice.followTarget != null) voice.source.transform.position = voice.followTarget.position;
            timer += Time.deltaTime;
            yield return null;
        }

        if (voice.source != null) voice.source.Stop();
        voice.isClaimed = false;
        voice.followTarget = null;
        voice.routine = null;
        activeVoiceCount--;
    }
    #endregion

    #region Internal Logic (Music)
    private IEnumerator CrossfadeMusicRoutine(MusicResource newMusic)
    {
        AudioSource fadingOut = _isUsingMusicA ? _musicSourceA : _musicSourceB;
        AudioSource fadingIn = _isUsingMusicA ? _musicSourceB : _musicSourceA;

        // Setup new source
        fadingIn.clip = newMusic.clip;
        fadingIn.outputAudioMixerGroup = newMusic.mixerGroup != null ? newMusic.mixerGroup : defaultMusicGroup;
        fadingIn.volume = 0f;
        fadingIn.Play();

        float timer = 0f;
        float startVol = fadingOut.volume; // Capture current volume to avoid popping if mid-fade

        while (timer < newMusic.fadeTime)
        {
            timer += Time.unscaledDeltaTime; // Music usually runs independent of time scale
            float t = timer / newMusic.fadeTime;

            fadingIn.volume = Mathf.Lerp(0f, newMusic.volume, t);
            fadingOut.volume = Mathf.Lerp(startVol, 0f, t);

            yield return null;
        }

        fadingIn.volume = newMusic.volume;
        fadingOut.volume = 0f;
        fadingOut.Stop();

        _isUsingMusicA = !_isUsingMusicA; // Swap active flag
        _musicFadeRoutine = null;
    }

    private IEnumerator StopMusicRoutine(float duration)
    {
        AudioSource active = _isUsingMusicA ? _musicSourceA : _musicSourceB;
        float startVol = active.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            active.volume = Mathf.Lerp(startVol, 0f, timer / duration);
            yield return null;
        }

        active.Stop();
        active.volume = 0f;
        _musicFadeRoutine = null;
    }

    #endregion

    #region Handle Interface

    // O(1) Lookup via Index + Gen ID
    public void StopVoice(int poolIndex, int generationId)
    {
        // Safety check bounds
        if (poolIndex < 0 || poolIndex >= _voicePool.Length) return;

        var voice = _voicePool[poolIndex];

        // Check Generation ID to ensure we aren't stopping a reused voice
        if (voice.isClaimed && voice.id == generationId)
        {
            voice.source.Stop();
            // Coroutine handles cleanup
        }
    }

    public bool IsVoicePlaying(int poolIndex, int generationId)
    {
        if (poolIndex < 0 || poolIndex >= _voicePool.Length) return false;

        var voice = _voicePool[poolIndex];
        return voice.isClaimed && voice.id == generationId && voice.source.isPlaying;
    }
    #endregion
}

// Helper attribute
public class ReadOnlyAttribute : PropertyAttribute { }
#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif

public struct AudioHandle
{
    private AudioManager _manager;
    private int _poolIndex; // Where it is
    private int _id;        // What version it is

    public static AudioHandle Invalid => new (null, -1, -1);

    public AudioHandle(AudioManager manager, int poolIndex, int id)
    {
        _manager = manager;
        _poolIndex = poolIndex;
        _id = id;
    }

    public bool IsValid => _manager != null && _id != -1;

    public void Stop()
    {
        if (IsValid) _manager.StopVoice(_poolIndex, _id);
    }

    public bool IsPlaying()
    {
        if (!IsValid) return false;
        return _manager.IsVoicePlaying(_poolIndex, _id);
    }
}