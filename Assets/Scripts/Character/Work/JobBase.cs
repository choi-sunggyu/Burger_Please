using UnityEngine;

// 모든 일의 공통 부분: 역할, JobBoard 등록/해제
public abstract class JobBase : MonoBehaviour, IJob
{
    [SerializeField] JobBoard board;

    public abstract JobRole Role { get; }
    public abstract bool IsStillNeeded { get; }
    public virtual StuffType? WantedType => null;          // 줍기가 없는 일은 null
    public abstract Transform GetTarget(Employee worker);

    // 켜질 때 등록, 꺼질 때 해제 → 해금(SetActive)된 구역의 일이 자동으로 추가됨
    protected virtual void OnEnable()  { if (board != null) board.Register(this); }
    protected virtual void OnDisable() { if (board != null) board.Unregister(this); }
}