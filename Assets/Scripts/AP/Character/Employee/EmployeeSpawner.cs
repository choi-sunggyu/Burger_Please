using UnityEngine;
using UnityEngine.AI;

public class EmployeeSpawner : MonoBehaviour
{
    [SerializeField] Employee employeePrefab;
    [SerializeField] Transform spawnPoint;
    [SerializeField] JobBoard board;

    [Header("역할별 색 (구분용)")]
    [SerializeField] Color cleanColor   = Color.gray;
    [SerializeField] Color deliverColor = Color.yellow;
    [SerializeField] Color packColor    = Color.cyan;
    [SerializeField] Color sellColor    = Color.green;

    public Employee Hire(JobRole role)
    {
        Vector3 pos = spawnPoint.position;

        // 스폰 지점 근처(3m 이내)에서 가장 가까운 NavMesh 위의 점에 생성 (공중/벽 속에 생성되는 것 방지)
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            pos = hit.position;
        else
            Debug.LogWarning($"[EmployeeSpawner] 스폰 지점 근처에 NavMesh가 없습니다: {pos}", spawnPoint);

        Employee e = Instantiate(employeePrefab, pos, spawnPoint.rotation);
        e.Init(board, role);
        Tint(e, role);
        return e;
    }

    void Tint(Employee e, JobRole role)
    {
        Color c = role switch
        {
            JobRole.Clean   => cleanColor,
            JobRole.Deliver => deliverColor,
            JobRole.Pack    => packColor,
            JobRole.Sell    => sellColor,
            _               => Color.white,      // 겸직 등
        };
        foreach (var r in e.GetComponentsInChildren<Renderer>())
            r.material.color = c;                // .material → 이 직원만의 복사본이 생김
    }
}