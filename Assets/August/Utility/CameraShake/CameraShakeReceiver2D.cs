using UnityEngine;

/// <summary>
/// Minimal receiver that applies a per-frame shake offset to a target transform.
/// Stores a baseLocalPos and adds the offset each frame.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraShakeReceiver2D : MonoBehaviour, IShakeReceiver
{
    [SerializeField] private Transform target;  // if null, uses this.transform
    private Vector3 _baseLocalPos;

    private void Awake()
    {
        if (target == null) target = transform;
        _baseLocalPos = target.localPosition;
    }

    public void SetShakeOffset(Vector2 offset)
    {
        // Z unchanged; add 2D offset in XY
        target.localPosition = _baseLocalPos + new Vector3(offset.x, offset.y);
    }

    public void ClearShake()
    {
        target.localPosition = _baseLocalPos;
    }
}

/// <summary>
/// Contract for anything that can receive a 2D shake offset per frame.
/// </summary>
public interface IShakeReceiver
{
    void SetShakeOffset(Vector2 offset);
    void ClearShake();
}