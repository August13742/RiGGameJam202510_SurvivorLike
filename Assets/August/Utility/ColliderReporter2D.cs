using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ColliderReporter2D : MonoBehaviour
{
    public System.Action<Collider2D, Collider2D> TriggerEnter;   // (thisCollider, other)
    public System.Action<Collider2D, Collider2D> TriggerExit;

    private Collider2D _this;
    private void Awake() { _this = GetComponent<Collider2D>(); }

    private void OnTriggerEnter2D(Collider2D other) => TriggerEnter?.Invoke(_this, other);
    private void OnTriggerExit2D(Collider2D other) => TriggerExit?.Invoke(_this, other);
}
