using System.Collections.Generic;
using UnityEngine;

// Cube 를 바닥에 격자(columns x rows)로 깔고, 다 차면 한 층씩 위로 쌓는 장소
public class CubeSpot : MonoBehaviour, IStuffReceiver
{
    [SerializeField] private Transform stackOrigin;   // 쌓기 시작점 (Scale 1,1,1 인 빈 자식 오브젝트 권장)
    // 한 층에 모두 쌓도록 함
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 2;
    [SerializeField] private int maxLayers = 5;
    [SerializeField] private Vector3 cellSize = new Vector3(0.5f, 0.3f, 0.5f);
    [SerializeField] private StuffType acceptType = StuffType.Cube;

    private readonly List<Stuff> cubes = new List<Stuff>();
    private readonly List<Stuff> cubePacks = new List<Stuff>();

    public int Count => cubes.Count;
    private int Capacity => columns * rows * maxLayers;

    private void Awake()
    {
        if (stackOrigin == null) stackOrigin = transform;

        // 부모 쪽 Scale 이 (1,1,1) 이 아니면 쌓인 큐브가 늘어나거나 납작해진다
        if ((stackOrigin.lossyScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            Debug.LogWarning($"[CubeSpot] stackOrigin 의 월드 Scale 이 {stackOrigin.lossyScale} 입니다. " +
                             "Scale (1,1,1) 인 오브젝트 아래에 두세요.", this);
        }
    }

    public bool CanReceive(StuffType type) => type == acceptType && cubes.Count < Capacity;

    public void Receive(Stuff stuff)
    {
        stuff.transform.SetParent(stackOrigin, false);
        stuff.transform.localPosition = GetSlotPosition(cubes.Count);
        stuff.transform.localRotation = Quaternion.identity;
        cubes.Add(stuff);
        Debug.Log($"Cube placed ({cubes.Count}/{Capacity})");
    }

    // index 번째 칸의 위치: 바닥(가로 x 세로)을 먼저 채우고, 다 차면 다음 층
    private Vector3 GetSlotPosition(int index)
    {
        int perLayer = columns * rows;
        int layer = index / perLayer;
        int inLayer = index % perLayer;
        int row = inLayer / columns;
        int col = inLayer % columns;
        return new Vector3(col * cellSize.x, layer * cellSize.y, row * cellSize.z);
    }

    // 나중에 직원/판매 로직에서 꺼내 갈 때 사용
    public Stuff TakeTop()
    {
        if (cubes.Count == 0) return null;
        int last = cubes.Count - 1;
        Stuff top = cubes[last];
        cubes.RemoveAt(last);
        top.transform.SetParent(null);
        return top;
    }
}
