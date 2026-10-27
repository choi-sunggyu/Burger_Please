using System;
using System.Collections.Generic;
using UnityEngine;

public class Carry : MonoBehaviour
{
    // 감지 반경 안에 들어온 것을 '거리 확인'으로 찾아서 이벤트로 알려주는 역할만 담당 (트리거 충돌 X)
    // - Stuff        -> GrabAction 이 구독
    // - IStuffReceiver -> PutAction 이 구독
    // ThrowTrash, SellingAction, Packing 도 이 컴포넌트의 IsNear 로 거리를 확인한다

    public event Action<Stuff> OnStuffEntered;
    public event Action<IStuffReceiver> OnReceiverEntered;
    public event Action<IStuffReceiver> OnReceiverExited;

    [SerializeField] private float range;                  // 감지 반경(m). 0이면 같은 오브젝트의 SphereCollider 크기를 그대로 사용
    [SerializeField] private float checkInterval = 0.1f;   // 거리 확인 간격(초)

    // 지금 범위 안에 있는 Stuff 목록 (손이 비었을 때 다시 줍기용)
    private readonly List<Stuff> nearbyStuff = new List<Stuff>();
    public IReadOnlyList<Stuff> NearbyStuff => nearbyStuff;

    private readonly List<IStuffReceiver> nearbyReceivers = new List<IStuffReceiver>();
    private Vector3 localCenter;   // 감지 중심 (SphereCollider의 center)
    private float timer;

    public float Range => range;
    public Vector3 Center => transform.TransformPoint(localCenter);

    private void Awake()
    {
        // 예전 트리거(SphereCollider)와 같은 범위가 되도록 반경/중심을 가져옴 → 프리팹 수정 없이 동작이 같게
        if (TryGetComponent(out SphereCollider sphere))
        {
            localCenter = sphere.center;
            Vector3 s = transform.lossyScale;
            if (range <= 0f) range = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
        if (range <= 0f) range = 1f;
    }

    private void OnEnable() => Registry<Carry>.Add(this);

    private void OnDisable()
    {
        Registry<Carry>.Remove(this);

        // 꺼질 때 범위 안에 있던 것들에서 나간 것으로 처리
        foreach (IStuffReceiver r in nearbyReceivers) OnReceiverExited?.Invoke(r);
        nearbyReceivers.Clear();
        nearbyStuff.Clear();
    }

    // target(의 콜라이더 표면)이 감지 반경 안에 있나
    public bool IsNear(Component target) =>
        target != null && Proximity.InRange(Center, target, range);

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        CheckStuff();
        CheckReceivers();
    }

    private void CheckStuff()
    {
        // 멀어졌거나, 누가 주웠거나(콜라이더 꺼짐), 파괴된 것은 제거
        nearbyStuff.RemoveAll(s => s == null || !s.IsLoose || !IsNear(s));

        // 새로 들어온 것만 알림 (Grab 안에서 목록이 바뀔 수 있으므로 뒤에서부터 순회)
        IReadOnlyList<Stuff> all = Registry<Stuff>.Items;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (i >= all.Count) continue;
            Stuff s = all[i];
            if (s == null || !s.IsLoose || nearbyStuff.Contains(s) || !IsNear(s)) continue;

            nearbyStuff.Add(s);
            OnStuffEntered?.Invoke(s);
        }
    }

    private void CheckReceivers()
    {
        for (int i = nearbyReceivers.Count - 1; i >= 0; i--)
        {
            IStuffReceiver r = nearbyReceivers[i];
            if (IsNearReceiver(r)) continue;

            nearbyReceivers.RemoveAt(i);
            OnReceiverExited?.Invoke(r);
        }

        foreach (IStuffReceiver r in Registry<IStuffReceiver>.Items)
        {
            if (nearbyReceivers.Contains(r) || !IsNearReceiver(r)) continue;

            nearbyReceivers.Add(r);
            OnReceiverEntered?.Invoke(r);
        }
    }

    // 켜져 있고 범위 안에 있는 받는 곳인가
    private bool IsNearReceiver(IStuffReceiver r) =>
        r is Behaviour b && b != null && b.isActiveAndEnabled && IsNear(b);

    // 주웠거나 파괴된 Stuff는 목록에서 제거
    public void Forget(Stuff stuff) => nearbyStuff.Remove(stuff);
}
