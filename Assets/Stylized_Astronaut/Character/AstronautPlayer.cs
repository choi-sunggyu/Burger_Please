
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

        private float verticalVelocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponentInChildren<Animator>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
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
                inputDirection.Normalize();

                // 카메라의 수평 방향만 사용
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;

                cameraForward.Normalize();
                cameraRight.Normalize();

                // 카메라 기준 이동
                Vector3 moveDirection =
                    cameraForward * inputDirection.z +
                    cameraRight * inputDirection.x;

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
                    moveDirection * speed * Time.deltaTime
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
