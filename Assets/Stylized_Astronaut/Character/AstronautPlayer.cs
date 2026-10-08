using UnityEngine;

namespace AstronautPlayer
{
    public class AstronautPlayer : MonoBehaviour
    {
        private Animator anim;
        private CharacterController controller;

        [Header("Movement")]
        public float speed = 6.0f;
        public float turnSpeed = 10.0f;
        public float gravity = 20.0f;

        [Header("Camera")]
        public Transform cameraTransform;

        private float verticalVelocity;

        void Start()
        {
            controller = GetComponent<CharacterController>();
            anim = GetComponentInChildren<Animator>();

            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            Move();
        }

        private void Move()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

            // 입력이 있는지 확인
            bool isMoving = inputDirection.sqrMagnitude > 0.01f;

            anim.SetInteger("AnimationPar", isMoving ? 1 : 0);

            if (isMoving)
            {
                inputDirection.Normalize();

                // 카메라의 수평 방향만 사용
                Vector3 cameraForward = cameraTransform.forward;
                Vector3 cameraRight = cameraTransform.right;

                cameraForward.y = 0f;
                cameraRight.y = 0f;

                cameraForward.Normalize();
                cameraRight.Normalize();

                // 카메라 기준 이동 방향
                Vector3 moveDirection =
                    cameraForward * inputDirection.z +
                    cameraRight * inputDirection.x;

                // 이동 방향을 바라보게 함
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );

                // 이동
                controller.Move(moveDirection * speed * Time.deltaTime);
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
    }
}