using UnityEngine;
using UnityEngine.AI;

public enum EmployeeState { Idle, Moving, Working }

// 직원의 두뇌: 일 찾기 → 이동 → 일하기 (일이 없으면 근처를 돌아다님)
// 실제 줍기/내려놓기/판매/포장은 Worker 자식의 컴포넌트와 장소 쪽 코드가 처리하므로
// 이 클래스는 "어디로 갈지"와 "무엇을 주워도 되는지"만 정한다.
public class Employee : MonoBehaviour
{
    [SerializeField] float searchInterval = 0.5f;   // 일을 찾는 간격
    [SerializeField] JobRole defaultRoles = JobRole.All;   // 씬에 직접 둔 직원의 역할 (스포너로 만들면 Init이 덮어씀)

    [Header("일이 없을 때 돌아다니기")]
    [SerializeField] bool wanderWhenIdle = true;
    [SerializeField] Transform wanderCenter;        // 돌아다닐 중심 (비우면 처음 서 있던 곳)
    [SerializeField] float wanderRadius = 5f;       // 중심에서 이 반경 안의 NavMesh 위 지점으로 감
    [SerializeField] Vector2 wanderWait = new Vector2(1f, 3f);   // 도착 후 쉬는 시간 (최소, 최대)

    public JobRole Roles { get; private set; }
    public EmployeeState State => state;            // Inspector Debug 모드 확인용
    public GrabAction Hands { get; private set; }   // 손 (Worker 자식의 GrabAction)

    JobBoard board;
    NavMover mover;
    EmployeeState state;
    IJob job;            // 지금 맡은 일
    Transform target;    // 지금 가고 있는 / 서 있는 곳
    float searchTimer;
    Vector3 home;        // 처음 서 있던 곳 (wanderCenter가 없을 때 중심)
    bool wandering;      // 돌아다니는 목적지로 가는 중인가
    float wanderTimer;

    // 맡은 일이 사라졌나 (인터페이스로 들고 있으면 Unity의 '파괴됨' 판정이 안 되므로 Object로 한 번 더 확인)
    bool JobGone => job == null || (job is Object o && o == null);

    void Awake()
    {
        mover = GetComponent<NavMover>();
        Hands = GetComponentInChildren<GrabAction>();
        if (Hands != null) Hands.SetFilter(null);   // 일이 없을 땐 아무것도 줍지 않음
    }

    // 스포너가 생성 직후 호출
    public void Init(JobBoard board, JobRole roles)
    {
        this.board = board;
        Roles = roles;
    }

    // 스포너 없이 씬에 직접 둔 직원은 Init이 불리지 않으므로 여기서 스스로 준비
    void Start()
    {
        home = transform.position;
        if (Roles == JobRole.None) Init(FindFirstObjectByType<JobBoard>(), defaultRoles);
    }

    // ───── 매 프레임: 조건만 확인 ─────
    void Update()
    {
        switch (state)
        {
            case EmployeeState.Idle:
                if (!SearchJob()) Wander();
                break;

            case EmployeeState.Moving:
                if (!CheckJob()) break;
                if (mover.HasArrived()) ChangeState(EmployeeState.Working);
                break;

            case EmployeeState.Working:
                CheckJob();
                break;
        }
    }

    // 일을 맡았으면 true (씬에 JobBoard가 없으면 항상 false → 돌아다니기만 함)
    bool SearchJob()
    {
        if (board == null) return false;

        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return false;
        searchTimer = searchInterval;

        if (!board.TryClaim(this, out job)) return false;

        if (Hands != null) Hands.SetFilter(job.WantedType);   // 이 일에 필요한 종류만 줍기
        ChangeState(EmployeeState.Moving);
        return true;
    }

    // 일이 없을 때: 목적지에 도착하면 잠깐 쉬었다가 근처의 다른 곳으로
    void Wander()
    {
        if (!wanderWhenIdle) return;
        if (wandering && !mover.HasArrived()) return;   // 아직 가는 중

        wanderTimer -= Time.deltaTime;
        if (wanderTimer > 0f) return;
        wanderTimer = Random.Range(wanderWait.x, wanderWait.y);

        wandering = TryGetWanderPoint(out Vector3 point);
        if (wandering) mover.MoveTo(point);
    }

    // 중심 주변의 무작위 지점을 가장 가까운 NavMesh 위로 맞춤 (못 찾으면 이번엔 쉼)
    bool TryGetWanderPoint(out Vector3 point)
    {
        Vector3 center = wanderCenter != null ? wanderCenter.position : home;
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

        bool found = NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);
        point = found ? hit.position : center;
        return found;
    }

    // 일이 아직 유효하고 목적지가 그대로면 true
    // (IsStillNeeded는 "새로 맡을 가치"만 판단하고, 진행 중인 일은 GetTarget이 끝을 알려줌)
    bool CheckJob()
    {
        if (JobGone) { FinishJob(); return false; }

        Transform next = job.GetTarget(this);
        if (next == null) { FinishJob(); return false; }                              // 일 끝
        if (next != target) { ChangeState(EmployeeState.Moving); return false; }      // 다음 단계로

        return true;
    }

    // ───── 상태에 들어갈 때 한 번만 ─────
    void ChangeState(EmployeeState next)
    {
        state = next;

        switch (next)
        {
            case EmployeeState.Idle:
                job = null;
                target = null;
                searchTimer = searchInterval;
                wandering = false;
                wanderTimer = Random.Range(wanderWait.x, wanderWait.y);   // 일을 마치면 잠깐 쉬고 돌아다님
                break;

            case EmployeeState.Moving:
                wandering = false;
                target = job.GetTarget(this);
                if (target == null) { FinishJob(); return; }
                mover.MoveTo(target.position);
                break;

            case EmployeeState.Working:
                mover.Stop();
                break;
        }
    }

    void FinishJob()
    {
        if (job != null && board != null) board.Release(job);
        if (Hands != null) Hands.SetFilter(null);
        ChangeState(EmployeeState.Idle);
    }

    void OnDestroy()
    {
        if (job != null && board != null) board.Release(job);   // 해고되어도 일이 예약된 채 남지 않게
    }
}
