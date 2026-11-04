using UnityEngine;

/// <summary>
/// NavMesh 없이 배회하는 토끼.
/// 매 프레임: 원하는 방향 계산(배회 + 귀소 + 회피) → 그 방향으로 회전 → 앞으로 이동 + 바닥에 붙이기.
/// 이동과 휴식을 번갈아 반복한다.
/// </summary>
public class EmployeeWander : MonoBehaviour
{
    private enum State { Idle, Move }

    [Header("Ground / Obstacle")]
    [SerializeField] private GroundSettings ground = new GroundSettings();
    [SerializeField] private float stepHeight = 0.5f;       // 넘을 수 있는 턱 높이

    [Header("Movement")]
    [SerializeField] private Vector2 speedRange = new Vector2(1.5f, 3f);
    [SerializeField] private float turnSpeed = 240f;        // 초당 최대 회전 각도

    [Header("Wander (Reynolds)")]
    [SerializeField] private float wanderDistance = 2f;     // 앞쪽 가상 원의 중심까지 거리
    [SerializeField] private float wanderRadius = 1f;       // 가상 원 반지름 (클수록 크게 꺾음)
    [SerializeField] private float wanderJitter = 4f;       // 초당 흔들림 (클수록 변덕스러움)

    [Header("Home Leash")]
    [SerializeField] private float homeRadius = 12f;        // 스폰 지점에서 이 이상 멀어지면 끌어당김

    [Header("Obstacle Sensor")]
    [SerializeField] private float sensorLength = 1.5f;
    [SerializeField] private float sensorAngle = 35f;
    [SerializeField] private float sensorHeight = 0.3f;
    [SerializeField] private float sensorInterval = 0.1f;   // 레이캐스트 주기 (매 프레임 X → 성능)
    [SerializeField] private float avoidWeight = 3f;

    [Header("Rhythm")]
    [SerializeField] private Vector2 moveTimeRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 idleTimeRange = new Vector2(0.5f, 2f);

    /// <summary>Animator 파라미터 연결용</summary>
    public float CurrentSpeed { get; private set; }

    private State state;
    private float stateTimer;
    private float speed;
    private Vector3 home;
    private Vector2 wanderTarget;   // 가상 원 위의 점 (로컬 XZ)
    private Vector3 avoidDir;       // 장애물 센서 결과 (sensorInterval마다 갱신)
    private Vector3 edgeAvoid;      // 낭떠러지/급경사 회피 (서서히 감쇠)
    private float sensorTimer;
    private float sideBias;         // 벽 정면 충돌 시 비켜갈 방향 (개체마다 고정)

    private void OnEnable()
    {
        // 풀에서 꺼낼 때마다 초기화 — PoolManager가 위치를 먼저 잡고 켜므로 여기가 '집'
        home = transform.position;
        wanderTarget = Vector2.up * wanderRadius;
        avoidDir = edgeAvoid = Vector3.zero;
        sensorTimer = Random.Range(0f, sensorInterval);   // 100마리 센서 타이밍 분산
        sideBias = Random.value < 0.5f ? -1f : 1f;
        EnterState(Random.value < 0.5f ? State.Idle : State.Move);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        stateTimer -= dt;
        if (stateTimer <= 0f)
            EnterState(state == State.Move ? State.Idle : State.Move);

        if (state == State.Idle)
        {
            CurrentSpeed = 0f;
            return;
        }

        UpdateSensors(dt);
        Vector3 desired = ComputeDesiredDirection(dt);
        Steer(desired, dt);
        MoveForward(desired, dt);
    }

    private void EnterState(State next)
    {
        state = next;
        if (next == State.Move)
        {
            stateTimer = Random.Range(moveTimeRange.x, moveTimeRange.y);
            speed = Random.Range(speedRange.x, speedRange.y);
        }
        else
        {
            stateTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
        }
    }

    // ───────────────────── 1. 원하는 방향 ─────────────────────

