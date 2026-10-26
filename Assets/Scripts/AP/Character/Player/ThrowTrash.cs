using System.Collections.Generic;
using UnityEngine;

// 플레이어/직원이 TrashBox 범위 안에 있으면, 손에 든 쓰레기를 하나씩 TrashBox 에 넘긴다.
// (판단: 운반자 / 처리·연출: TrashBox.Receive)
// 쓰레기통 트리거가 여러 개 겹쳐 있어도(몸체 + Inside 등) 하나에서 나갈 때 멈추지 않도록 목록으로 관리
public class ThrowTrash : MonoBehaviour
{
    [SerializeField] private GrabAction grab;
    [SerializeField] private float throwInterval = 0.15f; // 한 개씩 버리는 간격(초)

    private readonly List<TrashBox> boxes = new List<TrashBox>();
    private float timer;

    // 파괴/비활성화된 것은 정리하고, 가장 최근에 들어간 쓰레기통을 사용
    private TrashBox CurrentBox
    {
        get
        {
            boxes.RemoveAll(b => b == null || !b.isActiveAndEnabled);
            return boxes.Count > 0 ? boxes[boxes.Count - 1] : null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out TrashBox box) || boxes.Contains(box)) return;
        if (boxes.Count == 0) timer = 0f;   // 처음 들어갈 때만 즉시 버리기
        boxes.Add(box);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out TrashBox box)) boxes.Remove(box);
    }

    private void Update()
    {
        TrashBox box = CurrentBox;
        if (box == null || grab.IsEmpty) return;
        if (grab.HeldType != StuffType.Trash) return;   // 쓰레기를 들고 있을 때만

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = throwInterval;

        box.Receive(grab.TakeTop());
    }
}
