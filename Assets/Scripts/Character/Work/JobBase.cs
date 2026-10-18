using UnityEngine;

// 모든 일의 공통 부분: 역할, JobBoard 등록/해제, 맡은 직원 기억
public abstract class JobBase : MonoBehaviour, IJob
{
    [SerializeField] JobBoard board;   // 비워두면 씬에서 자동으로 찾음

    public abstract JobRole Role { get; }
    public abstract bool IsStillNeeded { get; }
    public virtual StuffType? WantedType => null;          // 줍기가 없는 일은 null
    public abstract Transform GetTarget(Employee worker);

    protected Employee Worker { get; private set; }        // 지금 이 일을 맡은 직원 (없으면 null)

    public virtual void OnAssigned(Employee worker) => Worker = worker;
    public virtual void OnReleased() => Worker = null;

    // 켜질 때 등록, 꺼질 때 해제 → 해금(SetActive)된 구역의 일이 자동으로 추가됨
    protected virtual void OnEnable()
    {
        if (board == null) board = FindFirstObjectByType<JobBoard>();
        if (board != null) board.Register(this);
        else Debug.LogWarning($"[{GetType().Name}] {name}: 씬에 JobBoard가 없습니다.", this);
    }

    protected virtual void OnDisable()
    {
        if (board != null) board.Unregister(this);
    }
}
