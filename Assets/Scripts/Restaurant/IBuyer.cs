// 물건을 사 가는 쪽 (Customer, Car)
public interface IBuyer
{
    bool CanReceive { get; }
    bool IsOrderComplete { get; }
    int RequiredCount { get; }
    void ReceiveItem(Stuff item);
}

// 구매자들의 줄 (CounterQueue, DriveThruLane)
public interface IBuyerQueue
{
    IBuyer Front { get; }
}