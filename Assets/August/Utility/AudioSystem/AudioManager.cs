using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio manager (Resource-based API).
/// Handles music crossfading, SFX pooling (one-shots and loops), and playlists.
/// </summary>
///
namespace AugustsUtility.AudioSystem
{

    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        public static AudioManager Instance
        {
            get; private set;
        }
        #endregion

        #region Events
        public event Action<string, int> OnSFXFinished;                      // eventName, voiceId
                                                                             //public static event Action<double> OnMusicStartConfirmed;            // dsp time
        public event Action<MusicPlaylist, int, MusicResource> OnPlaylistTrackChanged;
        #endregion

        #region Inspector
        [Header("Mixer & Groups")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup SFXGroup;

        [Header("Global Options")]
        [Tooltip("If false, positional APIs fold into 2D one-shots.")]
        [SerializeField] private bool enableSpatialApi = true;

        [Header("Pools & Music")]
        [SerializeField, Min(1)] private int SFXPoolSize = 16;
        [SerializeField, Min(0f)] private float defaultMusicCrossfade = 1.5f;

        [Header("Mixer Param Names (dB)")]
        [SerializeField] private string masterVolParam = "MasterVolume";
        [SerializeField] private string musicVolParam = "MusicVolume";
        [SerializeField] private string SFXVolParam = "SFXVolume";
        #endregion

        #region Internal: SFX
        private List<AudioSource> _sfxPool;
        private readonly Dictionary<string, AudioSource> _loopSfx = new();
        private readonly List<AudioSource> _pausedSfx = new();
        private int _nextVoiceId = 0;
        #endregion

        #region Internal: Music
        private AudioSource _musicA, _musicB, _activeMusic;
        private Coroutine _musicFadeCoro;
        private float _musicBaseVolume = 1f;
        private float _duckFactor = 1f;
        private Coroutine _duckCoro;

        private bool _isMusicPaused = false;
        private bool _wasScheduledToPlay = false;
        private double _musicPauseDsp;
        private double _scheduledStartDsp;

        public bool IsMusicPaused => _isMusicPaused;
        public bool IsMusicPlaying => _activeMusic != null && _activeMusic.isPlaying && !_isMusicPaused;
        public float MusicVolume => _musicBaseVolume * _duckFactor;
        #endregion

        #region Internal: Playlists
        private MusicPlaylist _currentPlaylist;
        private int _playlistIndex = -1; // FIX: State moved from ScriptableObject to manager
        private Coroutine _playlistWatcher;
        private readonly Dictionary<int, SFXPlState> _sfxPlStates = new();
        #endregion

        #region Unity
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPools();
            InitMusicPlayers();
        }
        #endregion

        #region Init
        private void InitPools()
        {
            _sfxPool = new List<AudioSource>(SFXPoolSize);
            for (int i = 0; i < SFXPoolSize; i++)
                _sfxPool.Add(CreateChildSource($"SFX Player {i}", SFXGroup));
        }

        private void InitMusicPlayers()
        {
            _musicA = CreateChildSource("Music A", musicGroup);
            _musicA.loop = true;
            _musicA.volume = 0f;
            _musicB = CreateChildSource("Music B", musicGroup);
            _musicB.loop = true;
            _musicB.volume = 0f;
            _activeMusic = _musicA;
        }

        private AudioSource CreateChildSource(string name, AudioMixerGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = group;
            return src;
        }

        private void ApplyMusicVolume()
        {
            if (_activeMusic != null)
                _activeMusic.volume = Mathf.Clamp01(_musicBaseVolume * _duckFactor);
        }
        #endregion

        #region Public: Music
        public void PlayMusicImmediate(MusicResource resource, float crossfade = -1f)
        {
            if (resource == null || resource.clip == null)
                return;

            _musicBaseVolume = Mathf.Clamp01(resource.volume);
            // FIX: Use fade-in from resource if crossfade is not specified
            float dur = crossfade >= 0 ? crossfade : resource.fadeInSeconds;

            StartImmediateCrossfade(resource.clip, dur);
            _musicA.loop = resource.loop;
            _musicB.loop = resource.loop;
        }

        public void StopMusic(float fadeOut = -1f)
        {
            float dur = fadeOut >= 0 ? fadeOut : defaultMusicCrossfade;
            if (_musicFadeCoro != null)
                StopCoroutine(_musicFadeCoro);
            _musicFadeCoro = StartCoroutine(FadeOutBothAndStop(dur));

            _isMusicPaused = false;
            _wasScheduledToPlay = false;
            if (_duckCoro != null)
                StopCoroutine(_duckCoro);
            _duckFactor = 1f;
        }

