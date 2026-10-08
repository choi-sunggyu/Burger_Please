using UnityEngine;

public class Table : MonoBehaviour
{
    [SerializeField] Transform[] seatPoints;     // 자리 수만큼 (업그레이드로 늘어날 수 있음)
    [SerializeField] Transform tipPoint;         // 팁이 쌓이는 곳 (테이블 옆 한 곳, Scale 1,1,1)
    [SerializeField] float moneyStackHeight = 0.15f;

    bool[] occupied;

    public bool IsFull => System.Array.IndexOf(occupied, false) < 0;

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

    // 손님이 남긴 돈을 팁 위치에 위로 쌓기
    public void PlaceTip(GameObject moneyPrefab)
    {
        Vector3 offset = Vector3.up * moneyStackHeight * tipPoint.childCount;
        Instantiate(moneyPrefab, tipPoint.position + offset, Quaternion.identity, tipPoint);
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