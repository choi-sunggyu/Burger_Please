using System.Collections.Generic;
using UnityEngine;

// 거리 확인용 등록부: 켜져 있는 대상만 담아 둔다 (트리거 충돌 대신 이 목록을 돌며 거리로 판단)
// 대상 쪽에서 OnEnable에 Add, OnDisable에 Remove
public static class Registry<T> where T : class
{
    private static readonly List<T> items = new List<T>();
    public static IReadOnlyList<T> Items => items;

    public static void Add(T item) { if (!items.Contains(item)) items.Add(item); }
    public static void Remove(T item) => items.Remove(item);
}

// 수평(XZ) 거리 계산
// 대상에 켜진 Collider가 있으면 그 표면까지 잰다 → 예전 트리거 영역 크기를 그대로 '범위 모양'으로 씀
public static class Proximity
{
    public static float Distance(Vector3 from, Component target)
    {
        Vector3 to = ClosestPoint(target, from);
        float dx = to.x - from.x;
        float dz = to.z - from.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static bool InRange(Vector3 from, Component target, float range) => Distance(from, target) <= range;

    private static Vector3 ClosestPoint(Component target, Vector3 from)
    {
        if (!target.TryGetComponent(out Collider col) || !col.enabled) return target.transform.position;
        if (col is MeshCollider mesh && !mesh.convex) return col.bounds.ClosestPoint(from); // 오목 메쉬는 ClosestPoint 불가
        return col.ClosestPoint(from);
    }
}
