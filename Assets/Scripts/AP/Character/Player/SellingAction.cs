using UnityEngine;
using UnityEngine.Serialization;

// SellingPoint에 부착. 매장 카운터 / 드라이브 스루 창구 공용
// 운반자(플레이어/직원)가 이 오브젝트(콜라이더가 있으면 그 영역) 근처에 있으면 판매 (트리거 충돌 대신 거리 확인)
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

    IBuyerQueue queue;
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
        if (queue == null || !HasStaff()) return;

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
        MoneyPile pile = MoneyPile.On(moneyPoint);
        for (int i = 0; i < count; i++) pile.Add(moneyPrefab);
    }

    // 운반자 중 누구라도 감지 반경 안에 있나
    bool HasStaff()
    {
        foreach (Carry carry in Registry<Carry>.Items)
            if (carry.IsNear(this)) return true;
        return false;
    }
}