using System.Collections.Generic;
using UnityEngine;

// 큐브/큐브팩 운반
// - 맵에 있는 큐브, 큐브팩 확인 → 해당 위치로 이동 (줍기는 Carry/GrabAction이 자동)
// - 더 이상 없거나 손이 꽉 차면 → 받을 수 있는 CubeSpot 중 적재량이 가장 적은 곳으로 이동 (내려놓기는 PutAction이 자동)
// - 다시 큐브가 있는지 확인하고 반복
// - 큐브는 CubeSpot(acceptType=Cube)으로, 큐브팩은 CubeSpot(acceptType=CubePack)으로
//
// [붙이는 곳]
//  · CubeSpawn이 붙은 오브젝트 → 큐브 운반 (startPoint, itemType 자동)
//  · Packing이 붙은 오브젝트(PackingPoint) → 완성된 큐브팩 운반 (startPoint, itemType 자동)
//  endPoints를 비워두면 itemType을 받는 CubeSpot을 씬에서 모두 찾아 후보로 씀
public class DeliverCubeJob : HaulJob
{
    [SerializeField] Transform startPoint;                         // 큐브/큐브팩이 '자식'으로 쌓이는 곳
    [SerializeField] StuffType itemType = StuffType.Cube;           // 옮길 종류 (자동 설정 시 덮어씀)
    [SerializeField] List<CubeSpot> endPoints = new List<CubeSpot>(); // 놓을 곳 후보

    public override JobRole Role => JobRole.Deliver;
    protected override Transform SourcePile => startPoint;
    protected override StuffType ItemType => itemType;

    void Awake()
    {
        if (startPoint == null)
        {
            if (TryGetComponent(out CubeSpawn spawn))
            {
                startPoint = spawn.SpawnPoint;
                itemType = StuffType.Cube;
            }
            else if (TryGetComponent(out Packing packing))
            {
                startPoint = packing.PackTable;
                itemType = StuffType.CubePack;
            }
        }

        if (endPoints.Count == 0)
        {
            // 아직 해금되지 않은(비활성) 구역의 CubeSpot도 후보에 넣어두고, 실제로는 활성일 때만 사용
            foreach (CubeSpot spot in FindObjectsByType<CubeSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (spot.AcceptType == itemType) endPoints.Add(spot);
        }

        if (startPoint == null)
            Debug.LogError($"[DeliverCubeJob] {name}: startPoint가 없습니다. CubeSpawn/Packing과 같은 오브젝트에 붙이거나 직접 연결하세요.", this);
        if (endPoints.Count == 0)
            Debug.LogWarning($"[DeliverCubeJob] {name}: {itemType}을(를) 받는 CubeSpot이 없습니다.", this);
    }

    // 받을 수 있는 곳 중 가장 적게 쌓인 곳
    protected override Transform GetDropTarget()
    {
        CubeSpot best = null;
        foreach (CubeSpot spot in endPoints)
        {
            if (spot == null || !spot.isActiveAndEnabled) continue;
            if (!spot.CanReceive(itemType)) continue;
            if (best == null || spot.Count < best.Count) best = spot;
        }
        return best != null ? best.transform : null;
    }
}
