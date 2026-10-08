using UnityEngine;

// 플레이어가 TrashBox 범위 안에 있으면, 손에 든 쓰레기를 하나씩 TrashBox 에 넘긴다.
// (판단: 플레이어 / 처리·연출: TrashBox.Receive)
public class ThrowTrash : MonoBehaviour
{
    [SerializeField] private GrabAction grab;
    [SerializeField] private float throwInterval = 0.15f; // 한 개씩 버리는 간격(초)

    private TrashBox currentBox;
    private float timer;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out TrashBox box))
        {
            currentBox = box;
            timer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out TrashBox box) && box == currentBox)
        {
            currentBox = null;
        }
    }

    private void Update()
    {
        if (currentBox == null || grab.IsEmpty) return;
        if (grab.HeldType != StuffType.Trash) return;   // 쓰레기를 들고 있을 때만

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = throwInterval;

        Stuff trash = grab.TakeTop();
        currentBox.Receive(trash);
    }
}
