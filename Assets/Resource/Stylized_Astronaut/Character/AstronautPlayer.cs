
using UnityEngine;
using UnityEngine.InputSystem;

namespace AstronautPlayer
{
    [RequireComponent(typeof(CharacterController))]
    public class AstronautPlayer : MonoBehaviour
    {
        private Animator anim;
        private CharacterController controller;

        [Header("Movement")]
        public float speed = 6.0f;
        public float turnSpeed = 20.0f;
        public float gravity = 20.0f;

        [Header("Camera")]
        public Transform cameraTransform;

        [Header("Touch")]
        [SerializeField] private TouchJoystick joystick;   // 비워두면 씬에서 찾음

        private float verticalVelocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponentInChildren<Animator>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (joystick == null)
                joystick = FindFirstObjectByType<TouchJoystick>();
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            // New Input System
            Vector2 input = ReadMovementInput();

            Vector3 inputDirection =
                new Vector3(input.x, 0f, input.y);

            bool isMoving = inputDirection.sqrMagnitude > 0.01f;

            if (anim != null)
                anim.SetInteger("AnimationPar", isMoving ? 1 : 0);

            if (isMoving && cameraTransform != null)
            {
                // 조이스틱은 드래그 정도만큼 속도 조절 (키보드는 항상 1)
                float strength = Mathf.Clamp01(inputDirection.magnitude);

                inputDirection.Normalize();

                // 카메라의 고정 요(수평 각도)만 사용 → 피치와 무관하게 조작 방향 일정
                Quaternion yawRotation =
                    Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f);

                // 카메라 기준 이동
                Vector3 moveDirection = yawRotation * inputDirection;

                moveDirection.Normalize();

                // 이동 방향 회전
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                // 이동
                controller.Move(
                    moveDirection * speed * strength * Time.deltaTime
                );
            }

            // 중력
            if (controller.isGrounded)
            {
                verticalVelocity = -1f;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            controller.Move(
                Vector3.up * verticalVelocity * Time.deltaTime
            );
        }

        private Vector2 ReadMovementInput()
        {
            // 터치 조이스틱 우선
            if (joystick != null && joystick.Value != Vector2.zero)
                return joystick.Value;

            if (Keyboard.current == null)
                return Vector2.zero;

            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.aKey.isPressed)
                horizontal -= 1f;

            if (Keyboard.current.dKey.isPressed)
                horizontal += 1f;

            if (Keyboard.current.wKey.isPressed)
                vertical += 1f;

            if (Keyboard.current.sKey.isPressed)
                vertical -= 1f;

            return new Vector2(horizontal, vertical);
        }
    }
}
