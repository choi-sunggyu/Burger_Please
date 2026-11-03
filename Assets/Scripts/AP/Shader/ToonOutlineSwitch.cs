using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬의 모든 캐릭터에 Toon / Outline을 버튼으로 넣고 뺀다 (Outline은 Toon이 켜져 있을 때만 누를 수 있음)
public class ToonOutlineSwitch : MonoBehaviour
{
    [System.Serializable]
    struct MaterialPair
    {
        public Material plain;
        public Material toon;
    }

    [Header("Buttons")]
    [SerializeField] Button toonButton;
    [SerializeField] Button outlineButton;

    [Header("Materials")]
    [SerializeField] MaterialPair[] pairs;   // 캐릭터별 기본 / 툰 머티리얼
    [SerializeField] Material outlineMaterial;

    bool toonOn = true;
    bool outlineOn = true;

    void Awake()
    {
        toonButton.onClick.AddListener(ToggleToon);
        outlineButton.onClick.AddListener(ToggleOutline);
    }

    void Start() => Apply();

    void ToggleToon()
    {
        toonOn = !toonOn;
        outlineOn = false;   // Toon을 껐다 켜도 Outline은 꺼진 상태로 시작
        Apply();
    }

    void ToggleOutline()
    {
        outlineOn = !outlineOn;
        Apply();
    }

    // shortcut: 실행 중 새로 스폰된 캐릭터는 다음 버튼 입력 때 반영되고, Tint로 복사된 머티리얼은 찾지 못함. 스포너를 쓰는 씬에 넣을 때 스폰 시 등록 방식으로 교체
    void Apply()
    {
        foreach (var r in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Material[] current = r.sharedMaterials;
            if (current.Length == 0) continue;

            foreach (MaterialPair pair in pairs)
            {
                if (current[0] != pair.plain && current[0] != pair.toon) continue;

                Material body = toonOn ? pair.toon : pair.plain;
                r.sharedMaterials = outlineOn ? new[] { body, outlineMaterial } : new[] { body };
                break;
            }
        }

        outlineButton.interactable = toonOn;
        toonButton.GetComponentInChildren<TMP_Text>().text = toonOn ? "Toon: ON" : "Toon: OFF";
        outlineButton.GetComponentInChildren<TMP_Text>().text = outlineOn ? "Outline: ON" : "Outline: OFF";
    }
}
