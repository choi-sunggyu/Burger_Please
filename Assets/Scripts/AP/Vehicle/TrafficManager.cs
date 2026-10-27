using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 씬의 도로 타일(5m 격자)을 스캔해서 왕복 2차선 경로를 자동 생성하고 차량을 운행시킨다.
// 1. 컴포넌트 우클릭 -> "Bake Routes From Road Tiles" 로 경로 생성 (에디터에서 1회)
// 2. Play 시 경로마다 폐곡선을 만들고, 경로끼리 겹치는 곳은 교차로(TrafficZone)로 처리
// 차량마다 Update를 두지 않고 여기서 한 번에 이동시킴 (부하 테스트 시 CPU 비용 최소화)
public class TrafficManager : MonoBehaviour
{
    [Serializable]
    public class RouteLine
    {
        public bool enabled = true;
        public Vector3 start;
        public Vector3 end;
    }

    private struct TileCell
    {
        public bool laneX;
        public bool laneZ;
        public float heightSum;
        public int tileCount;
    }

    private struct Candidate
    {
        public bool alongX;
        public float line;
        public int from;
        public int to;
        public int laneTiles;
        public float height;
    }

    [Header("Bake")]
    // 비워두면 씬 전체에서 도로 타일을 찾음
    [SerializeField] private Transform roadRoot;
    [SerializeField] private string tilePrefix = "SM_Env_Road";
    // 도로 위에 덮이는 데칼 류는 무시
    [SerializeField] private string[] ignoreKeywords = { "Arrow", "Patch", "ParkingLines" };
    // 방향이 없는 타일 (교차로, 빈 아스팔트)
    [SerializeField] private string[] connectorKeywords = { "Bare", "Crossing", "Road_Lines" };
    [SerializeField] private float cellSize = 5f;
    // 이 칸 수 이하로 끊긴 곳은 이어진 도로로 판단
    [SerializeField] private int gapTolerance = 2;
    // 차선 타일이 이 수보다 적으면 도로가 아닌 것(주차장 등)으로 판단
    [SerializeField] private int minLaneTiles = 3;
    // 도로 끝에서 유턴 지점까지 여유
    [SerializeField] private float endMargin = 3.5f;
    [SerializeField] private List<RouteLine> routes = new();

    [Header("Lane")]
    // 도로 중앙선 ~ 차선 중심까지 거리 (유턴 반지름이기도 함)
    [SerializeField] private float laneOffset = 2.5f;
    [SerializeField] private int uTurnSegments = 12;
    [SerializeField] private float vehicleHalfWidth = 1.2f;
    // 교차로 영역 확장 (차량 길이 절반 정도)
    [SerializeField] private float zoneMargin = 2.5f;

    [Header("Vehicle")]
    // 지정하면 이 오브젝트의 (활성화된) 자식 차량들을 그대로 경로에 배치 (프리팹 스폰 대신)
    [SerializeField] private Transform existingVehicleRoot;
    [SerializeField] private GameObject[] vehiclePrefabs;
    [SerializeField] private int vehicleCount = 40;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private VehicleDriveSettings drive = new();

    private readonly List<TrafficRoute> runtimeRoutes = new();
    private readonly List<TrafficZone> zones = new();
    private readonly Queue<Transform> existingVehicles = new();
    private Transform vehicleRoot;

    public int VehicleCount
    {
        get
        {
            int count = 0;
            foreach (TrafficRoute route in runtimeRoutes)
                count += route.VehicleCount;
            return count;
        }
    }

    private void Start()
    {
        BuildRuntime();

        if (existingVehicleRoot != null)
            UseExistingVehicles();
        else if (spawnOnStart)
            SpawnVehicles(vehicleCount);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        foreach (TrafficRoute route in runtimeRoutes)
            route.Simulate(dt, drive);
    }

    // 프리팹으로 새 차량 생성
    public void SpawnVehicles(int total)
    {
        ClearVehicles();

        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            return;

        if (vehicleRoot == null)
            vehicleRoot = new GameObject("Traffic_Vehicles").transform;

        Distribute(total, CreateVehicle);
    }

