using UnityEngine;
using UnityEngine.InputSystem;

// Third-person orbit camera: mouse to rotate, scroll to zoom.
// Uses PhysX (Physics.Raycast), so it only sees GameObject colliders, not colliders baked in a subscene.
public class CameraFollowPlayer : MonoBehaviour
{
    [Tooltip("Leave empty to use the GameObject tagged Player.")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 6f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minPitch = -45f;
    [SerializeField] private float maxPitch = 90f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundBuffer = 1f;
    [SerializeField] private float pivotHeight = 1.5f;

    private float yaw = 0f;
    private float pitch = 45f;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraFollowPlayer: no target set and no GameObject tagged Player.", this);
            }
        }
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            // Old Input Manager scaled raw mouse delta by 0.1, so keep that to preserve sensitivity
            Vector2 mouseDelta = mouse.delta.ReadValue() * 0.1f;
            yaw += mouseDelta.x * sensitivity;
            pitch -= mouseDelta.y * sensitivity;

            // Scroll values differ per platform (e.g. 120 per notch on Windows), so use one step per notch
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                distance -= Mathf.Sign(scroll) * 0.1f * zoomSpeed;
            }
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * new Vector3(0, 0, -1f);

        Vector3 pivot = target.position + Vector3.up * pivotHeight;

        // Cast from the pivot toward the desired camera spot. If the ground
        // is in the way before we reach the full zoom distance, pull the
        // camera in to the hit point instead - this is what makes tilting
        // up near the ground also zoom the camera closer to the player.
        float effectiveDistance = distance;
        if (Physics.Raycast(pivot, direction, out RaycastHit hit, distance, groundLayer))
        {
            effectiveDistance = Mathf.Max(hit.distance - groundBuffer, 0f);
        }

        Vector3 position = pivot + direction * effectiveDistance;

        transform.SetPositionAndRotation(position, rotation);
    }
}
