using UnityEngine;

// 포장
// PackingCounter에 큐브가 대기 중이면(포장 가능하면) PackingPoint에 서 있도록 함
// 실제 포장은 PackingPoint의 Packing이 "계산원이 서 있음"을 감지해서 처리
//
// [붙이는 곳] Packing이 붙은 오브젝트(PackingPoint) → packing, standPoint 자동
public class PackJob : JobBase
{
    [SerializeField] Transform standPoint;     // 직원이 설 위치 (비우면 PackingPoint)
    [SerializeField] Packing packing;          // 비우면 같은 오브젝트에서 찾음

    public override JobRole Role => JobRole.Pack;

    public override bool IsStillNeeded => packing != null && packing.CanPack;

    public override Transform GetTarget(Employee worker) => IsStillNeeded ? standPoint : null;

    void Awake()
    {
        if (packing == null) packing = GetComponent<Packing>();
        if (standPoint == null && packing != null) standPoint = packing.transform;

        if (packing == null)
            Debug.LogError($"[PackJob] {name}: Packing이 없습니다. PackingPoint에 붙이거나 직접 연결하세요.", this);
    }
}