    // 씬에 이미 배치된 차량들을 경로에 재배치
    public void UseExistingVehicles()
    {
        ClearVehicles();
        existingVehicles.Clear();

        foreach (Transform child in existingVehicleRoot)
        {
            if (child.gameObject.activeInHierarchy)
                existingVehicles.Enqueue(child);
        }

        Distribute(existingVehicles.Count, TakeExistingVehicle);

        // 경로에 다 못 들어간 차량은 도로 위에서 겹칠 수 있으므로 비활성화
        if (existingVehicles.Count > 0)
            Debug.LogWarning($"[TrafficManager] 배치하지 못한 기존 차량 {existingVehicles.Count}대 비활성화");

        while (existingVehicles.Count > 0)
            existingVehicles.Dequeue().gameObject.SetActive(false);
    }

    // 전체 차량 수를 경로 길이에 비례해 분배
    private void Distribute(int total, Func<Transform> create)
    {
        if (runtimeRoutes.Count == 0)
            return;

        float totalLength = 0f;
        foreach (TrafficRoute route in runtimeRoutes)
            totalLength += route.Length;

        int remaining = total;
        int spawned = 0;

        for (int i = 0; i < runtimeRoutes.Count; i++)
        {
            TrafficRoute route = runtimeRoutes[i];
            int count = i == runtimeRoutes.Count - 1
                ? remaining
                : Mathf.RoundToInt(total * route.Length / totalLength);

            count = Mathf.Min(count, remaining);
            remaining -= count;
            spawned += route.Spawn(count, create, drive);
        }

        if (spawned < total)
            Debug.LogWarning($"[TrafficManager] 도로 길이에 비해 차량이 많아 {spawned}/{total}대만 배치");
    }

    public void ClearVehicles()
    {
        // 기존 차량은 씬 오브젝트이므로 삭제하지 않음
        foreach (TrafficRoute route in runtimeRoutes)
            route.Clear(existingVehicleRoot == null);
    }

    [ContextMenu("Respawn Vehicles")]
    private void Respawn()
    {
        if (!Application.isPlaying)
            return;

        if (existingVehicleRoot != null)
            UseExistingVehicles();
        else
            SpawnVehicles(vehicleCount);
    }

    private Transform CreateVehicle()
    {
        GameObject prefab = vehiclePrefabs[UnityEngine.Random.Range(0, vehiclePrefabs.Length)];
        GameObject go = Instantiate(prefab, vehicleRoot);
        return PrepareVehicle(go);
    }

    // Distribute에 큐 크기만큼만 요청하므로 비어있을 때 호출되지 않음
    private Transform TakeExistingVehicle()
    {
        return PrepareVehicle(existingVehicles.Dequeue().gameObject);
    }

