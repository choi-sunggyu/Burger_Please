using UnityEngine;

public class TableManager : MonoBehaviour
{
    Table[] tables;
    void Awake() => tables = GetComponentsInChildren<Table>();    

    //업그레이드로 테이블이 생기면 호출
    public void RefreshTables() => tables = GetComponentsInChildren<Table>();
    
    public bool TryReserve(out Table table, out int seat)   // 모든 테이블을 돌며 Reserve() 시도
    {
        foreach (var t in GetComponentsInChildren<Table>())
        {
            seat = t.Reserve();
            if (seat >= 0)
            {
                table = t;
                return true;
            }
        }
        table = null;
        seat = -1;
        return false;
    }
}