        public void PauseMusic()
        {
            if (_isMusicPaused)
                return;
            if (_musicFadeCoro != null)
            {
                StopCoroutine(_musicFadeCoro);
                _musicFadeCoro = null;
            }
            _musicPauseDsp = AudioSettings.dspTime;
            _activeMusic?.Pause();
            _isMusicPaused = true;
        }

        public void ResumeMusic()
        {
            if (!_isMusicPaused)
                return;

            double now = AudioSettings.dspTime;
            double pausedDur = now - _musicPauseDsp;
            bool started = _activeMusic != null && _activeMusic.clip != null && _activeMusic.timeSamples > 0;

            if (_wasScheduledToPlay && !started)
            {
                double minLead = 0.020;
                double newStart = Math.Max(now + minLead, _scheduledStartDsp + pausedDur);
                _activeMusic.Stop();
                _activeMusic.PlayScheduled(newStart);
                _scheduledStartDsp = newStart;
            }
            else
            {
                _wasScheduledToPlay = false;
                _activeMusic?.UnPause();
            }
            _isMusicPaused = false;
        }
        #endregion

        #region Public: Mixer & Volume Control
        public void SetMasterVolume(float linear) => SetBusLinear(masterVolParam, linear);
        public void SetSFXVolume(float linear) => SetBusLinear(SFXVolParam, linear);
        public void SetMusicBusVolume(float linear) => SetBusLinear(musicVolParam, linear);

        private void SetBusLinear(string param, float linear)
        {
            if (mixer == null || string.IsNullOrEmpty(param))
                return;
            float db = linear > 0.001f ? 20f * Mathf.Log10(Mathf.Clamp01(linear)) : -80f;
            mixer.SetFloat(param, db);
        }
        #endregion

        #region Public: SFX
        public int PlaySFX(SFXResource res, float volumeScale = 1f)
        {
            if (res == null || res.clip == null)
                return -1;

            float vol = Mathf.Clamp(res.volume * volumeScale, 0f, 2f);
            if (res.loop)
            {
                PlaySFXLoop(res.eventName, res.clip, vol, res.pitch);
                return -1; // Looping sounds don't have a voice ID from the pool
            }
            return PlaySFXOneShot(res.clip, vol, res.pitch, res.eventName, res.trackFinish);
        }

        public void StopSFX(SFXResource res, float fadeOut = 0f)
        {
            if (res != null && res.loop)
                StopLoopedSFX(res.eventName, fadeOut);
        }

        // FIX: Implemented public method to stop a named loop
        public void StopLoopedSFX(string eventName, float fadeOut = 0f)
        {
            if (string.IsNullOrEmpty(eventName) || !_loopSfx.TryGetValue(eventName, out var src))
                return;

            _loopSfx.Remove(eventName);
            if (fadeOut > 0.01f)
            {
                StartCoroutine(FadeOutAndDestroy(src, fadeOut, null));
            }
            else
            {
                Destroy(src.gameObject);
            }
            OnSFXFinished?.Invoke(eventName, -1); // -1 signifies a loop was stopped
        }

        // FIX: Implemented SFX pause
        public void PauseSFX()
        {
            _pausedSfx.Clear();
            foreach (var src in _sfxPool)
            {
                if (src.isPlaying)
                {
                    src.Pause();
                    _pausedSfx.Add(src);
                }
            }
            foreach (var src in _loopSfx.Values)
            {
                if (src.isPlaying)
                {
                    src.Pause();
                    _pausedSfx.Add(src);
                }
            }
        }

        public void ResumeSFX()
        {
            foreach (var src in _pausedSfx)
            {
                if (src != null)
                    src.UnPause();
            }
            _pausedSfx.Clear();
        }
        #endregion

        #region Public: SFX Positional
        public void PlaySFXAtPosition(SFXResource res, Vector3 position, float volumeScale = 1f)
        {
            if (res == null || res.clip == null)
                return;
            if (!enableSpatialApi)
            {
                PlaySFX(res, volumeScale);
                return;
            }
            if (res.loop)
            {
                Debug.LogWarning("Looping SFX should be attached to a specific GameObject, not played positionally this way.");
                return;
            }

            var go = new GameObject($"SFX_{res.name}");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = SFXGroup;
            src.clip = res.clip;
            src.volume = Mathf.Clamp01(res.volume * volumeScale);
            src.pitch = res.pitch;
            src.spatialBlend = 1f;
            src.Play();
            Destroy(go, res.clip.length / res.pitch + 0.1f);
        }
        #endregion

