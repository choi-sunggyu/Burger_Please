using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// layout 글자대로 타일 프리팹을 격자에 배치한다.
// 에디터: 인스펙터의 Build/Clear 버튼 또는 컴포넌트 ⋮ 메뉴 > Build Tiles
// 플레이 중: Game 뷰의 OnGUI 패널 (에디터와 Development Build에서만 표시)
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
        return Instantiate(prefab, transform);
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
    private static readonly string[] RotationLabels = { "0", "90", "180", "270" };
    private Rect windowRect = new Rect(10, 10, 340, 260);

    private void OnGUI()
    {
        if (!showRuntimeGui) return;
        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "Tile Grid");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("Layout");
        layout = GUILayout.TextArea(layout, GUILayout.MinHeight(100));

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Gap {gap:F2}", GUILayout.Width(70));
        gap = GUILayout.HorizontalSlider(gap, 0f, 10f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Size x{tileScale:F2}", GUILayout.Width(70));
        tileScale = GUILayout.HorizontalSlider(tileScale, 0.1f, 5f);
        GUILayout.EndHorizontal();

        GUILayout.Label("Rotation Y");
        for (int i = 0; i < tiles.Length; i++)
        {
            string prefabName = tiles[i].prefab != null ? tiles[i].prefab.name : "(없음)";
            int current = Mathf.RoundToInt(Mathf.Repeat(tiles[i].rotationY, 360f) / 90f) % 4;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{tiles[i].key} {prefabName}", GUILayout.Width(130));
            int selected = GUILayout.Toolbar(current, RotationLabels);
            GUILayout.EndHorizontal();

            // 클릭했을 때만 쓴다. Inspector에 입력한 45 같은 값이 저절로 바뀌지 않게 한다.
            if (selected != current) tiles[i].rotationY = selected * 90f;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Build")) Build();
        if (GUILayout.Button("Clear")) ClearTiles();
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 20)); // 제목 표시줄을 잡고 이동
    }
#endif
}