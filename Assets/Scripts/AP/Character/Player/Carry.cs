using System;
using System.Collections.Generic;
using UnityEngine;

public class Carry : MonoBehaviour
{
    // 플레이어 트리거에 들어온 것을 감지해서 이벤트로 알려주는 역할만 담당
    // - Stuff        -> GrabAction 이 구독
    // - IStuffReceiver -> PutAction 이 구독

    public event Action<Stuff> OnStuffEntered;
    public event Action<IStuffReceiver> OnReceiverEntered;
    public event Action<IStuffReceiver> OnReceiverExited;

    // 지금 범위 안에 있는 Stuff 목록 (손이 비었을 때 다시 줍기용)
    private readonly List<Stuff> nearbyStuff = new List<Stuff>();
    public IReadOnlyList<Stuff> NearbyStuff => nearbyStuff;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stuff") && other.TryGetComponent(out Stuff stuff))
        {
            if (!nearbyStuff.Contains(stuff)) nearbyStuff.Add(stuff);
            OnStuffEntered?.Invoke(stuff);
            return;
        }

        if (other.TryGetComponent(out IStuffReceiver receiver))
        {
            OnReceiverEntered?.Invoke(receiver);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Stuff stuff))
        {
            nearbyStuff.Remove(stuff);
            return;
        }

        if (other.TryGetComponent(out IStuffReceiver receiver))
        {
            OnReceiverExited?.Invoke(receiver);
        }
    }

    // 주웠거나 파괴된 Stuff는 목록에서 제거 (콜라이더를 끄면 OnTriggerExit가 오지 않기 때문)
    public void Forget(Stuff stuff) => nearbyStuff.Remove(stuff);
}
