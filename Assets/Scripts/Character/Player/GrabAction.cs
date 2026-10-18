using System.Collections.Generic;
using UnityEngine;

public class GrabAction : MonoBehaviour
{
    [SerializeField] private Carry carry;
    [SerializeField] private Wallet wallet;               // 플레이어만. 직원은 비워둠 → 돈을 줍지 않음
    [SerializeField] private CarrierStat carrierStat;
    [SerializeField] private Transform grabPoint;
    [SerializeField] private float stackHeight = 0.3f; // 물건 하나 높이

    private readonly List<Stuff> holding = new List<Stuff>();

    // 직원용 줍기 필터. 켜져 있으면 allowedType만 줍는다 (null이면 아무것도 줍지 않음). 플레이어는 필터를 쓰지 않음
    private bool useFilter;
    private StuffType? allowedType;

    public int Count => holding.Count;
    public bool IsEmpty => holding.Count == 0;
    public StuffType HeldType => holding[0].Type; // IsEmpty 일 때는 호출하지 말 것
    public bool IsFull => !IsEmpty && HeldType != StuffType.Trash && holding.Count >= carrierStat.Capacity; // 쓰레기는 무제한

    private void Awake()
    {
        if (carrierStat == null) carrierStat = GetComponentInParent<CarrierStat>();
    }

    private void OnEnable()
    {
        carry.OnStuffEntered += Grab;
    }

    private void OnDisable()
    {
        carry.OnStuffEntered -= Grab;
    }

    // 직원이 일을 맡거나 끝낼 때 호출
    public void SetFilter(StuffType? allowed)
    {
        useFilter = true;
        allowedType = allowed;
        RetryNearby();   // 필터가 바뀌면 이미 범위 안에 있던 물건도 다시 확인
    }

    public void ClearFilter()
    {
        useFilter = false;
        RetryNearby();
    }

    private void Grab(Stuff stuff)
    {
        // 돈은 손에 들지 않고 바로 소지금으로 (같은 종류 규칙의 예외). 지갑이 없는 운반자(직원)는 무시
        if (stuff is Money money)
        {
            if (wallet != null) CollectMoney(money);
            return;
        }

        if (!CanHold(stuff)) return; // 용량 초과, 다른 물건을 들고 있음, 필터에 맞지 않음

        if (stuff.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true; // 물리 영향 끄기
        if (stuff.TryGetComponent(out Collider col)) col.enabled = false; // 충돌 끄기

        // stuff의 위치 이동 후에 해당 위치에 고정해서 캐릭터와 함께 움직이도록
        stuff.transform.SetParent(grabPoint, false); // 손 위치로 이동
        stuff.transform.localPosition = Vector3.up * stackHeight * holding.Count; // 쌓기
        stuff.transform.localRotation = Quaternion.identity; // 회전 초기화

        carry.Forget(stuff); // 주변 목록에서 제거 (GrabAction 이 들고 있으므로 Carry 가 더 이상 관리하지 않음)
        holding.Add(stuff);
    }

    private bool CanHold(Stuff stuff)
    {
        if (holding.Contains(stuff)) return false;
        if (useFilter && (allowedType == null || stuff.Type != allowedType.Value)) return false; // 직원: 맡은 일의 종류만
        if (stuff.Type != StuffType.Trash && holding.Count >= carrierStat.Capacity) return false; // 쓰레기는 무제한
        if (!IsEmpty && stuff.Type != HeldType) return false;      // 다른 종류는 내려놓기 전까지 불가
        return true;
    }

    // 돈 한 장에 닿으면 그 돈이 속한 더미(MoneyPile) 전체를 획득. 더미가 아니면 그 한 장만
    private void CollectMoney(Money money)
    {
        Transform parent = money.transform.parent;
        if (parent != null && parent.TryGetComponent(out MoneyPile pile))
        {
            foreach (Money m in pile.TakeAll()) Collect(m);
        }
        else
        {
            Collect(money);
        }
    }

    private void Collect(Money money)
    {
        wallet.GetMoney(money.Value);
        carry.Forget(money);
        Destroy(money.gameObject);
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
        if (carry == null) return;

        // Grab 안에서 carry 목록이 바뀌므로 복사본으로 순회
        foreach (Stuff s in new List<Stuff>(carry.NearbyStuff))
        {
            if (s != null) Grab(s);
        }
    }
}
