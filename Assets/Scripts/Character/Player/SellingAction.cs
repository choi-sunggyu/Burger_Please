using UnityEngine;
using UnityEngine.Serialization;

// SellingPoint(Is Trigger)에 부착. 매장 카운터 / 드라이브 스루 창구 공용
public class SellingAction : MonoBehaviour
{
    [Header("판매")]
    [FormerlySerializedAs("queue")]
    [SerializeField] MonoBehaviour queueSource;   // CounterQueue 또는 DriveThruLane
    [FormerlySerializedAs("cubeSpot")]
    [SerializeField] CubeSpot itemSpot;           // 판매할 물건이 쌓인 곳
    [SerializeField] float giveInterval = 0.2f;

    [Header("결제")]
    [SerializeField] GameObject moneyPrefab;
    [SerializeField] Transform moneyPoint;        // Scale 1,1,1
    [SerializeField] float moneyStackHeight = 0.15f;

    IBuyerQueue queue;
    int staffInside;
    float timer;

    void Awake()
    {
        queue = queueSource as IBuyerQueue;
        if (queue == null)
            Debug.LogError($"[SellingAction] {name}: queueSource에 CounterQueue나 DriveThruLane을 넣어주세요.", this);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < giveInterval) return;
        if (queue == null || staffInside == 0) return;

        IBuyer buyer = queue.Front;
        if (buyer == null || !buyer.CanReceive) return;   // 오는 중이거나 주문이 끝났으면 X

        Stuff item = itemSpot.TakeTop();
        if (item == null) return;                          // 선반이 비어 있음

        buyer.ReceiveItem(item);
        if (buyer.IsOrderComplete) Pay(buyer.RequiredCount);

        timer = 0f;
    }

    void Pay(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = moneyPoint.position + Vector3.up * moneyStackHeight * moneyPoint.childCount;
            Instantiate(moneyPrefab, pos, Quaternion.identity, moneyPoint);
        }
    }

    bool IsStaff(Collider other) => other.GetComponentInParent<Carry>() != null;
    void OnTriggerEnter(Collider other) { if (IsStaff(other)) staffInside++; }
    void OnTriggerExit(Collider other)  { if (IsStaff(other)) staffInside = Mathf.Max(0, staffInside - 1); }
}