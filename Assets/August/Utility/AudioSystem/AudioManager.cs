using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
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
    [SerializeField, ReadOnly] private string currentMusicName = "None";
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
            if (voice.isClaimed && voice.source.isPlaying) voice.source.Pause();
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
        StopPlaylist(); // Stop playlist logic
        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicSourceA.Stop();
        _musicSourceB.Stop();
        currentMusicName = "None";
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

    // Playlist Internals
    private MusicPlaylist _activePlaylist;
    private List<int> _playlistQueue = new List<int>(); // Shuffle bag
    private Coroutine _playlistRoutine;

    // Sequence Internals
    private Dictionary<int, int> _sequenceIndices = new Dictionary<int, int>();
    private Dictionary<SFXResource, List<Vector3>> _coalesceBuffer = new ();

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
        _voicePool = new ActiveVoice[voicePoolSize];
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
                poolIndex = i,
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
        // Loop defaults to true, but PlayInternal controls it now
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

        if (!is2D && data.useSpatialCoalescing)
        {
            if (!_coalesceBuffer.ContainsKey(data)) _coalesceBuffer[data] = new List<Vector3>();
            var playedPositions = _coalesceBuffer[data];
            float sqrThreshold = data.minSpatialSeparation * data.minSpatialSeparation;

            for (int i = 0; i < playedPositions.Count; i++)
            {
                if (Vector3.SqrMagnitude(playedPositions[i] - startPos) < sqrThreshold) return AudioHandle.Invalid;
            }
            playedPositions.Add(startPos);
        }

        return PlayInternal(data, startPos, followTarget, is2D, 1f);
    }

    public AudioHandle PlaySequence(SFXSequence seq, Vector3 position = default, Transform followTarget = null)
    {
        if (seq == null || seq.steps.Count == 0) return AudioHandle.Invalid;

        int idHash = seq.GetHashCode();
        int indexToPlay = 0;

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
        else
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

    /// <summary>
    /// Plays a single track, looping indefinitely. 
    /// Stops any active Playlist.
    /// </summary>
    public void PlayMusic(MusicResource music)
    {
        // If user manually calls PlayMusic, we assume they want to override the playlist.
        StopPlaylist();
        PlayMusicInternal(music, true);
    }

    /// <summary>
    /// Starts a Playlist with the defined Playback Mode.
    /// </summary>
    public void PlayPlaylist(MusicPlaylist playlist)
    {
        if (playlist == null || playlist.tracks.Count == 0) return;

        // If already playing this playlist, do nothing (optional, maybe restart?)
        if (_activePlaylist == playlist && _playlistRoutine != null) return;

        StopPlaylist(); // Clean up old routines
        _activePlaylist = playlist;

        // Initialise Queue
        RefillPlaylistQueue();

        _playlistRoutine = StartCoroutine(PlaylistLifecycle());
    }

    public void StopPlaylist()
    {
        if (_playlistRoutine != null)
        {
            StopCoroutine(_playlistRoutine);
            _playlistRoutine = null;
        }
        _activePlaylist = null;
        _playlistQueue.Clear();
    }

    public void StopMusic(float fadeOutDuration = 1.0f)
    {
        StopPlaylist(); // Ensure playlist doesn't trigger next song
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
        src.loop = data.loop;
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

        // duration: finite for one-shots, infinite for looped SFX
        float voiceDuration = data.loop ? Mathf.Infinity : (clip.length / finalPitch);

        if (voice.routine != null) StopCoroutine(voice.routine);
        voice.routine = StartCoroutine(VoiceLifecycle(voice, voiceDuration));

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
        bool infinite = float.IsInfinity(duration);

        while (true)
        {
            if (!voice.isClaimed || voice.source == null) break;
            if (!voice.source.isPlaying) break;

            if (voice.followTarget != null)
            {
                voice.source.transform.position = voice.followTarget.position;
            }

            if (!infinite)
            {
                timer += Time.deltaTime;
                if (timer >= duration) break;
            }

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
    private void PlayMusicInternal(MusicResource music, bool loop)
    {
        if (music == null || music.clip == null) return;

        AudioSource active = _isUsingMusicA ? _musicSourceA : _musicSourceB;

        // If requesting the same song that is already playing, just ensure loop status is correct and return
        if (active.isPlaying && active.clip == music.clip)
        {
            active.loop = loop;
            return;
        }

        currentMusicName = music.clip.name;

        if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
        _musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(music, loop));
    }

    private IEnumerator PlaylistLifecycle()
    {
        while (_activePlaylist != null)
        {
            // 1. Get Next Track
            if (_playlistQueue.Count == 0) RefillPlaylistQueue();

            int trackIndex = _playlistQueue[0];
            _playlistQueue.RemoveAt(0);

            MusicResource nextTrack = _activePlaylist.tracks[trackIndex];

            // 2. Play it (Non-looping so it finishes)
            PlayMusicInternal(nextTrack, loop: false);

            // 3. Wait for duration
            // We subtract the fadeTime so the NEXT song starts fading in 
            // while this one is ending, creating a gapless crossfade.
            if (nextTrack.clip != null)
            {
                float waitDuration = nextTrack.clip.length - nextTrack.fadeTime;
                // Ensure we don't wait negative time
                waitDuration = Mathf.Max(0.1f, waitDuration);

                yield return new WaitForSecondsRealtime(waitDuration);
            }
            else
            {
                // Fallback if data is bad
                yield return null;
            }
        }
    }

    private void RefillPlaylistQueue()
    {
        _playlistQueue.Clear();
        for (int i = 0; i < _activePlaylist.tracks.Count; i++) _playlistQueue.Add(i);

        if (_activePlaylist.playbackMode == PlaybackMode.SHUFFLE)
        {
            // Fisher-Yates Shuffle
            int n = _playlistQueue.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                int value = _playlistQueue[k];
                _playlistQueue[k] = _playlistQueue[n];
                _playlistQueue[n] = value;
            }
        }
    }

    private IEnumerator CrossfadeMusicRoutine(MusicResource newMusic, bool loop)
    {
        AudioSource fadingOut = _isUsingMusicA ? _musicSourceA : _musicSourceB;
        AudioSource fadingIn = _isUsingMusicA ? _musicSourceB : _musicSourceA;

        // Setup new source
        fadingIn.clip = newMusic.clip;
        fadingIn.outputAudioMixerGroup = newMusic.mixerGroup != null ? newMusic.mixerGroup : defaultMusicGroup;
        fadingIn.loop = loop; // Apply loop setting here
        fadingIn.volume = 0f;
        fadingIn.Play();

        float timer = 0f;
        float startVol = fadingOut.volume;

        while (timer < newMusic.fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / newMusic.fadeTime;

            fadingIn.volume = Mathf.Lerp(0f, newMusic.volume, t);
            fadingOut.volume = Mathf.Lerp(startVol, 0f, t);

            yield return null;
        }

        fadingIn.volume = newMusic.volume;
        fadingOut.volume = 0f;
        fadingOut.Stop();

        _isUsingMusicA = !_isUsingMusicA;
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
        currentMusicName = "None";
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
    public void SetVoiceVolume(int poolIndex, int generationId, float volumeLinear)
    {
        if (poolIndex < 0 || poolIndex >= _voicePool.Length) return;

        var voice = _voicePool[poolIndex];
        if (!voice.isClaimed || voice.id != generationId) return;
        if (voice.source == null) return;

        voice.source.volume = Mathf.Clamp01(volumeLinear);
    }

    public void SetVoicePitch(int poolIndex, int generationId, float pitch)
    {
        if (poolIndex < 0 || poolIndex >= _voicePool.Length) return;

        var voice = _voicePool[poolIndex];
        if (!voice.isClaimed || voice.id != generationId) return;
        if (voice.source == null) return;

        // keep things sane
        voice.source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
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

    public static AudioHandle Invalid => new(null, -1, -1);

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

    public void SetVolume(float linear)
    {
        if (!IsValid) return;
        _manager.SetVoiceVolume(_poolIndex, _id, Mathf.Clamp01(linear));
    }

    public void SetPitch(float pitch)
    {
        if (!IsValid) return;
        _manager.SetVoicePitch(_poolIndex, _id, pitch);
    }
}