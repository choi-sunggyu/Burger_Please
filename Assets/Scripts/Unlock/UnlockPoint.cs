using UnityEngine;
using TMPro;

public class UnlockPoint : MonoBehaviour
{
    [Header("Unlock")]
    [SerializeField] private int requiredMoney = 100;
    [SerializeField] private GameObject unlockTarget;

    [Header("UI")]
    [SerializeField] private TMP_Text costText;

    [Header("Payment")]
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float payInterval = 0.01f;

    private int remainingMoney;

    [SerializeField] private Wallet wallet;

    private float stayTimer;
    private float payTimer;

    private bool playerInside; // 발판 위에 플레이어가 있는지 판단

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 확대 축소 모션 이벤트 추가 예정
        remainingMoney = requiredMoney;
        UpdateCostText();
    }

    private void Update()
    {
        if (!playerInside) // 플레이어 확인
        {
            return;
        }

        if (wallet == null) // 플레이어 확인
        {
            return;
        }


        stayTimer += Time.deltaTime;

        if (stayTimer < startDelay)  // 1초동안 머무르는지 확인
            return;

        payTimer += Time.deltaTime;

        if (payTimer >= payInterval)
        {
            payTimer = 0f;
            PayMoney();
        }
    }

    private void PayMoney()
    {
        if (remainingMoney <= 0) // 돈 없으면 가라
            return;

        bool paid = wallet.SpendMoney(1); //1원 지불확인

        if (!paid) //지불 안됐으면 가라
            return;

        remainingMoney--; //지불 됐으면 남은 cost 깎아줄게
        UpdateCostText(); // 표시도 해줄게

        if (remainingMoney <= 0)
        {
            Unlock(); // 다 지불되었으면 unlock할건데
        }
    }

    private void Unlock()
    {
        if (unlockTarget != null) // 무엇을 unlock할건지 정해졌있다면
        {
            unlockTarget.SetActive(true); // 내 마음속에 언!락!
        }

        Destroy(gameObject); // 발판 사라짐
    }

    private void UpdateCostText()
    {
        costText.text = remainingMoney.ToString();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        wallet = other.GetComponent<Wallet>();

        if (wallet == null)
            return;

        playerInside = true;
        stayTimer = 0f;
        payTimer = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;
        wallet = null;

        stayTimer = 0f;
        payTimer = 0f;
    }
}
