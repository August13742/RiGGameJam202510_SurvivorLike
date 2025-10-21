using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public GameObject FollowTarget;
    [SerializeField] private float acceleration = 4;

    private void Awake()
    {
        if (FollowTarget == null)
        {
            FollowTarget = GameObject.FindGameObjectWithTag("Player");
        }
    }

    void LateUpdate()
    {
        if (FollowTarget == null) return;
        Vector3 targetPosition = new Vector3(FollowTarget.transform.position.x, FollowTarget.transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, acceleration * Time.deltaTime);
    }
}
