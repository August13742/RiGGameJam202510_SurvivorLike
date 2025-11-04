using UnityEngine;
using DG.Tweening;
using Survivor.Game;

public class WeaponRotation : MonoBehaviour
{
    private Transform playerTransform;

    private float radius = 3.0f;
    private float rotationTime = 1.0f;
    private float cooldownTime = 3.0f;

    private Tween rotationTween;

    private Collider2D RotationWeaponCollider;
    private Renderer RotationWeaponRenderer;

    void Start()
    {
        RotationWeaponCollider = GetComponent<Collider2D>();
        RotationWeaponRenderer = GetComponent<Renderer>();

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            StartRotation();
        }
    }

    void StartRotation()
    {
        if (RotationWeaponCollider != null)
        {
            RotationWeaponCollider.enabled = true;
        }
        if (RotationWeaponRenderer != null)
        {
            RotationWeaponRenderer.enabled = true;
        }

        rotationTween = DOVirtual.Float(360f, 0f, rotationTime, (angle) =>
        {
            if (playerTransform == null) return;

            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * radius;
            float y = Mathf.Sin(rad) * radius;

            Vector3 targetPosition = playerTransform.position + new Vector3(x, y, 0);

            transform.position = targetPosition;
        })
        .SetEase(Ease.InOutCirc)
        .SetLoops(3, LoopType.Restart)
        .OnComplete(RotationStop);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.TryGetComponent<HealthComponent>(out var target)) return;

        target.Damage(5);
    }

    private void RotationStop()
    {
        if (RotationWeaponCollider != null)
        {
            RotationWeaponCollider.enabled = false;
        }
        if (RotationWeaponRenderer != null)
        {
            RotationWeaponRenderer.enabled = false;
        }

        rotationTween = DOVirtual.DelayedCall(cooldownTime, StartRotation);
    }
    
}