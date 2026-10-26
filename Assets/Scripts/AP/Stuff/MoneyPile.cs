using System.Collections.Generic;
using UnityEngine;

// 돈이 쌓이는 곳(테이블 TipPoint, 계산대 MoneyPoint)
// - 돈을 낮고 넓은 격자로 쌓아서 높이가 끝없이 올라가지 않게 함 (넣을 때마다 다시 정렬)
// - 플레이어가 한 장이라도 닿으면 더미 전체를 한꺼번에 획득 (GrabAction이 TakeAll 호출)
// 직접 붙여도 되고, 안 붙여도 Table/SellingAction이 MoneyPile.On()으로 실행 중에 자동으로 붙임
public class MoneyPile : MonoBehaviour
{
    [SerializeField] int columns = 2;                                   // 가로 칸 수
    [SerializeField] int rows = 2;                                      // 세로 칸 수
    [SerializeField] Vector3 cellSize = new Vector3(1.05f, 0.11f, 0.55f); // 칸 간격 (Money 프리팹 크기 1 x 0.1 x 0.5 기준)

    public int Count => transform.childCount;

    // 이 위치의 MoneyPile을 가져오고, 없으면 붙여서 돌려줌
    public static MoneyPile On(Transform point)
    {
        if (!point.TryGetComponent(out MoneyPile pile)) pile = point.gameObject.AddComponent<MoneyPile>();
        return pile;
    }

    public void Add(GameObject moneyPrefab)
    {
        Instantiate(moneyPrefab, transform.position, transform.rotation, transform);
        Relayout();
    }

    // 더미 전체를 꺼냄. 자식에서 즉시 빼서 개수가 바로 줄어들게 함 (Destroy는 프레임 끝에 일어나므로)
    public List<Money> TakeAll()
    {
        var list = new List<Money>(GetComponentsInChildren<Money>());
        foreach (Money m in list) m.transform.SetParent(null);
        return list;
    }

    // 바닥(가로 x 세로)을 먼저 채우고 다음 층으로. 가운데 정렬
    void Relayout()
    {
        int perLayer = Mathf.Max(1, columns * rows);
        for (int i = 0; i < transform.childCount; i++)
        {
            int layer = i / perLayer;
            int inLayer = i % perLayer;
            int row = inLayer / columns;
            int col = inLayer % columns;

            Transform child = transform.GetChild(i);
            child.localPosition = new Vector3(
                (col - (columns - 1) * 0.5f) * cellSize.x,
                layer * cellSize.y,
                (row - (rows - 1) * 0.5f) * cellSize.z);
            child.localRotation = Quaternion.identity;
        }
    }
}
