using UnityEngine;

public sealed class OneshotAnimatedVFX : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private int layerIndex = 0;
    [SerializeField] private bool destroyOnDisable = true;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) { Debug.LogError($"Animator Not Found On {name}"); Destroy(gameObject); }

    }

    private void Update()
    {
        if (animator == null) return;

        // Assumes: single state, non-looping, no transitions being used for this VFX
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layerIndex);

        // If it's a looping clip, this will never hit; that's fine for "oneshot" VFX.
        if (!state.loop && state.normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        if (destroyOnDisable && gameObject.scene.IsValid())
        {
            // Safety: if something disables it mid-way, just clean it up.
            Destroy(gameObject);
        }
    }
}
