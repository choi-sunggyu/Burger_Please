using UnityEngine;

public class Wallet : MonoBehaviour
{
    [SerializeField] private int money = 100; // 플레이어가 소유한 돈
    public int Money => money;

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
