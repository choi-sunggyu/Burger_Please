using UnityEngine;

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
        Employee e = Instantiate(employeePrefab, spawnPoint.position, spawnPoint.rotation);
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