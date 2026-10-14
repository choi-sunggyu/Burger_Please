using UnityEngine;
using UnityEngine.AI;

public class NavMover : MonoBehaviour
{
    NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void MoveTo(Vector3 target)
    {
        agent.isStopped = false;
        agent.updateRotation = true; // stop에서 껐었음
        agent.SetDestination(target);
    }

    public void Stop()
    {
        agent.isStopped = true;
        agent.updateRotation = false;   // 이후 회전은 코드가 직접 제어
    }

    public bool HasArrived()
    {
        if (agent.pathPending) return false;                       // 아직 경로 계산 중
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }
}