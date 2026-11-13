
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
        public float speed = 6f;
        public float turnSpeed = 20f;
        public float gravity = 20f;

        [Header("Camera")]
        public Transform cameraTransform;

        private TouchJoystick joystick;

        private PlayerCartInteractor cartInteractor;
        private GrabAction grab;

        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int HoldHash = Animator.StringToHash("Hold");

        private float verticalVelocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponentInChildren<Animator>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            cartInteractor = GetComponent<PlayerCartInteractor>();
            grab = GetComponentInChildren<GrabAction>();

            // 왼쪽 아래 고정 조이스틱 (없으면 붙여서 UI까지 자동 생성)
            joystick = GetComponent<TouchJoystick>();
            if (joystick == null)
                joystick = gameObject.AddComponent<TouchJoystick>();
        }

        // 코스튬 교체 후 활성화된 모델의 Animator를 다시 찾는다
        public void RefreshAnimator()
        {
            anim = GetComponentInChildren<Animator>();
        }

        private void Animate(bool walk)
        {
            if (anim == null)
                return;

            anim.SetBool(WalkHash, walk);
            anim.SetBool(HoldHash, grab != null && grab.IsHolding);
        }

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            Vector2 input = ReadMovementInput();

            if (cartInteractor != null && cartInteractor.IsPushing)
            {
                // 카트를 미는 동안 이동은 카트 물리가 담당 (W/S 전진/후진, A/D 회전)
                cartInteractor.UpdateCartInput(input, speed);

                Animate(cartInteractor.PushSpeed01 > 0.1f);

                verticalVelocity = 0f;
                return;
            }

            Vector3 inputDirection =
                new Vector3(input.x, 0f, input.y);

            bool isMoving = inputDirection.sqrMagnitude > 0.01f;

            Animate(isMoving);

            Vector3 horizontalMove = Vector3.zero;

            if (isMoving && cameraTransform != null)
            {
                float strength = Mathf.Clamp01(inputDirection.magnitude);

                inputDirection.Normalize();

                Quaternion yawRotation = Quaternion.Euler(
                    0f,
                    cameraTransform.eulerAngles.y,
                    0f
                );

                Vector3 moveDirection = yawRotation * inputDirection;
                moveDirection.Normalize();

                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                horizontalMove = moveDirection * speed * strength;
            }

            // 중력
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -1f;
            else
                verticalVelocity -= gravity * Time.deltaTime;

            // 수평 이동과 중력을 한 번에 적용
            Vector3 velocity =
                horizontalMove + Vector3.up * verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }

        private Vector2 ReadMovementInput()
        {
            // 1. 키보드 입력 확인
            Vector2 keyboardInput = ReadKeyboardInput();

            if (keyboardInput.sqrMagnitude > 0.01f)
                return keyboardInput;

            // 2. 키보드 입력이 없으면 터치 입력
            return joystick.Value;
        }

        private Vector2 ReadKeyboardInput()
        {
            if (Keyboard.current == null)
                return Vector2.zero;

            Vector2 input = Vector2.zero;

            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;

            return Vector2.ClampMagnitude(input, 1f);
        }
    }
}
