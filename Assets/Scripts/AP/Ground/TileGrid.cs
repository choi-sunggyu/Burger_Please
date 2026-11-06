using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// layout 글자대로 타일 프리팹을 격자에 배치한다. 인스펙터 컴포넌트 ⋮ 메뉴 > Build Tiles
// 칸 크기 = 사용한 타일 중 가장 큰 바닥(Concrete_Floor) 크기, 각 타일은 바닥 중심을 칸 중심에 맞춘다.
public class TileGrid : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public string key;       // 한 글자 (예: D)
        public GameObject prefab;
    }

    public Entry[] tiles;                       // D=DownTile, U=UpTile, S=StackTile, O=OfficeTile
    [TextArea(3, 12)] public string layout = "DU\nSO"; // 한 줄 = 한 행 (위에서부터 +z → -z), '.' = 빈칸
    public float gap = 0f;

#if UNITY_EDITOR
    [ContextMenu("Build Tiles")]
    private void Build()
    {
        // 이 오브젝트의 자식은 전부 지우고 다시 만든다 (Ctrl+Z로 되돌릴 수 있음)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);

        string[] rows = layout.Replace("\r", "").Split('\n');
        var placed = new System.Collections.Generic.List<(Transform tile, Bounds floor, int row, int col)>();
        Vector2 cell = Vector2.zero;

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c];
                if (ch == '.' || ch == ' ') continue;

                GameObject prefab = Find(ch);
                if (prefab == null)
                {
                    Debug.LogWarning($"[TileGrid] '{ch}'에 해당하는 타일이 없음 ({r}행 {c}열)", this);
                    continue;
                }

                var tile = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                Undo.RegisterCreatedObjectUndo(tile, "Build Tiles");
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

        EditorUtility.SetDirty(gameObject);
        Debug.Log($"[TileGrid] 타일 {placed.Count}개 배치, 칸 크기 {cell}", this);
    }

    private GameObject Find(char ch)
    {
        foreach (Entry e in tiles)
            if (!string.IsNullOrEmpty(e.key) && e.key[0] == ch) return e.prefab;
        return null;
    }

    // 타일 안의 바닥 메시 범위 (프리팹 피벗이 바닥 중심과 달라서 이 값으로 정렬함)
    private static Bounds FloorBounds(GameObject tile)
    {
        foreach (Transform t in tile.GetComponentsInChildren<Transform>())
            if (t.name.Contains("Floor")) return BoundsUtil.Of(t.gameObject);
        return BoundsUtil.Of(tile);
    }
#endif
}
