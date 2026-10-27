
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace AstronautThirdPersonCamera
{
    // 타이쿤 시점 카메라: 플레이어를 따라가며 확대·축소 / 방향 전환 / 틸트 지원
    // 모바일: 두 손가락 핀치 = 줌, 비틀기 = 방향 전환, 같이 위아래로 밀기 = 틸트
    //         (제스처 시작 시 움직임이 가장 큰 하나로 고정해서 섞이지 않게 함)
    // 에디터/PC: 마우스 휠 = 줌, 우클릭 드래그 = 방향 전환(좌우) + 틸트(상하)
    // 저사양 기기를 고려해 LateUpdate에서 단순 추적 + 보간만 수행 (매 프레임 GC 할당 없음)
    public class AstronautThirdPersonCamera : MonoBehaviour
    {
        private enum Gesture { None, Pending, Zoom, Rotate, Tilt }

        [Header("Target")]
        public Transform lookAt;
        [SerializeField] private Vector3 targetOffset = Vector3.zero;   // 바라볼 지점 보정

        [Header("Angle (시작 값)")]
        [SerializeField, Range(10f, 89f)] private float pitch = 50f;    // 내려다보는 각도
        [SerializeField] private float yaw = 0f;                         // 수평 방향

        [Header("Zoom")]
        public float distance = 15.0f;                                   // 시작 거리
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float wheelStep = 2f;                   // 휠 한 칸당 거리 변화
        [SerializeField] private float pinchSpeed = 0.05f;               // 핀치 픽셀당 거리 변화
        [SerializeField] private float zoomSmooth = 10f;                 // 줌 보간 속도

        [Header("Rotate (방향 전환)")]
        [SerializeField] private float twistSpeed = 1f;                  // 비튼 각도 1도당 회전 각도
        [SerializeField] private float mouseRotateSpeed = 0.2f;          // 우클릭 드래그 픽셀당 회전 각도

        [Header("Tilt")]
        [SerializeField, Range(10f, 89f)] private float minPitch = 25f;
        [SerializeField, Range(10f, 89f)] private float maxPitch = 80f;
        [SerializeField] private float tiltSpeed = 0.15f;                // 드래그 픽셀당 각도 변화
        [SerializeField] private float mouseTiltSpeed = 0.15f;

        [Header("Gesture")]
        [SerializeField] private float gestureThreshold = 20f;           // 제스처 판별에 필요한 움직임 (픽셀)
        [SerializeField] private float angleSmooth = 12f;                // 회전/틸트 보간 속도

        [Header("Follow")]
        [SerializeField] private float followSmooth = 12f;               // 추적 보간 속도 (0이면 즉시)

        private float targetDistance;
        private float currentDistance;
        private float targetYaw;
        private float currentYaw;
        private float targetPitch;
        private float currentPitch;
        private Vector3 focusPosition;

        private Gesture gesture;
        private float pendingZoom;
        private float pendingRotate;
        private float pendingTilt;

        // 이동 입력 기준으로 쓰는 현재 요 값
        public float Yaw => currentYaw;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Start()
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            currentDistance = targetDistance;

            targetYaw = currentYaw = yaw;
            targetPitch = currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            if (lookAt != null)
                focusPosition = lookAt.position + targetOffset;

            ApplyTransform();
        }

        private void Update()
        {
            ReadMouseInput();
            ReadTouchInput();

            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        private void ReadMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            // 마우스 휠: 플랫폼마다 값 크기가 달라 부호만 사용
            float scroll = mouse.scroll.ReadValue().y;

            if (scroll != 0f)
                targetDistance -= Mathf.Sign(scroll) * wheelStep;

            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                targetYaw += delta.x * mouseRotateSpeed;
                targetPitch -= delta.y * mouseTiltSpeed;
            }
        }

        private void ReadTouchInput()
        {
            var touches = Touch.activeTouches;

            if (touches.Count != 2)
            {
                gesture = Gesture.None;
                return;
            }

            Touch first = touches[0];
            Touch second = touches[1];

            Vector2 firstPos = first.screenPosition;
            Vector2 secondPos = second.screenPosition;
            Vector2 firstPrev = firstPos - first.delta;
            Vector2 secondPrev = secondPos - second.delta;

            // 손가락을 벌리면 +
            float pinch =
                Vector2.Distance(firstPos, secondPos) -
                Vector2.Distance(firstPrev, secondPrev);

            // 반시계 방향으로 비틀면 +
            float twist = Vector2.SignedAngle(secondPrev - firstPrev, secondPos - firstPos);

            // 두 손가락이 같은 방향으로 위아래 이동할 때만 틸트
            float tilt = first.delta.y * second.delta.y > 0f
                ? (first.delta.y + second.delta.y) * 0.5f
                : 0f;

            if (gesture == Gesture.None)
            {
                gesture = Gesture.Pending;
                pendingZoom = pendingRotate = pendingTilt = 0f;
                return;   // 두 번째 손가락이 닿은 프레임의 delta는 튈 수 있어 무시
            }

            if (gesture == Gesture.Pending)
            {
                // 비튼 각도를 손가락이 움직인 호 길이(픽셀)로 바꿔 같은 기준으로 비교
                float radius = Vector2.Distance(firstPos, secondPos) * 0.5f;

                pendingZoom += Mathf.Abs(pinch);
                pendingRotate += Mathf.Abs(twist) * Mathf.Deg2Rad * radius;
                pendingTilt += Mathf.Abs(tilt);

                float largest = Mathf.Max(pendingZoom, Mathf.Max(pendingRotate, pendingTilt));
                if (largest < gestureThreshold)
                    return;

                if (largest == pendingZoom) gesture = Gesture.Zoom;
                else if (largest == pendingRotate) gesture = Gesture.Rotate;
                else gesture = Gesture.Tilt;
            }

            switch (gesture)
            {
                case Gesture.Zoom:
                    targetDistance -= pinch * pinchSpeed;
                    break;

                case Gesture.Rotate:
                    // 손가락을 돌린 방향으로 화면이 같이 돌아가도록
                    targetYaw += twist * twistSpeed;
                    break;

                case Gesture.Tilt:
                    // 위로 밀면 눕혀서 멀리 보기
                    targetPitch -= tilt * tiltSpeed;
                    break;
            }
        }

        private void LateUpdate()
        {
            if (lookAt == null)
                return;

            float dt = Time.deltaTime;

            // 프레임레이트 독립 보간
            float zoomT = 1f - Mathf.Exp(-zoomSmooth * dt);
            float angleT = 1f - Mathf.Exp(-angleSmooth * dt);

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomT);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, angleT);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, angleT);

            Vector3 goal = lookAt.position + targetOffset;

            focusPosition = followSmooth > 0f
                ? Vector3.Lerp(focusPosition, goal, 1f - Mathf.Exp(-followSmooth * dt))
                : goal;

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            transform.SetPositionAndRotation(
                focusPosition - rotation * Vector3.forward * currentDistance,
                rotation
            );
        }

        private void OnValidate()
        {
            if (maxDistance < minDistance)
                maxDistance = minDistance;

            if (maxPitch < minPitch)
                maxPitch = minPitch;
        }
    }
}