    private Transform PrepareVehicle(GameObject go)
    {
        // 물리 / NavMesh 이동과 충돌하지 않도록
        if (go.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;

        if (go.TryGetComponent(out RandomVehicleMove randomMove))
            randomMove.enabled = false;

        if (go.TryGetComponent(out NavMeshAgent agent))
            agent.enabled = false;

        return go.transform;
    }

    private void BuildRuntime()
    {
        runtimeRoutes.Clear();
        zones.Clear();

        foreach (RouteLine line in routes)
        {
            if (line.enabled)
                runtimeRoutes.Add(new TrafficRoute(line.start, line.end, laneOffset, uTurnSegments));
        }

        // 경로끼리 겹치는 영역 = 교차로
        List<Rect> areas = new();
        foreach (TrafficRoute route in runtimeRoutes)
            areas.Add(route.GetArea(vehicleHalfWidth));

        List<Rect> overlaps = new();
        for (int a = 0; a < areas.Count; a++)
        {
            for (int b = a + 1; b < areas.Count; b++)
            {
                if (!areas[a].Overlaps(areas[b]))
                    continue;

                Rect overlap = Rect.MinMaxRect(
                    Mathf.Max(areas[a].xMin, areas[b].xMin) - zoneMargin,
                    Mathf.Max(areas[a].yMin, areas[b].yMin) - zoneMargin,
                    Mathf.Min(areas[a].xMax, areas[b].xMax) + zoneMargin,
                    Mathf.Min(areas[a].yMax, areas[b].yMax) + zoneMargin);

                overlaps.Add(overlap);
            }
        }

        // 서로 겹치는 교차로는 하나로 합침 (차량이 동시에 두 교차로를 점유하지 않도록)
        bool merged = true;
        while (merged)
        {
            merged = false;

            for (int a = 0; a < overlaps.Count && !merged; a++)
            {
                for (int b = a + 1; b < overlaps.Count; b++)
                {
                    if (!overlaps[a].Overlaps(overlaps[b]))
                        continue;

                    overlaps[a] = Rect.MinMaxRect(
                        Mathf.Min(overlaps[a].xMin, overlaps[b].xMin),
                        Mathf.Min(overlaps[a].yMin, overlaps[b].yMin),
                        Mathf.Max(overlaps[a].xMax, overlaps[b].xMax),
                        Mathf.Max(overlaps[a].yMax, overlaps[b].yMax));

                    overlaps.RemoveAt(b);
                    merged = true;
                    break;
                }
            }
        }

        foreach (Rect overlap in overlaps)
            zones.Add(new TrafficZone(overlap));

        foreach (TrafficRoute route in runtimeRoutes)
            route.BuildSpans(zones, 0.5f);
    }

    #region Bake

    [ContextMenu("Bake Routes From Road Tiles")]
    public void BakeRoutes()
    {
        Dictionary<Vector2Int, TileCell> cells = CollectTiles();

        if (cells.Count == 0)
        {
            Debug.LogWarning("[TrafficManager] 도로 타일을 찾지 못함 (tilePrefix / roadRoot 확인)");
            return;
        }

        int minX = int.MaxValue, minZ = int.MaxValue;
        int maxX = int.MinValue, maxZ = int.MinValue;

        foreach (Vector2Int key in cells.Keys)
        {
            minX = Mathf.Min(minX, key.x);
            minZ = Mathf.Min(minZ, key.y);
            maxX = Mathf.Max(maxX, key.x);
            maxZ = Mathf.Max(maxZ, key.y);
        }

        List<Candidate> candidates = new();

        // X축 방향 도로: 인접한 두 줄(z, z+1)이 모두 도로인 구간
        for (int z = minZ; z < maxZ; z++)
            ScanLine(cells, true, z, minX, maxX, candidates);

        // Z축 방향 도로: 인접한 두 줄(x, x+1)
        for (int x = minX; x < maxX; x++)
            ScanLine(cells, false, x, minZ, maxZ, candidates);

        RemoveDuplicates(candidates);

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Bake Traffic Routes");
#endif

        routes.Clear();

        foreach (Candidate c in candidates)
        {
            float from = c.from * cellSize + endMargin;
            float to = (c.to + 1) * cellSize - endMargin;

            if (to - from < laneOffset * 2f)
                continue;

            RouteLine line = new RouteLine
            {
                start = c.alongX ? new Vector3(from, c.height, c.line) : new Vector3(c.line, c.height, from),
                end = c.alongX ? new Vector3(to, c.height, c.line) : new Vector3(c.line, c.height, to),
            };

            routes.Add(line);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log($"[TrafficManager] 타일 {cells.Count}칸에서 경로 {routes.Count}개 생성");
    }

    private Dictionary<Vector2Int, TileCell> CollectTiles()
    {
        Dictionary<Vector2Int, TileCell> cells = new();

        Renderer[] renderers = roadRoot != null
            ? roadRoot.GetComponentsInChildren<Renderer>(true)
            : FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            Transform tile = FindTileRoot(renderer.transform);
            if (tile == null || ContainsAny(tile.name, ignoreKeywords))
                continue;

            Bounds bounds = renderer.bounds;
            Vector2Int key = new Vector2Int(
                Mathf.FloorToInt(bounds.center.x / cellSize),
                Mathf.FloorToInt(bounds.center.z / cellSize));

            cells.TryGetValue(key, out TileCell cell);

            if (!ContainsAny(tile.name, connectorKeywords))
            {
                // 차선 타일은 회전으로 도로 방향 판단
                Vector3 forward = tile.forward;
                if (Mathf.Abs(forward.x) > Mathf.Abs(forward.z))
                    cell.laneX = true;
                else
                    cell.laneZ = true;
            }

            cell.heightSum += bounds.max.y;
            cell.tileCount++;
            cells[key] = cell;
        }

        return cells;
    }

    private Transform FindTileRoot(Transform t)
    {
        while (t != null)
        {
            if (t.name.StartsWith(tilePrefix))
                return t;

            t = t.parent;
        }

        return null;
    }

    private static bool ContainsAny(string value, string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (!string.IsNullOrEmpty(keyword) && value.Contains(keyword))
                return true;
        }

        return false;
    }

