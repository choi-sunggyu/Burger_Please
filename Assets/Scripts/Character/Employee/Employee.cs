using UnityEngine;

public class Employee : MonoBehaviour
{
    public JobRole Roles { get; private set; }

    JobBoard board;
    NavMover mover;
    // (상태 머신은 2단계에서)

    void Awake() => mover = GetComponent<NavMover>();

    // 스포너가 생성 직후 호출
    public void Init(JobBoard board, JobRole roles)
    {
        this.board = board;
        Roles = roles;
    }
}
