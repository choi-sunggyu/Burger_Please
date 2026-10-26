using UnityEngine;

public enum EmployeeState { Idle, Moving, Working }

// 직원의 두뇌: 일 찾기 → 이동 → 일하기
// 실제 줍기/내려놓기/판매/포장은 Worker 자식의 컴포넌트와 장소 쪽 코드가 처리하므로
// 이 클래스는 "어디로 갈지"와 "무엇을 주워도 되는지"만 정한다.
public class Employee : MonoBehaviour
{
    [SerializeField] float searchInterval = 0.5f;   // 일을 찾는 간격

    public JobRole Roles { get; private set; }
    public EmployeeState State => state;            // Inspector Debug 모드 확인용
    public GrabAction Hands { get; private set; }   // 손 (Worker 자식의 GrabAction)

    JobBoard board;
    NavMover mover;
    EmployeeState state;
    IJob job;            // 지금 맡은 일
    Transform target;    // 지금 가고 있는 / 서 있는 곳
    float searchTimer;

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

    // ───── 매 프레임: 조건만 확인 ─────
    void Update()
    {
        if (board == null) return;

        switch (state)
        {
            case EmployeeState.Idle:
                SearchJob();
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

    void SearchJob()
    {
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = searchInterval;

        if (!board.TryClaim(this, out job)) return;

        if (Hands != null) Hands.SetFilter(job.WantedType);   // 이 일에 필요한 종류만 줍기
        ChangeState(EmployeeState.Moving);
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
                break;

            case EmployeeState.Moving:
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
