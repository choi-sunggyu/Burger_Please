
using UnityEngine;

// 손잡이를 밀어서 움직이는 카트 (W/S = 전진/후진, A/D = 회전)
// 회전할 때 차체가 frontPivot을 중심으로 돈다
[RequireComponent(typeof(Rigidbody))]
public class CartController : MonoBehaviour
{
    [SerializeField] private Transform frontPivot;   // 손잡이 → 앞쪽 방향이 진행 방향
    [SerializeField] private Transform handlePoint;
    [SerializeField, Range(0.1f, 1.5f)] private float pushSpeedRatio = 0.75f; // 걷기 속도 대비
    [SerializeField] private float accelerationTime = 0.5f;
    [SerializeField] private float brakeTime = 0.3f;
    [SerializeField] private float coastStopTime = 1f;   // 손을 놓은 뒤 멈추기까지
    [SerializeField] private float turnSpeed = 70f;      // deg/s
    [SerializeField] private float turnAccelerationTime = 0.25f;
    [SerializeField] private float attachBlendTime = 0.15f;

    private Rigidbody rb;
    private Collider[] cartColliders;
    private CapsuleCollider playerProxy;
    private Transform player;
    private CharacterController playerController;
    private Vector3 localForward, localUp, handleOffset, axleOffset, standOffset, heldForward, attachStartPos;
    private Quaternion attachStartRot;
    private Vector2 input;
    private float maxSpeed = 1f, attachTimer;

    public bool IsGrabbed { get; private set; }
    public float Speed01 => Mathf.Clamp01(Mathf.Abs(Vector3.Dot(rb.linearVelocity, GetForward(rb.rotation))) / maxSpeed);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotation; // 회전 제약이 있으면 A/D 회전이 막힌다 (기울기는 FixedUpdate에서 직접 세움)
        rb.angularDamping = 0f; // 회전 속도는 직접 계산하므로 감쇠 제거
        rb.centerOfMass = rb.centerOfMass; // 프록시 콜라이더가 붙어도 무게중심이 바뀌지 않게 고정

