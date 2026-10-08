using UnityEngine;
using UnityEngine.AI;

public class CustomerMover : MonoBehaviour
{
    NavMeshAgent agent;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public void MoveTo(Vector3 target)
    {
        agent.isStopped = false;
        agent.SetDestination(target);
    }

    public bool HasArrived()
    {
        if (agent.pathPending) return false;                       // 아직 경로 계산 중
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }
}