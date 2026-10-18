using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField] Transform[] seatPoints;     // 자리 수만큼 (업그레이드로 늘어날 수 있음)
    [SerializeField] Transform tipPoint;         // 팁이 쌓이는 곳 (테이블 옆 한 곳, Scale 1,1,1)
    [SerializeField] Transform trashPoint;
    [SerializeField] float trashStackHeight = 0.2f;

    bool[] occupied;

    public bool IsFull => System.Array.IndexOf(occupied, false) < 0;
    public Transform TrashPoint => trashPoint;           // CleanTableJob이 쓰레기를 주우러 오는 곳
    public bool IsDirty => trashPoint.childCount > 0;   // 치우지 않은 쓰레기가 있나

    void Awake()
    {
        occupied = new bool[seatPoints.Length];

        if (seatPoints.Length == 0)
            Debug.LogError($"[Table] {name}: seatPoints가 비어 있습니다.", this);
        if (tipPoint == null)
            Debug.LogError($"[Table] {name}: tipPoint가 연결되지 않았습니다.", this);
    }

    public int Reserve()
    {
        if (IsDirty) return -1; // 쓰레기 치우기 전에는 예약 불가

        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i]) continue;
            occupied[i] = true;
            return i;
        }
        return -1;
    }

    public void Release(int seatNumber)
    {
        if (seatNumber < 0 || seatNumber >= occupied.Length) return;
        occupied[seatNumber] = false;
    }

    public Transform GetSeat(int seat) => seatPoints[seat];

    // 손님이 남긴 돈을 팁 위치에 쌓기 (쌓는 모양은 MoneyPile이 결정)
    public void PlaceTip(GameObject moneyPrefab)
    {
        MoneyPile.On(tipPoint).Add(moneyPrefab);
    }

    public void PlaceTrash(GameObject trashPrefab)
    {
        Vector3 offset = Vector3.up * trashStackHeight * trashPoint.childCount;
        Instantiate(trashPrefab, trashPoint.position + offset, Quaternion.identity, trashPoint);   // 부모 지정이 핵심
    }

    void OnDrawGizmos()
    {
        if (seatPoints != null)
        {
            for (int i = 0; i < seatPoints.Length; i++)
            {
                Transform seat = seatPoints[i];
                if (seat == null) continue;

                bool isTaken = occupied != null && i < occupied.Length && occupied[i];
                Gizmos.color = isTaken ? Color.red : Color.green;
                Gizmos.DrawWireSphere(seat.position, 0.3f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(seat.position, seat.forward * 0.6f);   // 앉았을 때 바라볼 방향
            }
        }

        if (tipPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(tipPoint.position, Vector3.one * 0.3f);
        }
    }
}