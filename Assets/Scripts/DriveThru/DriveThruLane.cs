using System.Collections.Generic;
using UnityEngine;

// 창구 앞 차량 줄. CounterQueue와 같은 구조이고, 자리를 "도로 위 거리"로 계산한다.
public class DriveThruLane : MonoBehaviour, IBuyerQueue
{
    [SerializeField] DriveThruPath path;
    [SerializeField] Transform windowPoint;   // 창구 위치 (도로 근처에 두면 가장 가까운 도로 지점을 찾음)
    [SerializeField] float spacing = 8f;      // 차 사이 간격 (차 길이보다 크게)
    [SerializeField] int maxLength = 8;

    readonly List<Car> line = new List<Car>();   // 0번 = 창구 앞
    float windowDistance;

    IBuyer IBuyerQueue.Front => Front;
    public DriveThruPath Path => path;
    public Car Front => line.Count > 0 ? line[0] : null;

    // 도로 시작점보다 뒤로는 줄을 세울 수 없으므로 실제 최대 대수를 제한
    int Capacity => Mathf.Min(maxLength, Mathf.FloorToInt(windowDistance / spacing) + 1);
    public bool IsFull => line.Count >= Capacity;

    // 새 차를 받을 수 있는가: 자리가 있고, 마지막 차가 진입로를 벗어났는가
    public bool CanAccept => !IsFull && (line.Count == 0 || line[line.Count - 1].Distance >= spacing);

    void Start() => windowDistance = path.GetClosestDistance(windowPoint.position);   // path.Awake 이후

    public void Enqueue(Car car)
    {
        if (car == null || IsFull || line.Contains(car)) return;
        line.Add(car);
        car.SetQueueSlot(SlotDistance(line.Count - 1), line.Count == 1);
    }

    public void Remove(Car car)
    {
        if (!line.Remove(car)) return;
        for (int i = 0; i < line.Count; i++)
            line[i].SetQueueSlot(SlotDistance(i), i == 0);
    }

    float SlotDistance(int index) => windowDistance - spacing * index;

    void OnDrawGizmos()
    {
        if (path == null || windowPoint == null) return;
        if (!Application.isPlaying) path.Build();   // 편집 중에는 점이 움직일 수 있으므로 다시 계산

        float wd = path.GetClosestDistance(windowPoint.position);
        for (int i = 0; i < maxLength; i++)
        {
            float d = wd - spacing * i;
            if (d < 0f) break;                       // 도로 시작점보다 뒤는 그리지 않음
            Gizmos.color = i == 0 ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(path.GetPoint(d) + Vector3.up * 0.5f, new Vector3(1f, 1f, 1f));
        }
    }
}