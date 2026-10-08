using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CustomerState { InQueue, Ordering, WaitingTable, MoveToTable, Eating, Leaving }

public class Customer : MonoBehaviour
{
    [Header("프리팹 내부")]
    [SerializeField] Transform grabPoint;           // 받은 큐브를 쌓을 위치 (Scale 1,1,1)
    [SerializeField] float cubeStackHeight = 0.3f;

    [Header("식사 / 지불")]
    [SerializeField] float eatDuration = 5f;
    [SerializeField] GameObject moneyPrefab;
    [SerializeField] GameObject trashPrefab;

    public int RequiredCount { get; private set; }
    public int ReceivedCount { get; private set; }
    public bool IsOrderComplete => ReceivedCount >= RequiredCount;
    public bool CanReceive => state == CustomerState.Ordering && !IsOrderComplete;   // SellingAction용
    public event Action OnLeft;

    // Init으로 받는 참조
    CounterQueue queue;
    TableManager tables;
    Transform exitPoint;

    CustomerMover mover;
    CustomerState state;
    bool isFront;
    bool inQueue;

    Table targetTable;
    int seatIndex = -1;

    readonly List<Stuff> heldCubes = new List<Stuff>();

    void Awake() => mover = GetComponent<CustomerMover>();

    // ───── 바깥에서 호출 ─────

    public void Init(CounterQueue queue, TableManager tables, Transform exit, int required)
    {
        this.queue = queue;
        this.tables = tables;
        exitPoint = exit;
        RequiredCount = required;

        state = CustomerState.InQueue;
        inQueue = true;
        queue.Enqueue(this);          // → CounterQueue가 SetQueueSlot을 호출해 줌
    }

    // CounterQueue가 호출: 줄에 처음 섰을 때, 앞사람이 빠져서 당겨질 때
    public void SetQueueSlot(Vector3 position, bool front)
    {
        isFront = front;
        if (state == CustomerState.InQueue)
            mover.MoveTo(position);
    }

    // SellingAction이 호출: CubeSpot.TakeTop()으로 꺼낸 큐브를 받음
    public void ReceiveCube(Stuff cube)
    {
        if (!CanReceive || cube == null) return;

        if (cube.TryGetComponent(out Collider col)) col.enabled = false;   // 플레이어가 다시 줍지 못하게
        cube.transform.SetParent(grabPoint, false);
        cube.transform.localPosition = Vector3.up * cubeStackHeight * heldCubes.Count;
        cube.transform.localRotation = Quaternion.identity;

        heldCubes.Add(cube);
        ReceivedCount++;
        Debug.Log("큐브 겟도다제: " + ReceivedCount + "/" + RequiredCount);
        // TODO: OrderBubble 갱신 (RequiredCount - ReceivedCount)
    }

    // ───── 상태 머신 ─────

    void Update()
    {
        switch (state)
        {
            case CustomerState.InQueue:
                if (isFront && mover.HasArrived()) ChangeState(CustomerState.Ordering);
                break;

            case CustomerState.Ordering:
            case CustomerState.WaitingTable:
                if (IsOrderComplete) TryGoToTable();
                break;

            case CustomerState.MoveToTable:
                if (mover.HasArrived()) ChangeState(CustomerState.Eating);
                break;

            case CustomerState.Eating:
                break; // 코루틴이 처리

            case CustomerState.Leaving:
                if (mover.HasArrived()) Destroy(gameObject);
                break;
        }
    }

    void ChangeState(CustomerState next)
    {
        state = next;

        switch (next)   // 상태에 "들어갈 때" 한 번만 할 일
        {
            case CustomerState.Ordering:
                // TODO: 말풍선 켜기
                break;

            case CustomerState.MoveToTable:
                // TODO: 말풍선 끄기
                mover.MoveTo(targetTable.GetSeat(seatIndex).position);
                break;

            case CustomerState.Eating:
                mover.Stop();
                Vector3 dir = targetTable.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
                StartCoroutine(EatRoutine());
                break;

            case CustomerState.Leaving:
                mover.MoveTo(exitPoint.position);
                break;
        }
    }

    void TryGoToTable()
    {
        if (tables.TryReserve(out targetTable, out seatIndex))
        {
            LeaveQueue();                              // 예약 성공 후에 줄에서 빠짐
            ChangeState(CustomerState.MoveToTable);
        }
        else if (state != CustomerState.WaitingTable)
        {
            ChangeState(CustomerState.WaitingTable);   // 카운터 앞에서 계속 대기
        }
    }

    IEnumerator EatRoutine()
    {
        yield return new WaitForSeconds(eatDuration);
        LeaveStuff();
        ReleaseSeat();
        ChangeState(CustomerState.Leaving);
    }

    void LeaveStuff()
    {
        foreach (var cube in heldCubes) Destroy(cube.gameObject);
        heldCubes.Clear();

        targetTable.PlaceTip(moneyPrefab);   // 쌓는 위치는 Table이 결정
        targetTable.PlaceTrash(trashPrefab);
        Instantiate(trashPrefab, targetTable.transform.position + Vector3.up * 0.5f, Quaternion.identity);
    }

    // ───── 정리 ─────

    void LeaveQueue()
    {
        if (!inQueue) return;
        inQueue = false;
        if (queue != null) queue.Remove(this);
    }

    void ReleaseSeat()
    {
        if (targetTable == null) return;
        targetTable.Release(seatIndex);
        targetTable = null;
        seatIndex = -1;
    }

    void OnDestroy()
    {
        LeaveQueue();     // 줄에 null이 남지 않게
        ReleaseSeat();    // 자리가 영원히 예약되지 않게
        OnLeft?.Invoke();
    }
}