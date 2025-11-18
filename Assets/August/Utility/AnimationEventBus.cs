using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AnimationEventBus : MonoBehaviour
{
    // Consumers subscribe to this once (e.g., in Start).
    public event Action<AnimationEvent> Fired;

    // Put this component on the same GameObject as the Animator.
    // In the AnimationEvent's Function field, type exactly "AE".
    public void AE(AnimationEvent e) => Fired?.Invoke(e);
}
