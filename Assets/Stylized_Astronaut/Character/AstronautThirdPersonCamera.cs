
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautThirdPersonCamera
{
    public class AstronautThirdPersonCamera : MonoBehaviour
    {
        private const float Y_ANGLE_MIN = 0.0f;
        private const float Y_ANGLE_MAX = 50.0f;

        [Header("Target")]
        public Transform lookAt;
        public Transform camTransform;

        [Header("Camera")]
        public float distance = 5.0f;

        [Header("Mouse")]
        public float sensitivityX = 0.1f;
        public float sensitivityY = 0.1f;

        private float currentX = 0.0f;
        private float currentY = 45.0f;

        private void Start()
        {
            camTransform = transform;
        }

        private void Update()
        {
            RotateCamera();
        }

        private void RotateCamera()
        {
            if (Mouse.current == null)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            currentX += mouseDelta.x * sensitivityX;
            currentY -= mouseDelta.y * sensitivityY;

            currentY = Mathf.Clamp(
                currentY,
                Y_ANGLE_MIN,
                Y_ANGLE_MAX
            );
        }

        private void LateUpdate()
        {
            if (lookAt == null)
                return;

            Vector3 dir = new Vector3(0f, 0f, -distance);

            Quaternion rotation = Quaternion.Euler(
                currentY,
                currentX,
                0f
            );

            camTransform.position =
                lookAt.position + rotation * dir;

            camTransform.LookAt(lookAt.position);
        }
    }
}
