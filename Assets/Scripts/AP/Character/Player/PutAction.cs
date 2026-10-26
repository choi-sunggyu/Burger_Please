using UnityEngine;

public class PutAction : MonoBehaviour
{
    [SerializeField] private Carry carry;
    [SerializeField] private GrabAction grab;
    [SerializeField] private float putInterval = 0.15f; // 한 개씩 내려놓는 간격(초)

    private IStuffReceiver currentReceiver;
    private float timer;

    private void OnEnable()
    {
        carry.OnReceiverEntered += EnterReceiver;
        carry.OnReceiverExited += ExitReceiver;
    }

    private void OnDisable()
    {
        carry.OnReceiverEntered -= EnterReceiver;
        carry.OnReceiverExited -= ExitReceiver;
    }

    private void EnterReceiver(IStuffReceiver receiver)
    {
        currentReceiver = receiver; //콜라이더 충돌로 현재 내려놓을 수 있는 곳을 기억
        timer = 0f;
    }

    private void ExitReceiver(IStuffReceiver receiver)
    {
        if (currentReceiver == receiver) currentReceiver = null;
    }

    private void Update()
    {
        if (currentReceiver == null || grab.IsEmpty) return; // 내려놓을 곳이 없거나 손에 든 게 없으면 아무것도 안 함
        if (!currentReceiver.CanReceive(grab.HeldType)) return; // 내려놓을 곳이 지금 받을 수 없는 종류면 아무것도 안 함

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        Stuff top = grab.TakeTop(); // 제일 위에 있는 것 내려놓기

        // 유효한 오브젝트가 없으면 전달하지 않음
        if (top == null)
            return;

        timer = putInterval;
        currentReceiver.Receive(top);
    }
}
