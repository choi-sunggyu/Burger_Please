using UnityEngine;

public class SellingAction : MonoBehaviour
{
    // 큐브 판매
    // Counter의 CubeZone에 쌓인 큐브를 손님에게 전달
    // Counter의 CubeZone에 쌓인 큐브 => Counter의 CubeZone에 있는 CubeSpot.cs의 TakeTop 메서드 사용
    // 손님에게 전달 => Customer.cs의 ReceiveCube 메서드 사용

    [SerializeField] CounterQueue queue;
    [SerializeField] CubeSpot cubeSpot;          // 카운터 위 큐브 쌓는 곳
    [SerializeField] float giveInterval = 0.2f;  // 하나씩 건네는 간격
    
    [Header("계산대")]
    [SerializeField] Transform cashierZone;   // 계산대 구역. 플레이어가 이 안에 있어야 판매 가능
    [SerializeField] bool requireCashier = false;   // 켜면 플레이어가 구역 안에 있어야 판매


    float timer;
    bool cashierPresent;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < giveInterval) return;

        if (requireCashier && !cashierPresent) return;

        Customer customer = queue.Front;
        if (customer == null || !customer.CanReceive) return;   // 걸어오는 중이거나 주문이 끝났으면 X

        Stuff cube = cubeSpot.TakeTop();
        if (cube == null) return;                                // 카운터에 큐브가 없음

        customer.ReceiveCube(cube);
        timer = 0f;
    }

    // 계산대 구역: Counter 자식에 Is Trigger 콜라이더를 두고 이 스크립트와 같은 오브젝트에 배치
    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) cashierPresent = true; }
    void OnTriggerExit(Collider other)  { if (other.CompareTag("Player")) cashierPresent = false; }
}