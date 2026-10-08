using UnityEngine;
using System.Collections.Generic;

public class CounterQueue : MonoBehaviour
{
    [SerializeField] Transform frontPoint;   // 맨 앞 자리. 파란 화살표(forward)가 카운터를 향하게 회전
    [SerializeField] float spacing = 1f;     // 손님 사이 간격
    [SerializeField] int maxLength = 5;      // 최대 줄 길이
    
    // 처음에는 Queue로 하려고 했는데 List로 바꿈. 순서 접근이 쉬워서
    public List<Customer> customers = new List<Customer>();
    public bool IsFull => customers.Count >= maxLength;
    public Customer Front => customers.Count > 0 ? customers[0] : null;

    // 줄 선 손님 목록 관리, 각자에게 서 있을 위치 선정
    // 기능 : Enqueue, Dequeue, Front, 앞 사람 빠지면 나머지 당기기
    public void Enqueue(Customer customer)
    {
        // CustomerSpawner에서 손님이 스폰되면 CounterQueue의 큐에 추가
        if (customer == null || IsFull || customers.Contains(customer)) return;

        customers.Add(customer);
        customer.SetQueueSlot(GetSlotPosition(customers.Count - 1), customers.Count == 1);
    }

    public void Remove(Customer customer)
    {
        // 손님이 Destroy되면 CounterQueue의 큐에서 제거
        if (!customers.Remove(customer)) return;   // 줄에 없던 손님이면 아무것도 안 함
        RefreshSlots();
    }

    // 모든 손님에게 현재 순번에 맞는 자리를 다시 알려줌
    void RefreshSlots()
    {
        for (int i = 0; i < customers.Count; i++)
            customers[i].SetQueueSlot(GetSlotPosition(i), i == 0);
    }

    // i번째 자리 = 맨 앞에서 "뒤쪽"으로 spacing * i 만큼 떨어진 곳
    Vector3 GetSlotPosition(int index)
    {
        return frontPoint.position + frontPoint.forward * spacing * index;
    }

    // Scene 뷰에서 줄 위치를 미리 보기 *** 이게 쩔어 ***
    void OnDrawGizmos()
    {
        if (frontPoint == null) return;
        for (int i = 0; i < maxLength; i++)
        {
            Gizmos.color = i == 0 ? Color.green : Color.yellow;   // 맨 앞은 초록색
            Gizmos.DrawWireSphere(GetSlotPosition(i), 0.3f);
        }
    }

}
