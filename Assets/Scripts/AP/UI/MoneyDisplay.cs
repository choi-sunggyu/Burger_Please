using UnityEngine;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private Wallet wallet;
    [SerializeField] private TMP_Text moneyUI;
    
    void Update()
    {
        moneyUI.text = wallet.Money.ToString();
    }
}