        #region Public: Music & SFX Playlists
        public void PlayPlaylist(MusicPlaylist playlist, int startIndex = -1, float crossfade = -1f)
        {
            if (playlist == null || playlist.tracks == null || playlist.tracks.Count == 0)
            {
                StopPlaylist();
                return;
            }

            _currentPlaylist = playlist;
            _playlistIndex = (startIndex >= 0 && startIndex < playlist.tracks.Count)
                ? startIndex
                : GetNextPlaylistIndex(true);

            var res = _currentPlaylist.tracks[_playlistIndex];
            PlayMusicImmediate(res, crossfade);

            OnPlaylistTrackChanged?.Invoke(_currentPlaylist, _playlistIndex, res);
            RestartPlaylistWatcher(crossfade);
        }

        public void StopPlaylist()
        {
            _currentPlaylist = null;
            _playlistIndex = -1;
            if (_playlistWatcher != null)
            {
                StopCoroutine(_playlistWatcher);
                _playlistWatcher = null;
            }
            StopMusic();
        }

        public void NextPlaylistTrack(float crossfade = -1f)
        {
            if (_currentPlaylist == null)
                return;
            _playlistIndex = GetNextPlaylistIndex(false);
            var res = _currentPlaylist.tracks[_playlistIndex];
            PlayMusicImmediate(res, crossfade);

            OnPlaylistTrackChanged?.Invoke(_currentPlaylist, _playlistIndex, res);
            RestartPlaylistWatcher(crossfade);
        }

        // FIX: Corrected SFX Playlist logic to use SFXResource and combine properties.
        public int PlaySFXFromPlaylist(SFXPlaylist pl, int userKey, Transform parent = null)
        {
            if (pl == null)
                return -1;
            if (!_sfxPlStates.TryGetValue(userKey, out var st))
                st = new SFXPlState { index = -1, lIndex = -1, rIndex = -1, nextLeft = true };

            SFXResource res = null;
            // Logic to pick the next SFXResource from the playlist
            switch (pl.mode)
            {
                case SFXPlaylistMode.Sequential:
                    if (pl.clips == null || pl.clips.Count == 0)
                        return -1;
                    int next = (st.index + 1) % pl.clips.Count;
                    if (pl.clips.Count > 1 && UnityEngine.Random.value < pl.skipChance)
                        next = (next + 1) % pl.clips.Count;
                    st.index = next;
                    res = pl.clips[st.index];
                    break;

                case SFXPlaylistMode.PairedAlternate:
                    if (st.nextLeft)
                    {
                        if (pl.leftBin == null || pl.leftBin.Count == 0)
                            return -1;
                        st.lIndex = (st.lIndex + 1) % pl.leftBin.Count;
                        res = pl.leftBin[st.lIndex];
                    }
                    else
                    {
                        if (pl.rightBin == null || pl.rightBin.Count == 0)
                            return -1;
                        st.rIndex = (st.rIndex + 1) % pl.rightBin.Count;
                        res = pl.rightBin[st.rIndex];
                    }
                    st.nextLeft = !st.nextLeft;
                    break;
            }
            _sfxPlStates[userKey] = st;

            if (res == null || res.clip == null)
                return -1;

            // Combine resource properties with playlist's global modifiers
            float vol = res.volume * pl.volume * RandScale(1f, pl.volJitter);
            float pit = res.pitch * pl.pitch * RandScale(1f, pl.pitchJitter);

            if (parent != null && enableSpatialApi)
            {
                // Simplified positional logic for brevity, you can expand this
                PlaySFXAtPosition(res, parent.position, vol / res.volume); // adjust scale
                return -1;
            }
            else
            {
                // Use the one-shot player with calculated values, bypassing event tracking for playlists
                return PlaySFXOneShot(res.clip, vol, pit, "__playlist__", trackFinish: false);
            }
        }
        #endregion

        #region Coroutines & Internals
        private AudioSource GetAvailableSFXSource()
        {
            foreach (var s in _sfxPool)
                if (!s.isPlaying)
                    return s;

            // Pool exhausted, create a temporary source.
            var extra = CreateChildSource("SFX Player (Dynamic)", SFXGroup);
            _sfxPool.Add(extra);
            return extra;
        }

        private void StartImmediateCrossfade(AudioClip newClip, float duration)
        {
            if (_musicFadeCoro != null)
                StopCoroutine(_musicFadeCoro);
            _musicFadeCoro = StartCoroutine(DoImmediateCrossfade(newClip, duration));
        }

        private IEnumerator DoImmediateCrossfade(AudioClip newClip, float duration)
        {
            var fadeIn = (_activeMusic == _musicA) ? _musicB : _musicA;
            var fadeOut = _activeMusic;

            fadeIn.Stop();
            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();

            _activeMusic = fadeIn;
            _wasScheduledToPlay = false;
            _isMusicPaused = false;

            float t = 0f;
            float outStart = fadeOut != null && fadeOut.isPlaying ? fadeOut.volume : 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                float target = Mathf.Clamp01(_musicBaseVolume * _duckFactor);
                fadeIn.volume = Mathf.Lerp(0f, target, k);
                if (fadeOut != null && fadeOut.isPlaying)
                    fadeOut.volume = Mathf.Lerp(outStart, 0f, k);
                yield return null;
            }

