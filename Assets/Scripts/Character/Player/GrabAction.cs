using System.Collections.Generic;
using UnityEngine;

public class GrabAction : MonoBehaviour
{
    [SerializeField] private Carry carry;
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float stackHeight = 0.3f; // 물건 하나 높이

    private readonly List<Stuff> holding = new List<Stuff>();

    public int Count => holding.Count;
    public bool IsEmpty => holding.Count == 0;
    public StuffType HeldType => holding[0].Type; // IsEmpty 일 때는 호출하지 말 것

    private void OnEnable()
    {
        carry.OnStuffEntered += Grab;
    }

    private void OnDisable()
    {
        carry.OnStuffEntered -= Grab;
    }

    private void Grab(Stuff stuff)
    {
        // 돈은 손에 들지 않고 바로 소지금으로 (같은 종류 규칙의 예외)
        if (stuff is Money money)
        {
            CollectMoney(money);
            return;
        }

        if (!CanHold(stuff)) return; // 용량 초과 혹은 다른 물건을 들고 있을 경우 무시

        if (stuff.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true; // 물리 영향 끄기
        if (stuff.TryGetComponent(out Collider col)) col.enabled = false; // 충돌 끄기

        // stuff의 위치 이동 후에 해당 위치에 고정해서 캐릭터와 함께 움직이도록
        stuff.transform.SetParent(grabPoint, false); // 손 위치로 이동
        stuff.transform.localPosition = Vector3.up * stackHeight * holding.Count; // 쌓기
        stuff.transform.localRotation = Quaternion.identity; // 회전 초기화

        carry.Forget(stuff); // 주변 목록에서 제거 (GrabAction 이 들고 있으므로 Carry 가 더 이상 관리하지 않음)
        holding.Add(stuff);
        Debug.Log($"Grabbed {stuff.Type}! ({holding.Count}/{playerStat.Capacity})");
    }

    private bool CanHold(Stuff stuff)
    {
        if (holding.Contains(stuff)) return false;
        if (holding.Count >= playerStat.Capacity) return false;   // 용량 초과
        if (!IsEmpty && stuff.Type != HeldType) return false;      // 다른 종류는 내려놓기 전까지 불가
        return true;
    }

    private void CollectMoney(Money money)
    {
        playerStat.GetMoney(money.Value);
        carry.Forget(money);
        Destroy(money.gameObject);
        Debug.Log($"+{money.Value} (Total: {playerStat.Money})");
    }

    // PutAction 이 맨 위 물건을 하나 꺼내 갈 때 사용
    public Stuff TakeTop()
    {
        if (IsEmpty) return null;

        int last = holding.Count - 1;
        Stuff top = holding[last];
        holding.RemoveAt(last);
        top.transform.SetParent(null);

        if (IsEmpty) RetryNearby(); // 손이 비면, 범위 안에서 못 주웠던 물건을 다시 시도
        return top;
    }

    private void RetryNearby()
    {
        // Grab 안에서 carry 목록이 바뀌므로 복사본으로 순회
        foreach (Stuff s in new List<Stuff>(carry.NearbyStuff))
        {
            if (s != null) Grab(s);
        }
    }
}
