
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautThirdPersonCamera
{
    // 타이쿤 시점 카메라: 고정된 피치/요로 플레이어를 따라가며 확대·축소만 허용
    // 저사양 기기를 고려해 LateUpdate에서 단순 추적 + 보간만 수행 (매 프레임 GC 할당 없음)
    public class AstronautThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        public Transform lookAt;
        [SerializeField] private Vector3 targetOffset = Vector3.zero;   // 바라볼 지점 보정

        [Header("Angle (고정)")]
        [SerializeField, Range(10f, 89f)] private float pitch = 50f;    // 내려다보는 각도
        [SerializeField] private float yaw = 0f;                         // 고정 수평 방향

        [Header("Zoom")]
        public float distance = 15.0f;                                   // 시작 거리
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float wheelStep = 2f;                   // 휠 한 칸당 거리 변화
        [SerializeField] private float pinchSpeed = 0.05f;               // 핀치 픽셀당 거리 변화
        [SerializeField] private float zoomSmooth = 10f;                 // 줌 보간 속도

        [Header("Follow")]
        [SerializeField] private float followSmooth = 12f;               // 추적 보간 속도 (0이면 즉시)

        private float targetDistance;
        private float currentDistance;
        private Vector3 focusPosition;
        private float prevPinchDistance = -1f;

        // 이동 입력 기준으로 쓰는 고정 요 값
        public float Yaw => yaw;

        private void Start()
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentDistance = targetDistance;

            if (lookAt != null)
                focusPosition = lookAt.position + targetOffset;

            ApplyTransform();
        }

        private void Update()
        {
            ReadZoomInput();
        }

        private void ReadZoomInput()
        {
            // 마우스 휠: 플랫폼마다 값 크기가 달라 부호만 사용
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;

                if (scroll != 0f)
                    targetDistance -= Mathf.Sign(scroll) * wheelStep;
            }

            // 모바일 핀치: 두 손가락 간 거리 변화량
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null)
            {
                var touches = touchscreen.touches;

                if (touches.Count >= 2 &&
                    touches[0].press.isPressed &&
                    touches[1].press.isPressed)
                {
                    float pinchDistance = Vector2.Distance(
                        touches[0].position.ReadValue(),
                        touches[1].position.ReadValue()
                    );

                    // 손가락을 벌리면 확대 (거리 감소)
                    if (prevPinchDistance >= 0f)
                        targetDistance -= (pinchDistance - prevPinchDistance) * pinchSpeed;

                    prevPinchDistance = pinchDistance;
                }
                else
                {
                    prevPinchDistance = -1f;
                }
            }

            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            if (lookAt == null)
                return;

            float dt = Time.deltaTime;

            // 프레임레이트 독립 보간
            currentDistance = Mathf.Lerp(
                currentDistance,
                targetDistance,
                1f - Mathf.Exp(-zoomSmooth * dt)
            );

            Vector3 goal = lookAt.position + targetOffset;

            focusPosition = followSmooth > 0f
                ? Vector3.Lerp(focusPosition, goal, 1f - Mathf.Exp(-followSmooth * dt))
                : goal;

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            transform.SetPositionAndRotation(
                focusPosition - rotation * Vector3.forward * currentDistance,
                rotation
            );
        }

        private void OnValidate()
        {
            if (maxDistance < minDistance)
                maxDistance = minDistance;
        }
    }
}