    private void ScanLine(
        Dictionary<Vector2Int, TileCell> cells, bool alongX, int row,
        int min, int max, List<Candidate> candidates)
    {
        int runFrom = 0, runTo = 0, laneTiles = 0, gap = 0, heightCount = 0;
        float heightSum = 0f;
        bool inRun = false;

        for (int t = min; t <= max + gapTolerance + 1; t++)
        {
            Vector2Int a = alongX ? new Vector2Int(t, row) : new Vector2Int(row, t);
            Vector2Int b = alongX ? new Vector2Int(t, row + 1) : new Vector2Int(row + 1, t);

            if (cells.TryGetValue(a, out TileCell cellA) && cells.TryGetValue(b, out TileCell cellB))
            {
                if (!inRun)
                {
                    inRun = true;
                    runFrom = t;
                    laneTiles = 0;
                    heightSum = 0f;
                    heightCount = 0;
                }

                runTo = t;
                gap = 0;

                bool laneA = alongX ? cellA.laneX : cellA.laneZ;
                bool laneB = alongX ? cellB.laneX : cellB.laneZ;
                if (laneA && laneB)
                    laneTiles++;

                heightSum += cellA.heightSum + cellB.heightSum;
                heightCount += cellA.tileCount + cellB.tileCount;
                continue;
            }

            if (!inRun)
                continue;

            gap++;
            if (gap <= gapTolerance)
                continue;

            inRun = false;

            if (laneTiles >= minLaneTiles)
            {
                candidates.Add(new Candidate
                {
                    alongX = alongX,
                    line = (row + 1) * cellSize,
                    from = runFrom,
                    to = runTo,
                    laneTiles = laneTiles,
                    height = heightSum / Mathf.Max(1, heightCount),
                });
            }
        }
    }

    // 3칸 이상 넓은 도로에서 한 칸씩 어긋난 중복 경로 제거 (차선 타일이 많은 쪽 유지)
    private void RemoveDuplicates(List<Candidate> candidates)
    {
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < candidates.Count; j++)
            {
                if (i == j)
                    continue;

                Candidate a = candidates[i];
                Candidate b = candidates[j];

                bool parallel = a.alongX == b.alongX && Mathf.Abs(a.line - b.line) < cellSize * 2f - 0.01f;
                bool overlap = a.from <= b.to && b.from <= a.to;
                bool weaker = a.laneTiles < b.laneTiles || (a.laneTiles == b.laneTiles && i > j);

                if (parallel && overlap && weaker)
                {
                    candidates.RemoveAt(i);
                    break;
                }
            }
        }
    }

    #endregion

    private void OnDrawGizmos()
    {
        List<Vector3> loop = new();
        List<bool> turns = new();

        foreach (RouteLine line in routes)
        {
            if (!line.enabled)
                continue;

            TrafficRoute.BuildLoop(line.start, line.end, laneOffset, uTurnSegments, loop, turns);

            int count = loop.Count;
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Gizmos.color = turns[i] || turns[next] ? Color.yellow : Color.cyan;
                Gizmos.DrawLine(loop[i] + Vector3.up * 0.3f, loop[next] + Vector3.up * 0.3f);
            }
        }

        foreach (TrafficZone zone in zones)
        {
            Gizmos.color = zone.Occupied ? Color.red : Color.green;
            Vector3 center = new Vector3(zone.Area.center.x, 0.5f, zone.Area.center.y);
            Gizmos.DrawWireCube(center, new Vector3(zone.Area.width, 1f, zone.Area.height));
        }
    }
}
