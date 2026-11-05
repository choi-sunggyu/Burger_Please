using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 적재함: 지정한 선반들의 칸을 계산해 박스를 넣고 꺼낸다 (먼저 들어온 박스부터 나감)
public class ShelfStorage : MonoBehaviour
{
    public class Slot
    {
        public Vector3 Position;
        public Bounds ShelfBounds;
        public GameObject Box;
    }

    [SerializeField] private Transform[] shelves;
    [SerializeField] private int columns = 4;                                    // 선반 하나의 가로 칸 수
    [SerializeField] private float[] levelOffsets = { 28.5f, 41.5f, 53f };       // 선반 맨 아래에서 각 단에 놓인 박스 중심까지 높이 (맨 아래 단은 바닥에 묻혀 제외)
    [SerializeField] private float aisleGap = 12f;                               // 알바가 선반에서 떨어져 서는 거리
    [SerializeField] private Transform obstacleRoot;                             // 자식 선반마다 NavMesh 장애물을 만들어 알바가 돌아가게 함
    [SerializeField] private WorldLabel label;

    private readonly List<Slot> emptySlots = new List<Slot>();
    private readonly Queue<Slot> filledSlots = new Queue<Slot>();
    private int capacity;
    private int stored;

    public bool HasEmptySlot => emptySlots.Count > 0;
    public bool HasBox => filledSlots.Count > 0;
    public int StoredCount => stored;

    private void Awake()
    {
        foreach (Transform shelf in shelves)
            AddSlots(shelf);

        capacity = emptySlots.Count;
        UpdateLabel();

        if (obstacleRoot == null) return;
        foreach (Transform shelf in obstacleRoot)
            AddObstacle(shelf);
    }

    private void AddSlots(Transform shelf)
    {
        Bounds bounds = BoundsUtil.Of(shelf.gameObject);
        bool alongX = bounds.size.x > bounds.size.z;
        Vector3 axis = alongX ? Vector3.right : Vector3.forward;
        float length = alongX ? bounds.size.x : bounds.size.z;

        foreach (float offset in levelOffsets)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = bounds.center + axis * ((col + 0.5f) / columns - 0.5f) * length;
                pos.y = bounds.min.y + offset;
                emptySlots.Add(new Slot { Position = pos, ShelfBounds = bounds });
            }
        }
    }

    private void AddObstacle(Transform shelf)
    {
        Bounds bounds = BoundsUtil.Of(shelf.gameObject);

        var go = new GameObject($"{shelf.name}_Obstacle");
        go.transform.SetParent(transform, false);
        go.transform.position = bounds.center;

        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = bounds.size;
        obstacle.carving = true;
    }

    public Slot ReserveEmptySlot()
    {
        Slot slot = emptySlots[0];
        emptySlots.RemoveAt(0);
        return slot;
    }

    public Slot ReserveBox() => filledSlots.Dequeue();

    public void Store(Slot slot, GameObject box)
    {
        box.transform.SetParent(transform, true);
        box.transform.position = slot.Position;
        slot.Box = box;
        filledSlots.Enqueue(slot);
        stored++;
        UpdateLabel();
    }

    public GameObject Take(Slot slot)
    {
        GameObject box = slot.Box;
        slot.Box = null;
        emptySlots.Add(slot);
        stored--;
        UpdateLabel();
        return box;
    }

    // 선반 긴 쪽을 따라 난 통로 중, 알바와 가까운 쪽에 서는 지점
    public Vector3 GetStandPoint(Slot slot, Vector3 from)
    {
        Bounds bounds = slot.ShelfBounds;
        Vector3 point = slot.Position;
        point.y = bounds.min.y;

        if (bounds.size.x > bounds.size.z)
            point.z = bounds.center.z + Mathf.Sign(from.z - bounds.center.z) * (bounds.extents.z + aisleGap);
        else
            point.x = bounds.center.x + Mathf.Sign(from.x - bounds.center.x) * (bounds.extents.x + aisleGap);

        return point;
    }

    private void UpdateLabel()
    {
        if (label != null) label.SetText($"적재함 {stored}/{capacity}");
    }
}
