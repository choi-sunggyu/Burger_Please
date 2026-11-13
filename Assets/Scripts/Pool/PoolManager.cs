using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 씬에 배치된 PoolManager만 인스턴스로 인정 (자동 생성 X → 프리팹 참조가 비는 문제 방지)
    public static PoolManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private GameObject rabbitPrefab;
    [SerializeField] private int poolSize = 100;
    [SerializeField] private bool allowExpand = true;   // 풀이 바닥나면 새로 생성할지

    [Header("Spawn Area")]
    [SerializeField] private Transform spawnCenter;      // 비워두면 PoolManager 위치 사용
    [SerializeField] private float spawnRadius = 20f;
    [SerializeField] private GroundSettings ground = new GroundSettings(); // 바닥/장애물 판정

    [Header("Test")]
    [SerializeField] private int spawnOnStart = 10;

    private readonly Queue<GameObject> inactive = new Queue<GameObject>();
    private readonly HashSet<GameObject> active = new HashSet<GameObject>();
    private Transform inactiveRoot;   // 비활성 부모: 이 아래에서 생성하면 Awake/OnEnable이 돌지 않음
    private int createdCount;

    public int ActiveCount => active.Count;
    public int InactiveCount => inactive.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rabbitPrefab == null)
        {
            Debug.LogError("[PoolManager] rabbitPrefab이 비어 있습니다.", this);
            enabled = false;
            return;
        }

        CreateInactiveRoot();
        InitializePool();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (spawnOnStart > 0) Spawn(spawnOnStart);
    }

    // ───────────────────── 풀 생성 ─────────────────────

    private void CreateInactiveRoot()
    {
        var root = new GameObject("_InactivePool");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        inactiveRoot = root.transform;
    }

    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
            inactive.Enqueue(CreateRabbit());
    }

    private GameObject CreateRabbit()
    {
        // 부모가 비활성이므로 생성 직후 Awake/OnEnable이 돌지 않는다
        // (엉뚱한 생성 위치를 토끼가 '집'으로 기억하는 문제 방지)
        GameObject rabbit = Instantiate(rabbitPrefab, inactiveRoot);
        VLB.BakedMaterialRemap.Apply(rabbit);
        rabbit.name = $"Rabbit_{createdCount++}";
        rabbit.SetActive(false); // activeSelf를 꺼둬야 부모를 옮겨도 자동으로 켜지지 않음
        return rabbit;
    }

    // ───────────────────── 스폰 / 반납 ─────────────────────

    /// <summary>count 마리를 바닥 위 랜덤 위치(장애물 회피)에 소환하고, 실제 소환된 수를 반환.</summary>
    public int Spawn(int count)
    {
        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (!GroundSampler.TryGetPoint(center, spawnRadius, ground, out Vector3 pos))
            {
                Debug.LogWarning("[PoolManager] 스폰 위치를 못 찾았습니다. 레이어 마스크와 반경을 확인하세요.", this);
                break;
            }

            GameObject rabbit = GetFromPool();
            if (rabbit == null) break;

            Activate(rabbit, pos);
            spawned++;
        }

        return spawned;
    }

    private GameObject GetFromPool()
    {
        if (inactive.Count > 0) return inactive.Dequeue();
        if (allowExpand) return CreateRabbit();

        Debug.LogWarning("[PoolManager] 풀이 비었습니다.", this);
        return null;
    }

    private void Activate(GameObject rabbit, Vector3 pos)
    {
        Transform t = rabbit.transform;
        t.SetParent(transform, false);
        t.SetPositionAndRotation(pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        // 위치를 먼저 잡고 켜야 OnEnable에서 이 자리를 '집'으로 기억한다
        rabbit.SetActive(true);

        active.Add(rabbit);
    }

    public void Despawn(GameObject rabbit)
    {
        if (!active.Remove(rabbit)) return; // 이미 반납됐거나 풀 소속이 아님

        rabbit.SetActive(false);
        rabbit.transform.SetParent(inactiveRoot, false);
        inactive.Enqueue(rabbit);
    }

    public void DespawnAll()
    {
        // 순회 중 컬렉션 수정 방지를 위해 복사본으로 돈다
        foreach (GameObject rabbit in new List<GameObject>(active))
            Despawn(rabbit);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.up, spawnRadius);
    }
#endif
}