        // 바닥 마찰은 직접 계산하므로 콜라이더 마찰 제거
        var wheels = new PhysicsMaterial("CartWheels") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
        cartColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in cartColliders)
            if (!col.isTrigger) col.sharedMaterial = wheels;

        Quaternion inv = Quaternion.Inverse(transform.rotation);
        Vector3 forward = Vector3.ProjectOnPlane(frontPivot.position - handlePoint.position, Vector3.up).normalized;
        localUp = inv * Vector3.up;
        localForward = inv * forward;
        handleOffset = inv * (handlePoint.position - transform.position);
        axleOffset = inv * (frontPivot.position - transform.position); // A/D 회전의 중심
    }

    public bool TryGrab(Transform target)
    {
        if (IsGrabbed || target == null || !target.TryGetComponent(out playerController))
            return false;

        player = target;
        IsGrabbed = true;
        input = Vector2.zero;
        heldForward = GetForward(rb.rotation);

        // 손잡이 뒤쪽 중앙, 카트 몸체와 겹치지 않는 위치에 선다
        Quaternion yaw = Quaternion.LookRotation(heldForward);
        Vector3 relative = Quaternion.Inverse(yaw) * (player.position - handlePoint.position);
        float radius = playerController.radius * Mathf.Max(player.lossyScale.x, player.lossyScale.z);
        float minDistance = GetRearClearance(heldForward) + radius * 1.1f;
        standOffset = new Vector3(0f, relative.y, -Mathf.Max(-relative.z, minDistance));

        attachStartPos = player.position;
        attachStartRot = player.rotation;
        attachTimer = 0f;
        playerController.enabled = false; // 붙어 있는 동안은 카트가 플레이어 위치를 결정
        EnablePlayerProxy(yaw, radius);
        rb.WakeUp();
        return true;
    }

    public void Release()
    {
        if (!IsGrabbed) return;
        IsGrabbed = false;
        input = Vector2.zero;
        if (playerProxy != null) playerProxy.enabled = false;
        playerController.enabled = true;
        playerController = null;
        player = null;
    }

    // x = 회전(A/D), y = 전진/후진(W/S)
    public void SetInput(Vector2 moveInput, float walkSpeed)
    {
        maxSpeed = Mathf.Max(0.01f, walkSpeed * pushSpeedRatio);
        input = Vector2.ClampMagnitude(moveInput, 1f);
    }

    private void FixedUpdate()
    {
        // 카트가 기울지 않게 Y축 회전만 허용
        Vector3 up = rb.rotation * localUp;
        if (Vector3.Angle(up, Vector3.up) > 0.05f)
            rb.rotation = Quaternion.FromToRotation(up, Vector3.up) * rb.rotation;

        if (!IsGrabbed && (rb.IsSleeping() || rb.linearVelocity.sqrMagnitude < 0.0001f && Mathf.Abs(rb.angularVelocity.y) < 0.001f))
            return;

        float dt = Time.fixedDeltaTime;
        Vector3 forward = GetForward(rb.rotation);
        float speed = Vector3.Dot(rb.linearVelocity, forward);
        float targetSpeed = IsGrabbed ? input.y * maxSpeed : 0f;
        float steer = IsGrabbed ? input.x : 0f;

        bool accelerating = Mathf.Abs(targetSpeed) > Mathf.Abs(speed) && targetSpeed * speed >= 0f;
        float speedTime = !IsGrabbed ? coastStopTime : accelerating ? accelerationTime : brakeTime;
        speed = Mathf.MoveTowards(speed, targetSpeed, maxSpeed / Mathf.Max(0.01f, speedTime) * dt);

        float maxYaw = turnSpeed * Mathf.Deg2Rad;
        float yawRate = Mathf.MoveTowards(rb.angularVelocity.y, steer * maxYaw,
            maxYaw / Mathf.Max(0.01f, IsGrabbed ? turnAccelerationTime : coastStopTime) * dt);

        // 회전 입력이 없으면 잡은 방향을 그대로 유지 (W/S만 누르면 절대 회전하지 않음)
        if (Mathf.Abs(steer) > 0.01f) heldForward = forward;
        else if (IsGrabbed) yawRate = Vector3.SignedAngle(forward, heldForward, Vector3.up) * Mathf.Deg2Rad / dt;

        // 차체 속도 = 진행 속도 + frontPivot 중심 회전
        Vector3 axle = rb.position + rb.rotation * axleOffset;
        Vector3 velocity = forward * speed + Vector3.Cross(Vector3.up * yawRate, rb.worldCenterOfMass - axle);
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.up * yawRate;
    }

    private void LateUpdate()
    {
        if (!IsGrabbed) return;

        // 보간된 Transform 기준으로 따라가야 떨림이 없다
        Quaternion yaw = Quaternion.LookRotation(GetForward(transform.rotation));
        Vector3 target = transform.position + transform.rotation * handleOffset + yaw * standOffset;
        attachTimer += Time.deltaTime;
        float t = attachBlendTime > 0f ? Mathf.SmoothStep(0f, 1f, attachTimer / attachBlendTime) : 1f;
        player.SetPositionAndRotation(Vector3.Lerp(attachStartPos, target, t), Quaternion.Slerp(attachStartRot, yaw, t));
    }

    // 밀고 있는 동안 플레이어 몸통 크기의 콜라이더를 카트에 붙여 벽 충돌을 함께 처리
    private void EnablePlayerProxy(Quaternion yaw, float radius)
    {
        if (playerProxy == null)
        {
            var proxy = new GameObject("PlayerProxyCollider") { layer = gameObject.layer };
            proxy.transform.SetParent(transform, false);
            playerProxy = proxy.AddComponent<CapsuleCollider>();
            playerProxy.direction = 1;
        }

        // 바닥에 닿아 마찰이 생기지 않도록 계단 높이만큼 띄운다
        float scaleY = player.lossyScale.y;
        float height = playerController.height * scaleY;
        float lift = Mathf.Min(playerController.stepOffset * scaleY, height * 0.5f);
        Vector3 center = handlePoint.position + yaw * standOffset + Vector3.up * (playerController.center.y * scaleY + lift * 0.5f);

        playerProxy.transform.localScale = Vector3.one / Mathf.Max(0.0001f, transform.lossyScale.x);
        playerProxy.transform.SetPositionAndRotation(center, yaw);
        playerProxy.radius = radius;
        playerProxy.height = Mathf.Max(height - lift, radius * 2f);
        playerProxy.enabled = true;
    }

    // 손잡이에서 카트 몸체 가장 뒤쪽까지의 거리
    private float GetRearClearance(Vector3 forward)
    {
        float clearance = 0f;
        foreach (Collider col in cartColliders)
        {
            if (col.isTrigger || col == playerProxy || col is MeshCollider { convex: false }) continue;
            Vector3 closest = col.ClosestPoint(handlePoint.position - forward * (col.bounds.size.magnitude + 1f));
            clearance = Mathf.Max(clearance, Vector3.Dot(handlePoint.position - closest, forward));
        }
        return clearance;
    }

    private Vector3 GetForward(Quaternion rotation) => Vector3.ProjectOnPlane(rotation * localForward, Vector3.up).normalized;

    private void OnDisable() => Release();
}
