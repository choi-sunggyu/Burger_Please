using System.Collections.Generic;
using UnityEngine;

// 카트 짐칸
// - 플레이어가 카트를 미는 동안 GrabAction 이 주운 물건을 손 대신 여기에 싣는다
// - 카트를 놓아도 실린 물건은 카트에 그대로 남는다 (다시 잡으면 이어서 사용)
// - 칸 배치: columns(좌우) x rows(앞뒤) 를 한 층으로, 다 차면 위로 한 층 더 쌓는다
[RequireComponent(typeof(CartController))]
public class CartCargo : MonoBehaviour
{
    [Header("짐칸 위치")]
    [Tooltip("짐칸 바닥의 중앙. 비워 두면 카트 콜라이더 윗면 중앙을 임시로 사용")]
    [SerializeField] private Transform cargoPoint;
    [Tooltip("짐칸 크기 (x = 좌우 너비, y = 앞뒤 길이), 월드 단위")]
    [SerializeField] private Vector2 bedSize = new Vector2(1f, 1.6f);
    [Tooltip("한 층의 높이 (물건 하나 높이), 월드 단위")]
    [SerializeField] private float layerHeight = 0.3f;

    [Header("적재")]
    [SerializeField] private int capacity = 30;
    [SerializeField, Min(1)] private int columns = 3; // 좌우 칸 수
    [SerializeField, Min(1)] private int rows = 5;    // 앞뒤 칸 수  → 3 x 5 = 15개 x 2층 = 30개
    [SerializeField] private StuffType[] acceptTypes = { StuffType.Cube };

    private readonly List<Stuff> items = new List<Stuff>();
    private CartController cart;

    public int Capacity => capacity;
    public int Count { get { Cleanup(); return items.Count; } }
    public bool IsEmpty => Count == 0;
    public bool IsFull => Count >= capacity;

    public StuffType HeldType
    {
        get
        {
            Cleanup();
            if (items.Count == 0)
                throw new System.InvalidOperationException("카트에 실린 물건이 없습니다.");
            return items[0].Type;
        }
    }

    private void Awake() => cart = GetComponent<CartController>();

    // 이 물건을 실을 수 있나 (용량, 허용 종류, 같은 종류만)
    public bool CanAccept(Stuff stuff)
    {
        if (stuff == null || items.Contains(stuff)) return false;
        if (System.Array.IndexOf(acceptTypes, stuff.Type) < 0) return false;
        if (Count >= capacity) return false;
        if (items.Count > 0 && stuff.Type != items[0].Type) return false;
        return true;
    }

    public bool TryAdd(Stuff stuff)
    {
        if (!CanAccept(stuff)) return false;

        if (stuff.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true; // 물리 영향 끄기
        if (stuff.TryGetComponent(out Collider col)) col.enabled = false;   // 충돌 끄기 (카트와 부딪히지 않게)

        // 카트 루트는 FBX 때문에 회전/스케일이 들어가 있으므로 월드 기준을 유지한 채 붙이고, 위치는 월드 좌표로 지정
        stuff.transform.SetParent(transform, true);
        GetSlot(items.Count, out Vector3 position, out Quaternion rotation);
        stuff.transform.SetPositionAndRotation(position, rotation);

        items.Add(stuff);
        return true;
    }

    // PutAction / GrabAction 이 맨 위(마지막에 실은) 물건을 꺼낼 때
    public Stuff TakeTop()
    {
        Cleanup();
        if (items.Count == 0) return null;

        int last = items.Count - 1;
        Stuff top = items[last];
        items.RemoveAt(last);
        top.transform.SetParent(null);
        return top;
    }

    // index 번째 칸의 월드 위치/회전
    private void GetSlot(int index, out Vector3 position, out Quaternion rotation)
    {
        int perLayer = columns * rows;
        int layer = index / perLayer;
        int i = index % perLayer;
        int row = i / columns;
        int col = i % columns;

        float cellW = bedSize.x / columns;
        float cellL = bedSize.y / rows;
        Vector3 local = new Vector3(
            (col + 0.5f) * cellW - bedSize.x * 0.5f,
            (layer + 0.5f) * layerHeight,
            (row + 0.5f) * cellL - bedSize.y * 0.5f);

        rotation = Quaternion.LookRotation(GetForward(), Vector3.up);
        position = GetOrigin() + rotation * local;
    }

    private Vector3 GetForward()
    {
        if (cart == null) cart = GetComponent<CartController>();
        Vector3 f = cart != null ? cart.Forward : transform.forward;
        return f.sqrMagnitude > 0.0001f ? f : Vector3.forward;
    }

    private Vector3 GetOrigin()
    {
        if (cargoPoint != null) return cargoPoint.position;

        // 임시 위치: 트리거가 아닌 카트 콜라이더의 윗면 중앙
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger || !c.enabled) continue;
            Bounds b = c.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
        return transform.position;
    }

    private void Cleanup() => items.RemoveAll(s => s == null);

    // 씬 뷰에서 카트를 선택하면 짐칸 영역(노란색)과 각 칸(하늘색)이 보인다 → cargoPoint / bedSize 맞출 때 사용
    private void OnDrawGizmosSelected()
    {
        Quaternion rot = Quaternion.LookRotation(GetForward(), Vector3.up);
        Vector3 origin = GetOrigin();
        int layers = Mathf.CeilToInt(capacity / (float)(columns * rows));

        Gizmos.matrix = Matrix4x4.TRS(origin, rot, Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(0f, layers * layerHeight * 0.5f, 0f),
            new Vector3(bedSize.x, layers * layerHeight, bedSize.y));
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Vector3 cell = new Vector3(bedSize.x / columns, layerHeight, bedSize.y / rows) * 0.9f;
        for (int i = 0; i < capacity; i++)
        {
            GetSlot(i, out Vector3 p, out Quaternion r);
            Gizmos.matrix = Matrix4x4.TRS(p, r, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, cell);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
}
