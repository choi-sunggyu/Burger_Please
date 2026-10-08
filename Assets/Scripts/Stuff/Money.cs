using UnityEngine;

public class Money : Stuff
{
    [SerializeField] private int value = 10; // 주웠을 때 오르는 금액
    public int Value => value;

    private void Awake()
    {
        Type = StuffType.Money;
    }
}
