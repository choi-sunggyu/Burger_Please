
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RandomVehicleMove : MonoBehaviour
{
    [SerializeField] private float searchRadius = 30f;
    [SerializeField] private float arrivalDistance = 1.5f;
    [SerializeField] private float retryInterval = 1f;

    private NavMeshAgent agent;
    private float nextRetryTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        agent.autoBraking = false;

        // 수많은 차량의 동시 경로 탐색을 분산
        nextRetryTime = Time.time + Random.Range(0f, 1f);
    }

    private void Update()
    {
        if (!agent.isOnNavMesh)
            return;

        if (Time.time < nextRetryTime)
            return;

        if (agent.pathPending)
            return;

        if (!agent.hasPath ||
            agent.remainingDistance <= arrivalDistance)
        {
            SetRandomDestination();
        }
    }

    private void SetRandomDestination()
    {
        // 실패할 경우 재시도 간격 적용
        nextRetryTime = Time.time + retryInterval;

        for (int i = 0; i < 5; i++)
        {
            Vector3 randomPoint =
                transform.position +
                Random.insideUnitSphere * searchRadius;

            if (!NavMesh.SamplePosition(
                randomPoint,
                out NavMeshHit hit,
                5f,
                agent.areaMask))
            {
                continue;
            }

            if (Vector3.Distance(
                transform.position, hit.position) < arrivalDistance * 2f)
            {
                continue;
            }

            agent.SetDestination(hit.position);
            return;
        }
    }
}
