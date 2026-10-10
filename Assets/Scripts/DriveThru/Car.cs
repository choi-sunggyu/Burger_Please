using UnityEngine;

public enum CarState { Queued, Ordering, Leaving }

public class Car : MonoBehaviour, IBuyer
{
    [SerializeField] float speed = 6f;
    public int ReceivedCount { get; private set; }
    public bool IsOrderComplete => ReceivedCount >= RequiredCount;
    public bool CanReceive => state == CarState.Ordering && !IsOrderComplete;

    public int RequiredCount { get; private set; }
    public float Distance => distance;          // Lane이 스폰 공간 확인에 사용

    DriveThruLane lane;
    DriveThruPath path;
    CarState state;
    float distance;          // 도로 시작점부터 현재 위치까지 (m)
    float targetDistance;    // 가야 할 거리
    bool isFront;
    bool inLane;

    bool Arrived => Mathf.Approximately(distance, targetDistance);

    public void Init(DriveThruLane lane, int required)
    {
        this.lane = lane;
        path = lane.Path;
        RequiredCount = required;

        distance = targetDistance = 0f;
        Place();                           // 생성 직후 도로 시작점으로

        state = CarState.Queued;
        inLane = true;
        lane.Enqueue(this);                // → SetQueueSlot 호출됨
    }

    // Lane이 호출: 줄에 섰을 때, 앞차가 빠져서 당겨질 때
    public void SetQueueSlot(float slotDistance, bool front)
    {
        isFront = front;
        if (state == CarState.Queued) targetDistance = slotDistance;
    }

    void Update()
    {
        if (path == null) return;

        distance = Mathf.MoveTowards(distance, targetDistance, speed * Time.deltaTime);
        Place();

        switch (state)
        {
            case CarState.Queued:
                if (isFront && Arrived) ChangeState(CarState.Ordering);
                break;

            case CarState.Ordering:
                if (IsOrderComplete) ChangeState(CarState.Leaving);
                break;

            case CarState.Leaving:
                if (Arrived) Destroy(gameObject);
                break;
        }
    }

    public void ReceiveItem(Stuff item)
    {
        if (!CanReceive || item == null) return;
        Destroy(item.gameObject);       // 차 안으로 들어간 것으로 처리
        ReceivedCount++;
    }

    void Place()
    {
        transform.position = path.GetPoint(distance);
        transform.forward  = path.GetDirection(distance);
    }

    void ChangeState(CarState next)
    {
        state = next;
        if (next == CarState.Leaving)
        {
            LeaveLane();
            targetDistance = path.Length;            // 도로 끝까지
        }
    }

    // 테스트용: Inspector에서 컴포넌트 ⋮ 메뉴 → "Test: Leave"
    [ContextMenu("Test: Leave")]
    void TestLeave() => ChangeState(CarState.Leaving);

    void LeaveLane()
    {
        if (!inLane) return;
        inLane = false;
        if (lane != null) lane.Remove(this);
    }

    void OnDestroy() => LeaveLane();
}