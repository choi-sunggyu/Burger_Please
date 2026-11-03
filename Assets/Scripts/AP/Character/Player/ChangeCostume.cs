using UnityEngine;

public class ChangeCostume : MonoBehaviour
{
    // 기본 모델과 코스튬 모델을 번갈아 켜고 끄는 스크립트
    [SerializeField] private GameObject defaultCostume; // basics_Idle
    [SerializeField] private GameObject changedCostume; // macaron_walk

    private AstronautPlayer.AstronautPlayer player;
    private bool isChanged;

    void Awake()
    {
        player = GetComponentInParent<AstronautPlayer.AstronautPlayer>();
        ApplyCostume();
    }

    public void Toggle()
    {
        isChanged = !isChanged;
        ApplyCostume();

        // 켜진 모델의 Animator로 교체
        if (player != null) player.RefreshAnimator();
    }

    private void ApplyCostume()
    {
        if (defaultCostume == null || changedCostume == null)
        {
            Debug.LogWarning("Costume 모델이 할당되지 않았습니다.");
            return;
        }

        defaultCostume.SetActive(!isChanged);
        changedCostume.SetActive(isChanged);
    }
}
