using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// 사무실 구역: 플레이어가 안에 있을 때만 고용 UI를 켠다 (거리 확인 방식, 트리거 없음)
public class OfficeHireZone : MonoBehaviour
{
    [Header("구역")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 zoneSize = new Vector3(200f, 100f, 240f);   // 이 오브젝트 위치가 중심

    [Header("UI")]
    [SerializeField] private GameObject hirePanel;
    [SerializeField] private Button hireButton;
    [SerializeField] private TMP_Text countText;

    [Header("알바")]
    [SerializeField] private GameObject workerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxWorkers = 5;
    [SerializeField] private float workerScale = 10f;   // 플레이어와 같은 크기
    [SerializeField] private float workerSpeed = 100f;  // 플레이어와 같은 속도
    [SerializeField] private CargoTruck unloadTruck;
    [SerializeField] private CargoTruck loadTruck;
    [SerializeField] private ShelfStorage storage;

    private const float SpawnSearchRadius = 50f;

    private int workerCount;

    private void Awake()
    {
        hireButton.onClick.AddListener(Hire);
        hirePanel.SetActive(false);
        UpdateUI();
    }

    private void Update()
    {
        bool inside = IsInside(player.position);
        if (hirePanel.activeSelf != inside) hirePanel.SetActive(inside);
    }

    private bool IsInside(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        return Mathf.Abs(offset.x) <= zoneSize.x * 0.5f && Mathf.Abs(offset.z) <= zoneSize.z * 0.5f;
    }

    private void Hire()
    {
        if (workerCount >= maxWorkers) return;

        // 스폰 지점 근처에서 가장 가까운 NavMesh 위의 점에 생성 (공중에 생성되면 에이전트가 만들어지지 않음)
        Vector3 pos = spawnPoint.position;
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, SpawnSearchRadius, NavMesh.AllAreas))
            pos = hit.position;
        else
            Debug.LogWarning($"[OfficeHireZone] 스폰 지점 근처에 NavMesh가 없습니다: {pos}", spawnPoint);

        GameObject go = Instantiate(workerPrefab, pos, spawnPoint.rotation);
        VLB.BakedMaterialRemap.Apply(go);
        go.transform.localScale = Vector3.one * workerScale;

        var worker = go.AddComponent<MockWorker>();
        worker.Init(unloadTruck, loadTruck, storage, workerSpeed);

        workerCount++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        countText.text = $"알바생 {workerCount}/{maxWorkers}";
        hireButton.interactable = workerCount < maxWorkers;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, zoneSize);
    }
}
