using UnityEngine;

// 테이블 청소
// - 쓰레기가 생기면 그 위치로 이동 → 콜라이더 충돌로 자동 수거 (Carry/GrabAction)
// - 다 주웠으면 TrashBox로 이동 → 자동으로 버림 (ThrowTrash)
// - 할 일이 없으면 제자리에서 대기 (Employee의 Idle)
// "쓰레기 감시자"는 따로 만들지 않음: 테이블마다 이 일이 붙어 있고, JobBoard가 IsStillNeeded로 감시함
//
// [붙이는 곳] Table 프리팹 루트 → trashPoint 자동, trashBox는 씬의 TrashBox 자동
public class CleanTableJob : HaulJob
{
    [SerializeField] Transform trashPoint;     // 테이블의 쓰레기가 '자식'으로 쌓이는 곳 (비우면 Table에서 찾음)
    [SerializeField] Transform trashBox;       // 쓰레기를 버릴 곳 (비우면 씬의 TrashBox)

    public override JobRole Role => JobRole.Clean;
    protected override Transform SourcePile => trashPoint;
    protected override StuffType ItemType => StuffType.Trash;
    protected override Transform GetDropTarget() => trashBox != null ? trashBox : null;

    void Awake()
    {
        if (trashPoint == null && TryGetComponent(out Table table)) trashPoint = table.TrashPoint;

        if (trashBox == null)
        {
            TrashBox box = FindFirstObjectByType<TrashBox>();
            if (box != null) trashBox = box.transform;
        }

        if (trashPoint == null)
            Debug.LogError($"[CleanTableJob] {name}: trashPoint가 없습니다. Table과 같은 오브젝트에 붙이거나 직접 연결하세요.", this);
        if (trashBox == null)
            Debug.LogWarning($"[CleanTableJob] {name}: 씬에 TrashBox가 없습니다.", this);
    }
}