            ApplyMusicVolume();
            if (fadeOut != null)
            {
                fadeOut.Stop();
                fadeOut.volume = 0f;
                fadeOut.clip = null;
            }
            _musicFadeCoro = null;
        }

        private IEnumerator FadeOutBothAndStop(float duration)
        {
            float a0 = _musicA.volume, b0 = _musicB.volume, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                _musicA.volume = Mathf.Lerp(a0, 0f, k);
                _musicB.volume = Mathf.Lerp(b0, 0f, k);
                yield return null;
            }
            _musicA.Stop();
            _musicA.clip = null;
            _musicB.Stop();
            _musicB.clip = null;
            _musicFadeCoro = null;
            _activeMusic = _musicA; // Reset active to A
        }

        private int PlaySFXOneShot(AudioClip clip, float volume, float pitch, string eventName, bool trackFinish)
        {
            var src = GetAvailableSFXSource();
            src.clip = clip;
            src.volume = Mathf.Clamp(volume, 0f, 2f);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.spatialBlend = 0f;
            src.loop = false;
            src.Play();

            int voiceId = _nextVoiceId++;
            if (trackFinish && !string.IsNullOrEmpty(eventName))
                StartCoroutine(TrackSFXFinish(eventName, voiceId, clip.length / src.pitch));

            return voiceId;
        }

        private void PlaySFXLoop(string eventName, AudioClip clip, float volume, float pitch)
        {
            if (string.IsNullOrEmpty(eventName))
                return;
            StopLoopedSFX(eventName); // Stop any existing loop with the same name

            var src = CreateChildSource($"Loop_{eventName}", SFXGroup);
            src.clip = clip;
            src.volume = Mathf.Clamp(volume, 0f, 2f);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.loop = true;
            src.spatialBlend = 0f;
            src.Play();
            _loopSfx[eventName] = src;
        }

        private void RestartPlaylistWatcher(float crossfadeUsed)
        {
            if (_playlistWatcher != null)
                StopCoroutine(_playlistWatcher);
            if (_currentPlaylist != null)
                _playlistWatcher = StartCoroutine(WatchActiveTrackAndAdvance(crossfadeUsed));
        }

        private IEnumerator WatchActiveTrackAndAdvance(float crossfadeUsed)
        {
            // Wait until the active music source is valid and playing
            while (_activeMusic == null || _activeMusic.clip == null || !_activeMusic.isPlaying)
                yield return null;

            var clip = _activeMusic.clip;
            float leadTime = Mathf.Max(0.1f, crossfadeUsed);

            // Poll until the track is near its end
            while (_activeMusic != null && _activeMusic.clip == clip && _activeMusic.isPlaying)
            {
                if (!_activeMusic.loop && (_activeMusic.clip.length - _activeMusic.time) <= leadTime)
                {
                    NextPlaylistTrack(crossfadeUsed);
                    yield break;
                }
                yield return null;
            }
        }

        private int GetNextPlaylistIndex(bool isFirst)
        {
            if (_currentPlaylist == null)
                return -1;
            int n = _currentPlaylist.tracks.Count;
            if (n == 0)
                return -1;

            if (_currentPlaylist.playbackMode == PlaybackMode.SEQUENTIAL)
            {
                return (_playlistIndex + 1) % n;
            }
            else // SHUFFLE
            {
                if (n == 1)
                    return 0;
                int pick = UnityEngine.Random.Range(0, n);
                // Avoid playing the same track twice in a row
                if (pick == _playlistIndex && !isFirst)
                    pick = (pick + 1) % n;
                return pick;
            }
        }

        private IEnumerator FadeOutAndDestroy(AudioSource src, float dur, Action onFinish)
        {
            float v0 = src.volume;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(v0, 0f, t / dur);
                yield return null;
            }
            onFinish?.Invoke();
            if (src)
                Destroy(src.gameObject);
        }

        private IEnumerator TrackSFXFinish(string eventName, int voiceId, float delay)
        {
            yield return new WaitForSeconds(delay);
            OnSFXFinished?.Invoke(eventName, voiceId);
        }

        private struct SFXPlState
        {
            public int index, lIndex, rIndex; public bool nextLeft;
        }
        private static float RandScale(float baseVal, float jitter) =>
            jitter <= 0f ? baseVal : baseVal * UnityEngine.Random.Range(1f - jitter, 1f + jitter);
        #endregion
    }
}
