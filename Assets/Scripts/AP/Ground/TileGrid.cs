using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// layout 글자대로 타일 프리팹을 격자에 배치한다.
// 에디터: 인스펙터의 Build/Clear 버튼 또는 컴포넌트 ⋮ 메뉴 > Build Tiles
// 플레이 중: 화면 오른쪽 위 OnGUI 패널 (에디터와 Development Build에서만 표시)
// 칸 크기 = 사용한 타일 중 가장 큰 바닥(Concrete_Floor) 크기, 각 타일은 바닥 중심을 칸 중심에 맞춘다.
public class TileGrid : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public string key;       // 한 글자 (예: D)
        public GameObject prefab;
        public float rotationY;  // 배치할 때 더할 Y축 회전 (예: DownTile = 180)
    }

    public Entry[] tiles;                       // D=DownTile, U=UpTile, S=StackTile, O=OfficeTile
    [TextArea(3, 12)] public string layout = "DU\nSO"; // 한 줄 = 한 행 (위에서부터 +z → -z), '.' = 빈칸
    public float gap = 0f;
    [Min(0.01f)] public float tileScale = 1f;   // 모든 타일에 곱할 크기 배율
    public bool showRuntimeGui = true;          // 플레이 중 OnGUI 패널 표시 여부

    [ContextMenu("Build Tiles")]
    public void Build()
    {
#if UNITY_EDITOR
        // 자식을 지우기 전에 검사해서, 실패해도 기존 타일이 남게 한다.
        if (!ValidateTiles()) return;
#endif
        ClearTiles();

        string[] rows = layout.Replace("\r", "").Split('\n');
        var placed = new System.Collections.Generic.List<(Transform tile, Bounds floor, int row, int col)>();
        Vector2 cell = Vector2.zero;

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c];
                if (ch == '.' || ch == ' ') continue;

                Entry entry = Find(ch);
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"[TileGrid] '{ch}'에 해당하는 타일이 없음 ({r}행 {c}열)", this);
                    continue;
                }

                GameObject tile = CreateTile(entry.prefab);
                if (tile == null)
                {
                    Debug.LogError($"[TileGrid] '{ch}' 생성 실패 ({r}행 {c}열)", entry.prefab);
                    continue;
                }

                // 회전과 크기는 바닥 범위를 바꾸므로 FloorBounds보다 먼저 적용한다.
                tile.transform.localRotation = Quaternion.Euler(0f, entry.rotationY, 0f) * tile.transform.localRotation;
                tile.transform.localScale *= tileScale;

                Bounds floor = FloorBounds(tile);
                cell = Vector2.Max(cell, new Vector2(floor.size.x, floor.size.z));
                placed.Add((tile.transform, floor, r, c));
            }
        }

        foreach (var p in placed)
        {
            Vector3 target = transform.position + new Vector3(p.col * (cell.x + gap), 0f, -p.row * (cell.y + gap));
            Vector3 shift = target - p.floor.center;
            p.tile.position += new Vector3(shift.x, 0f, shift.z); // 높이는 프리팹 값 유지
        }

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(gameObject);
#endif
        Debug.Log($"[TileGrid] 타일 {placed.Count}개 배치, 칸 크기 {cell}", this);

        if (Application.isPlaying) ResetPlayer();
    }

    // 플레이 시작 시 플레이어 위치를 기억해 두고, 플레이 중 Build하면 그 위치로 되돌린다.
    private Transform player;
    private Vector3 playerStartPos;
    private Quaternion playerStartRot;

    private void Start()
    {
        var p = FindFirstObjectByType<AstronautPlayer.AstronautPlayer>();
        if (p == null) return;
        player = p.transform;
        playerStartPos = player.position;
        playerStartRot = player.rotation;
    }

    private void ResetPlayer()
    {
        if (player == null) return;
        // CharacterController가 켜져 있으면 위치를 바꿔도 다음 Move에서 되돌려지므로 잠시 끈다.
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.SetPositionAndRotation(playerStartPos, playerStartRot);
        if (controller != null) controller.enabled = true;
    }

    // 이 오브젝트의 자식을 전부 지운다. 에디터 모드에서는 Ctrl+Z로 되돌릴 수 있음.
    [ContextMenu("Clear Tiles")]
    public void ClearTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(child);
                continue;
            }
#endif
            Destroy(child);
        }
    }

    private GameObject CreateTile(GameObject prefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var created = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            if (created != null) Undo.RegisterCreatedObjectUndo(created, "Build Tiles");
            return created;
        }
