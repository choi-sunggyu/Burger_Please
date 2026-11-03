using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnCharacterChangeSmoke : MonoBehaviour
{
    // C를 누르면 Smoke가 생성되도록 하는 스크립트
    [SerializeField] private GameObject smokePrefab; // Smoke Prefab
    [SerializeField] private ChangeCostume changeCostume; // 연기와 함께 전환할 코스튬

    void Update()
    {
        if(Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SpawnSmoke();
            if (changeCostume != null) changeCostume.Toggle();
        }
    }

    private void SpawnSmoke()
    {
        if (smokePrefab != null)
        {
            GameObject smoke = Instantiate(smokePrefab, transform.position, Quaternion.identity);
            Destroy(smoke, 2f); // 2초 후에 Smoke 파괴
        }
        else
        {
            Debug.LogWarning("Smoke Prefab이 할당되지 않았습니다.");
        }
    }
}
