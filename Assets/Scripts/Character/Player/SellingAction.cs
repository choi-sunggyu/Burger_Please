using UnityEngine;

public class SellingAction : MonoBehaviour
{
    // 큐브 판매
    // Counter의 CubeZone에 쌓인 큐브를 손님에게 전달
    // Counter의 CubeZone에 쌓인 큐브 => Counter의 CubeZone에 있는 CubeSpot.cs의 TakeTop 메서드 사용
    // 손님에게 전달 => Customer.cs의 ReceiveCube 메서드 사용

    [Header("결제")]
    [SerializeField] GameObject moneyPrefab;
    [SerializeField] Transform moneyPoint;      // 카운터 옆 돈 쌓는 곳 (Scale 1,1,1)
    [SerializeField] float moneyStackHeight = 0.15f;

    [SerializeField] CounterQueue queue;
    [SerializeField] CubeSpot cubeSpot;          // 카운터 위 큐브 쌓는 곳
    [SerializeField] float giveInterval = 0.2f;  // 하나씩 건네는 간격
    
    int staffInside;   // 구역 안에 있는 계산원 콜라이더 수

    bool IsStaff(Collider other) => other.GetComponentInParent<Carry>() != null;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < giveInterval) return;

        if (staffInside == 0) return;
        Debug.Log("여기까지 통과");
        Customer customer = queue.Front;
        if (customer == null || !customer.CanReceive) return;   // 걸어오는 중이거나 주문이 끝났으면 X

        Stuff cube = cubeSpot.TakeTop();
        if (cube == null) return;                                // 카운터에 큐브가 없음

        customer.ReceiveCube(cube);

        if (customer.IsOrderComplete)
        {
            for (int i = 0; i < customer.RequiredCount; i++)   // 큐브 개수만큼 돈 지급
            {
                Vector3 pos = moneyPoint.position + Vector3.up * moneyStackHeight * moneyPoint.childCount;
                Instantiate(moneyPrefab, pos, Quaternion.identity, moneyPoint);
            }
        }

        timer = 0f;
    }

    // 계산대 구역: Counter 자식에 Is Trigger 콜라이더를 두고 이 스크립트와 같은 오브젝트에 배치
    void OnTriggerEnter(Collider other) { if (IsStaff(other)) staffInside++; }
    void OnTriggerExit(Collider other)  { if (IsStaff(other)) staffInside = Mathf.Max(0, staffInside - 1); }
}