    private Vector3 ComputeDesiredDirection(float dt)
    {
        // (a) 배회: 앞쪽 가상 원 위의 점을 조금씩 흔들고, 그 점을 향한다
        wanderTarget += Random.insideUnitCircle * (wanderJitter * dt);
        wanderTarget = wanderTarget.normalized * wanderRadius;
        Vector3 local = new Vector3(wanderTarget.x, 0f, wanderTarget.y + wanderDistance);
        Vector3 desired = Flat(transform.TransformDirection(local)).normalized;

        // (b) 귀소: 집 반경의 70%를 넘으면 점점 강하게 집 쪽으로
        Vector3 toHome = Flat(home - transform.position);
        float pull = Mathf.InverseLerp(homeRadius * 0.7f, homeRadius, toHome.magnitude);
        if (pull > 0f)
            desired = Vector3.Slerp(desired, toHome.normalized, pull);

        // (c) 회피: 장애물 + 낭떠러지
        Vector3 avoid = avoidDir + edgeAvoid;
        if (avoid.sqrMagnitude > 0.0001f)
        {
            desired = (desired + avoid * avoidWeight).normalized;
            wanderTarget = Vector2.up * wanderRadius;   // 회피 중엔 배회가 반대로 끌지 않게 정면으로 리셋
        }

        edgeAvoid = Vector3.MoveTowards(edgeAvoid, Vector3.zero, dt * 2f);
        return desired;
    }

    // ───────────────────── 2. 장애물 센서 (더듬이 3개) ─────────────────────

    private void UpdateSensors(float dt)
    {
        sensorTimer -= dt;
        if (sensorTimer > 0f) return;
        sensorTimer = sensorInterval;

        avoidDir = Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 fwd = Flat(transform.forward).normalized;

        CastWhisker(origin, fwd, sensorLength);
        CastWhisker(origin, Quaternion.Euler(0f, -sensorAngle, 0f) * fwd, sensorLength * 0.7f);
        CastWhisker(origin, Quaternion.Euler(0f, sensorAngle, 0f) * fwd, sensorLength * 0.7f);
    }

    private void CastWhisker(Vector3 origin, Vector3 dir, float length)
    {
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, length,
                             ground.obstacleMask, QueryTriggerInteraction.Ignore))
            return;

        float strength = 1f - hit.distance / length;   // 가까울수록 강하게
        Vector3 away = Flat(hit.normal).normalized;

        // 벽을 정면으로 보면 normal이 진행 방향의 정반대 → 그대로 두면 제자리에서 멈칫거림
        // 개체별로 정해둔 쪽(sideBias)으로 비켜가게 옆 성분을 더한다
        if (Vector3.Dot(away, -dir) > 0.95f)
            away += Vector3.Cross(Vector3.up, dir) * sideBias;

        avoidDir += away.normalized * strength;
    }

    // ───────────────────── 3. 회전 & 이동 ─────────────────────

    private void Steer(Vector3 desired, float dt)
    {
        if (desired.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(desired, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
    }

    private void MoveForward(Vector3 desired, float dt)
    {
        Vector3 fwd = Flat(transform.forward).normalized;

        // 원하는 방향과 많이 어긋나 있으면 감속 → 돌면서 벽에 밀고 들어가는 것 방지
        float align = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(Vector3.Dot(fwd, desired)));
        float moveSpeed = speed * align;
        Vector3 next = transform.position + fwd * (moveSpeed * dt);

        if (TryGetGround(next, out Vector3 groundPoint))
        {
            transform.position = groundPoint;   // 바닥 높이에 붙이기 (언덕/계단 대응)
            CurrentSpeed = moveSpeed;
        }
        else
        {
            // 바닥 없음 / 너무 가파름 / 턱이 너무 높음 → 이동 취소, 뒤로 돌게 유도
            edgeAvoid = -fwd;
            CurrentSpeed = 0f;
        }
    }

    private bool TryGetGround(Vector3 pos, out Vector3 point)
    {
        Vector3 origin = pos + Vector3.up * stepHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, stepHeight * 2f,
                            ground.groundMask, QueryTriggerInteraction.Ignore)
            && ground.IsWalkable(hit.normal))
        {
            point = hit.point;
            return true;
        }

        point = pos;
        return false;
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? home : transform.position;
        UnityEditor.Handles.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, homeRadius);

        Vector3 origin = transform.position + Vector3.up * sensorHeight;
        Vector3 fwd = Flat(transform.forward).normalized;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, fwd * sensorLength);
        Gizmos.DrawRay(origin, Quaternion.Euler(0f, -sensorAngle, 0f) * fwd * sensorLength * 0.7f);
        Gizmos.DrawRay(origin, Quaternion.Euler(0f, sensorAngle, 0f) * fwd * sensorLength * 0.7f);

        // 배회용 가상 원
        Vector3 circleCenter = transform.position + fwd * wanderDistance;
        UnityEditor.Handles.color = Color.magenta;
        UnityEditor.Handles.DrawWireDisc(circleCenter, Vector3.up, wanderRadius);
    }
#endif
}