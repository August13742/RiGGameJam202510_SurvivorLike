using UnityEngine;

[DisallowMultipleComponent]
public sealed class ConstantSpinningElement : MonoBehaviour
{
    public float SpinSpeed = 10f;

    private void Update()
    {
        transform.Rotate(0f, 0f, SpinSpeed * Time.deltaTime, Space.Self);
    }
}
