using UnityEngine;
using DG.Tweening;

public class WeponTest : MonoBehaviour
{
    private Transform playerTransform;

    private float Radius = 5.0f;
    private float rotationTime = 5.0f;

    private Tween rotationTween;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            StartRotation();
        }
    }

    void StartRotation()
    {
        rotationTween = DOVirtual.Float(360f, 0f, rotationTime, (angle) =>
        {
            if (playerTransform == null) return;

            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Cos(rad) * Radius;
            float y = Mathf.Sin(rad) * Radius;

            UnityEngine.Vector3 targetPosition = playerTransform.position + new UnityEngine.Vector3(x, y, 0);

            transform.position = targetPosition;
        })
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Restart);
    }
}