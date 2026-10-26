
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TycoonCameraZoom : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Zoom Settings")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float zoomSpeed = 0.01f;

    [Header("Camera Angle")]
    [SerializeField] private float angle = 55f;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        if (Touch.activeTouches.Count != 2)
            return;

        Touch first = Touch.activeTouches[0];
        Touch second = Touch.activeTouches[1];

        Vector2 firstPos = first.screenPosition;
        Vector2 secondPos = second.screenPosition;

        Vector2 firstPrev = firstPos - first.delta;
        Vector2 secondPrev = secondPos - second.delta;

        float previousDistance =
            Vector2.Distance(firstPrev, secondPrev);

        float currentDistance =
            Vector2.Distance(firstPos, secondPos);

        float pinchDelta = currentDistance - previousDistance;

        distance -= pinchDelta * zoomSpeed;

        distance = Mathf.Clamp(
            distance, minDistance, maxDistance);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(angle, 0f, 0f);

        Vector3 offset =
            rotation * Vector3.back * distance;

        transform.position = target.position + offset;
        transform.rotation = rotation;
    }
}