#endif
        var tile = Instantiate(prefab, transform);
        VLB.BakedMaterialRemap.Apply(tile);
        return tile;
    }

    // 못 찾으면 prefab이 null인 기본값을 돌려준다.
    private Entry Find(char ch)
    {
        foreach (Entry e in tiles)
            if (!string.IsNullOrEmpty(e.key) && e.key[0] == ch) return e;
        return default;
    }

    // 타일 안의 바닥 메시 범위 (프리팹 피벗이 바닥 중심과 달라서 이 값으로 정렬함)
    private static Bounds FloorBounds(GameObject tile)
    {
        foreach (Transform t in tile.GetComponentsInChildren<Transform>())
            if (t.name.Contains("Floor")) return BoundsUtil.Of(t.gameObject);
        return BoundsUtil.Of(tile);
    }

#if UNITY_EDITOR
    private bool ValidateTiles()
    {
        bool valid = true;
        foreach (Entry e in tiles)
        {
            if (e.prefab != null && !PrefabUtility.IsPartOfPrefabAsset(e.prefab))
            {
                Debug.LogError($"[TileGrid] '{e.key}' 슬롯의 {e.prefab.name}은(는) 프리팹 에셋이 아님", this);
                valid = false;
            }
        }
        return valid;
    }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 화면 오른쪽 위 고정 패널. 모바일 터치를 위해 dpi에 맞춰 키우고, 슬라이더 대신 -/+ 버튼을 쓴다.
    private static readonly string[] RotationLabels = { "0", "90", "180", "270" };
    private const float PanelWidth = 300f, Margin = 8f, RowHeight = 36f;
    private bool guiOpen;
    private bool autoBuild = true;  // 간격·크기·회전을 바꾸면 바로 다시 배치
    private Rect windowRect;

    private void OnGUI()
    {
        if (!showRuntimeGui) return;

        // 160dpi = 1배 (안드로이드 dp 기준). dpi를 모르면 1배.
        float scale = Mathf.Max(1f, Screen.dpi > 0f ? Screen.dpi / 160f : 1f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float right = Screen.width / scale - Margin;

        if (!guiOpen)
        {
            if (GUI.Button(new Rect(right - 120f, Margin, 120f, RowHeight + 8f), "Tile Grid ▼")) guiOpen = true;
            return;
        }

        windowRect = new Rect(right - PanelWidth, Margin, PanelWidth, windowRect.height);
        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "Tile Grid");
    }

    private void DrawWindow(int id)
    {
        GUILayoutOption h = GUILayout.Height(RowHeight);

        GUILayout.BeginHorizontal();
        autoBuild = GUILayout.Toggle(autoBuild, " 자동 Build", h);
        if (GUILayout.Button("닫기 ▲", h, GUILayout.Width(90f))) guiOpen = false;
        GUILayout.EndHorizontal();

        GUILayout.Label("Layout ('.' = 빈칸)");
        layout = GUILayout.TextArea(layout, GUILayout.MinHeight(RowHeight * 3f));

        bool changed = false;
        changed |= Stepper("Gap", ref gap, 0.1f, 0f, 10f, h);
        changed |= Stepper("Size x", ref tileScale, 0.1f, 0.1f, 5f, h);

        GUILayout.Label("Rotation Y");
        for (int i = 0; i < tiles.Length; i++)
        {
            string prefabName = tiles[i].prefab != null ? tiles[i].prefab.name : "(없음)";
            int current = Mathf.RoundToInt(Mathf.Repeat(tiles[i].rotationY, 360f) / 90f) % 4;

            GUILayout.Label($"{tiles[i].key}  {prefabName}");
            int selected = GUILayout.Toolbar(current, RotationLabels, h);

            // 눌렀을 때만 쓴다. Inspector에 입력한 45 같은 값이 저절로 바뀌지 않게 한다.
            if (selected != current)
            {
                tiles[i].rotationY = selected * 90f;
                changed = true;
            }
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Build", GUILayout.Height(RowHeight + 8f))) Build();
        if (GUILayout.Button("Clear", GUILayout.Height(RowHeight + 8f))) ClearTiles();
        GUILayout.EndHorizontal();

        // Layout 글자 입력은 한 글자마다 다시 짓지 않도록 Build 버튼으로만 반영
        if (changed && autoBuild) Build();
    }

    // [라벨 값] [-] [+] 한 줄. 값이 바뀌면 true
    private static bool Stepper(string label, ref float value, float step, float min, float max, GUILayoutOption h)
    {
        float before = value;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label} {value:F1}", h, GUILayout.Width(110f));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("-", h, GUILayout.Width(48f))) value -= step;
        if (GUILayout.Button("+", h, GUILayout.Width(48f))) value += step;
        GUILayout.EndHorizontal();
        value = Mathf.Clamp(Mathf.Round(value / step) * step, min, max);
        return !Mathf.Approximately(before, value);
    }
#endif
}