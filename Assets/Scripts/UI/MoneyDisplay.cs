using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private PlayerStat playerStat;
    [SerializeField] private TMP_Text moneyUI;
    
    void Update()
    {
        moneyUI.text = playerStat.Money.ToString();
    }
}
