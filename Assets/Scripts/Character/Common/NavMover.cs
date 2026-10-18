using UnityEngine;
using UnityEngine.AI;

// NavMeshAgent 감싸기 (손님·직원 공용)
public class NavMover : MonoBehaviour
{
    [SerializeField] float arriveTolerance = 0.25f;   // Stopping Distance가 0이어도 이 거리 안이면 도착으로 봄

    NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void MoveTo(Vector3 target)
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[NavMover] {name}이(가) NavMesh 위에 없습니다. 생성 위치와 NavMeshAgent 크기를 확인하세요.", this);
            return;
        }
        agent.isStopped = false;
        agent.updateRotation = true; // stop에서 껐었음
        agent.SetDestination(target);
    }

    public void Stop()
    {
        if (!agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.updateRotation = false;   // 이후 회전은 코드가 직접 제어
    }

    public bool HasArrived()
    {
        if (!agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;                       // 아직 경로 계산 중
        if (agent.remainingDistance > Mathf.Max(agent.stoppingDistance, arriveTolerance)) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }
}
