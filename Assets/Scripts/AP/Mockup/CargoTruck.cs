using System.Collections;
using UnityEngine;

public enum CargoMode
{
    Unload,   // 하차: 가득 찬 채 들어와서 다 비우면 나감
    Load      // 상차: 빈 채 들어와서 다 채우면 나감
}

// 트럭 적재량 관리. 박스는 트럭 안에 있는 동안 숫자로만 존재하고, 꺼낼 때 생성/넣을 때 제거한다
[RequireComponent(typeof(TruckMove))]
public class CargoTruck : MonoBehaviour
{
    [SerializeField] private CargoMode mode = CargoMode.Unload;
    [SerializeField] private int capacity = 12;
    [SerializeField] private float awayTime = 2f;        // 나간 뒤 다시 들어오기까지 시간(초)
    [SerializeField] private float workGap = 10f;        // 알바가 트럭 뒤에서 떨어져 서는 거리
    [SerializeField] private GameObject boxPrefab;
    [SerializeField] private float boxScale = 500f;      // 씬에 놓인 box와 같은 크기
    [SerializeField] private WorldLabel label;

    private TruckMove move;
    private Vector3 rearPoint;   // 트럭 뒤쪽 (도크 기준 고정 위치)
    private float rearHeight;
    private int count;
    private int reserved;        // 알바가 맡아 둔 수 (한 박스를 두 명이 맡지 않도록)

    public Vector3 WorkPoint => rearPoint;

    // 하차: 꺼낼 박스가 남았나 / 상차: 넣을 자리가 남았나
    public bool HasWork => move.IsDocked && (mode == CargoMode.Unload
        ? count - reserved > 0
        : count + reserved < capacity);

    private void Awake()
    {
        move = GetComponent<TruckMove>();
        CalculateRearPoint();
    }

    private void Start()
    {
        move.PlaceAway();
        StartCoroutine(ArriveAfter(0f));
    }

    private void CalculateRearPoint()
    {
        Bounds bounds = BoundsUtil.Of(gameObject);
        Vector3 dir = move.OutDirection;
        float halfLength = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(dir.x), Mathf.Abs(dir.y), Mathf.Abs(dir.z)));

        rearPoint = bounds.center - dir * (halfLength + workGap);
        rearPoint.y = bounds.min.y;
        rearHeight = bounds.size.y * 0.5f;
    }

    public void Reserve()
    {
        reserved++;
    }

    // 하차: 박스 하나를 트럭 뒤에서 꺼낸다
    public GameObject TakeBox()
    {
        reserved--;
        count--;
        UpdateLabel();

        GameObject box = Instantiate(boxPrefab, rearPoint + Vector3.up * rearHeight, boxPrefab.transform.rotation);
        VLB.BakedMaterialRemap.Apply(box);
        box.transform.localScale = Vector3.one * boxScale;

        if (count == 0) LeaveAndReturn();
        return box;
    }

    // 상차: 박스 하나를 트럭에 넣는다
    public void PutBox(GameObject box)
    {
        Destroy(box);
        reserved--;
        count++;
        UpdateLabel();

        if (count == capacity) LeaveAndReturn();
    }

    private void LeaveAndReturn()
    {
        move.Leave(() => StartCoroutine(ArriveAfter(awayTime)));
    }

    private IEnumerator ArriveAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        count = mode == CargoMode.Unload ? capacity : 0;
        reserved = 0;
        UpdateLabel();
        move.Enter(null);
    }

    private void UpdateLabel()
    {
        if (label == null) return;

        string title = mode == CargoMode.Unload ? "하차" : "상차";
        label.SetText($"{title} {count}/{capacity}");
    }
}
