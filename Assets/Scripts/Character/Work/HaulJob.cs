using UnityEngine;

// "더미(Source)에서 줍고 → 놓을 곳(Drop)에 내려놓기"를 반복하는 일의 공통 흐름
// 실제 줍기/내려놓기/버리기는 직원의 Carry·GrabAction·PutAction·ThrowTrash가 트리거로 처리하고,
// 이 클래스는 "지금 어디로 가야 하는가"만 결정한다.
// 자식 클래스: DeliverCubeJob(큐브/큐브팩 운반), CleanTableJob(쓰레기 청소)
public abstract class HaulJob : JobBase
{
    protected abstract Transform SourcePile { get; }    // 주울 물건들이 '자식'으로 쌓여 있는 곳
    protected abstract StuffType ItemType { get; }      // 옮길 물건 종류
    protected abstract Transform GetDropTarget();       // 내려놓을 곳 (null이면 놓을 곳이 없음)

    bool unloading;   // 내려놓는 중인가 (손이 빌 때까지 놓는 곳에 머묾)

    protected int SourceCount => SourcePile != null ? SourcePile.childCount : 0;

    public override StuffType? WantedType => ItemType;

    // 주울 게 있고, 놓을 곳도 있으면 새로 맡을 가치가 있음
    public override bool IsStillNeeded => SourceCount > 0 && GetDropTarget() != null;

    public override void OnAssigned(Employee worker)
    {
        base.OnAssigned(worker);
        unloading = false;
    }

    public override Transform GetTarget(Employee worker)
    {
        GrabAction hands = worker.Hands;
        if (hands == null) return null;

        bool holding = !hands.IsEmpty && hands.HeldType == ItemType;

        // 1) 내려놓는 중이면 손이 빌 때까지 계속 놓는 곳에 있기
        if (unloading)
        {
            if (holding) return GetDropTarget();
            unloading = false;
        }

        // 2) 손이 꽉 찼거나 더미가 비었으면 내려놓으러
        if (holding && (hands.IsFull || SourceCount == 0))
        {
            unloading = true;
            return GetDropTarget();
        }

        // 3) 더미에 남은 게 있고 놓을 곳도 있으면 주우러
        if (SourceCount > 0 && GetDropTarget() != null) return SourcePile;

        // 4) 들고 있는 게 남았으면 마저 내려놓기, 아니면 일 끝
        if (holding)
        {
            unloading = true;
            return GetDropTarget();
        }
        return null;
    }
}
