using System.Collections.Generic;
using UnityEngine;

// 플레이어/직원이 TrashBox 근처(Carry 의 감지 반경 안)에 있으면, 손에 든 쓰레기를 하나씩 TrashBox 에 넘긴다.
// (판단: 운반자 / 처리·연출: TrashBox.Receive)
// 트리거 충돌 대신 거리로 확인. 쓰레기통이 여러 개 가까이 있어도 하나에서 멀어질 때 멈추지 않도록 목록으로 관리
public class ThrowTrash : MonoBehaviour
{
    [SerializeField] private GrabAction grab;
    [SerializeField] private Carry carry;                 // 감지 반경/중심을 같이 씀 (비우면 같은 오브젝트에서 찾음)
    [SerializeField] private float throwInterval = 0.15f; // 한 개씩 버리는 간격(초)

    private readonly List<TrashBox> boxes = new List<TrashBox>();
    private float timer;

    // 파괴/비활성화된 것은 정리하고, 가장 최근에 가까워진 쓰레기통을 사용
    private TrashBox CurrentBox
    {
        get
        {
            boxes.RemoveAll(b => b == null || !b.isActiveAndEnabled);
            return boxes.Count > 0 ? boxes[boxes.Count - 1] : null;
        }
    }

    private void Awake()
    {
        if (carry == null) carry = GetComponent<Carry>();
    }

    // 멀어진 쓰레기통은 빼고, 새로 가까워진 쓰레기통은 넣기
    private void CheckBoxes()
    {
        boxes.RemoveAll(b => b == null || !carry.IsNear(b));

        foreach (TrashBox box in Registry<TrashBox>.Items)
        {
            if (boxes.Contains(box) || !carry.IsNear(box)) continue;
            if (boxes.Count == 0) timer = 0f;   // 처음 가까워질 때만 즉시 버리기
            boxes.Add(box);
        }
    }

    private void Update()
    {
        if (carry == null) return;
        CheckBoxes();

        TrashBox box = CurrentBox;
        if (box == null || grab.IsEmpty) return;
        if (grab.HeldType != StuffType.Trash) return;   // 쓰레기를 들고 있을 때만

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = throwInterval;

        box.Receive(grab.TakeTop());
    }
}
