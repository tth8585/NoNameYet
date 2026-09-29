using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float smoothTime = 0.15f;
    [SerializeField] private Vector3 offset = new(0f, 1f, -10f);

    private Vector3 velocity;

    private void Awake()
    {
        if (target == null)
            target = FindFirstObjectByType<PlayerRuntime>()?.transform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var destination = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime);
    }
}