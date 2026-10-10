using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    [SerializeField] private int capacity = 3; // 한 번에 옮길 수 있는 양
    [SerializeField] private int money = 100; // 플레이어가 소유한 돈

    public int Money => money;
    public int Capacity => capacity;

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool SpendMoney(int cost)
    {
        if (cost <= 0)
            return false;

        if (money < cost)
            return false;

        money -= cost;
        return true; // 실제로 돈을 지불했느냐?
    }

    public void GetMoney(int amount)
    {
        if (amount <= 0)
            return;

        money += amount;
    }
}
