using UnityEngine;

// 서로 다른 경로가 겹치는 교차로 영역
// 한 번에 한 경로의 차량만 진입 가능 (같은 경로는 양쪽 차선 모두 동시에 진입 가능)
public class TrafficZone
{
    public Rect Area;

    private TrafficRoute owner;
    private int count;

    // 다른 경로가 기다리는 중이면 현재 경로의 신규 진입을 막아 번갈아 통과하도록 함
    private TrafficRoute waiting;
    private int waitingFrame;

    public bool Occupied => count > 0;

    public TrafficZone(Rect area)
    {
        Area = area;
    }

    public bool Contains(Vector3 position)
    {
        return Area.Contains(new Vector2(position.x, position.z));
    }

    public bool TryEnter(TrafficRoute route)
    {
        // 대기 요청이 끊기면(앞이 막혀 요청을 멈춘 경우) 우선권 해제
        if (waiting != null && waitingFrame < Time.frameCount - 1)
            waiting = null;

        if (count > 0 && owner != route)
        {
            if (waiting == null)
                waiting = route;

            if (waiting == route)
                waitingFrame = Time.frameCount;

            return false;
        }

        if (waiting != null && waiting != route)
            return false;

        if (waiting == route)
            waiting = null;

        owner = route;
        count++;
        return true;
    }

    public void Exit()
    {
        count = Mathf.Max(0, count - 1);

        if (count == 0)
            owner = null;
    }
}
