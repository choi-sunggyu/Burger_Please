using UnityEngine;

// 계산대(SellingPoint)에 서 있는 일
public class SellJob : JobBase
{
    [SerializeField] Transform standPoint;     // 직원이 설 위치 (SellingPoint)
    [SerializeField] MonoBehaviour queueSource; // CounterQueue 또는 DriveThruLane

    IBuyerQueue queue;

    public override JobRole Role => JobRole.Sell;

    // 줄에 구매자가 있으면 필요
    public override bool IsStillNeeded => queue != null && queue.Front != null;

    // 한 단계짜리 일: 계산대로 가서, 일이 필요 없어질 때까지 서 있기
    public override Transform GetTarget(Employee worker) => IsStillNeeded ? standPoint : null;

    void Awake() => queue = queueSource as IBuyerQueue;
}