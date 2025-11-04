AudioManager Scripting Guide
==============================

I. SETUP
--------------------
The AudioManager is a singleton. Ensure one instance exists in your scene.

1. Create a persistent GameObject (e.g., named "_Services").
2. Attach the AudioManager.cs script to it.
3. In the Inspector, assign your AudioMixer and the required AudioMixerGroup assets.

To access the manager from any other script:

```csharp
// Get the singleton instance
AudioManager audio = AudioManager.Instance;

II. MUSIC CONTROL

The system uses an A/B crossfading mechanism.

    Play a Music Track:

C#

public MusicResource myMusicTrack;

void StartMusic()
{
    // Play immediately with a 2.5 second crossfade
    AudioManager.Instance.PlayMusicImmediate(myMusicTrack, 2.5f);
}

    Stop Music:

C#

void StopAllMusic()
{
    // Stop with a 1.5 second fade out
    AudioManager.Instance.StopMusic(1.5f);
}

    Pause & Resume Music:

C#

void TogglePause()
{
    if (AudioManager.Instance.IsMusicPaused)
    {
        AudioManager.Instance.ResumeMusic();
    }
    else
    {
        AudioManager.Instance.PauseMusic();
    }
}

III. SFX CONTROL

Handles pooled one-shots and dedicated looping sounds.

    Play a Sound Effect:
    The PlaySFX method automatically checks the loop property on the resource.

C#

public SFXResource explosionSfx; // This resource has .loop set to false
public SFXResource engineSfx;    // This resource has .loop set to true and eventName = "PlayerEngineLoop"

void PlaySounds()
{
    // Play a one-shot. The returned ID can be tracked via the OnSFXFinished event.
    int voiceId = AudioManager.Instance.PlaySFX(explosionSfx);

    // Play a looping sound. The eventName ("PlayerEngineLoop") is its identifier.
    AudioManager.Instance.PlaySFX(engineSfx);
}

    Stop a Looping Sound Effect:

C#

void StopEngineSound()
{
    // Stop the loop using the same eventName, with a 0.5s fade out.
    AudioManager.Instance.StopLoopedSFX("PlayerEngineLoop", 0.5f);
}

    Play a Positional Sound Effect:

C#

public SFXResource impactSfx;

void OnCollisionEnter(Collision collision)
{
    // Play the SFX at the point of collision
    Vector3 impactPoint = collision.contacts[0].point;
    AudioManager.Instance.PlaySFXAtPosition(impactSfx, impactPoint);
}

    Pause & Resume All SFX:

C#

// Pause all currently playing one-shots and loops
AudioManager.Instance.PauseSFX();

// Resume them
AudioManager.Instance.ResumeSFX();

IV. PLAYLIST CONTROL

    Play a Music Playlist:

C#

public MusicPlaylist combatPlaylist;

void StartCombat()
{
    // Starts the playlist. It will follow the sequential/shuffle mode set on the asset.
    AudioManager.Instance.PlayPlaylist(combatPlaylist);
}

    Skip to the Next Track:

C#

void SkipTrack()
{
    // Will fade to the next track in the currently active playlist.
    AudioManager.Instance.NextPlaylistTrack(1.0f); // 1s crossfade
}

    Play from an SFX Playlist:
    The userKey ensures that different objects using the same playlist (e.g., two different enemies walking) maintain their own independent state.

C#

public SFXPlaylist footstepPlaylist;

// Inside a script on an enemy, for example
void PlayFootstepSound()
{
    // GetInstanceID() provides a unique integer key for this specific component instance.
    int myUniqueId = this.GetInstanceID();
    AudioManager.Instance.PlaySFXFromPlaylist(footstepPlaylist, myUniqueId);
}

V. VOLUME CONTROL

Methods accept a linear float value from 0.0f to 1.0f.
C#

void SetVolumes()
{
    // Set Master volume to 80%
    AudioManager.Instance.SetMasterVolume(0.8f);

    // Set Music volume to 50%
    AudioManager.Instance.SetMusicBusVolume(0.5f);
}

VI. EVENTS

Subscribe in OnEnable and unsubscribe in OnDisable to avoid memory leaks.
C#

void OnEnable()
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.OnSFXFinished += HandleSfxFinished;
        AudioManager.Instance.OnPlaylistTrackChanged += HandlePlaylistTrackChanged;
    }
}

void OnDisable()
{
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.OnSFXFinished -= HandleSfxFinished;
        AudioManager.Instance.OnPlaylistTrackChanged -= HandlePlaylistTrackChanged;
    }
}

// Example handler for an SFX finishing
private void HandleSfxFinished(string eventName, int voiceId)
{
    Debug.Log($"SFX with event name '{eventName}' finished playing.");
    // if voiceId is -1, it means a loop with that eventName was stopped.
}

// Example handler for a music track changing
private void HandlePlaylistTrackChanged(MusicPlaylist playlist, int newIndex, MusicResource newTrack)
{
    Debug.Log($"Playlist '{playlist.name}' changed to track {newIndex}: '{newTrack.name}'");
}