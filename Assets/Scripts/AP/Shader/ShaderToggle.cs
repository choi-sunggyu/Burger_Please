
using UnityEngine;
using UnityEngine.InputSystem;

public class ShaderToggle : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer; // Shader를 적용할 Renderer (예: 캐릭터의 SkinnedMeshRenderer)

    [Header("Toon Materials")]
    [SerializeField] private Material[] toonMaterials; // 넣으려는 툰 셰이더인데 캐릭터마다 셰이더가 다르기 때문에 Material 슬롯 개수와 일치해야 함

    private Material[] originalMaterials; // 원래 Material을 저장하기 위한 배열
    private bool isToonEnabled; // 현재 툰 셰이더가 적용되어 있는지 여부

    private void Awake()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer가 없습니다.");
            enabled = false;
            return;
        }

        // 기존 Material 저장
        originalMaterials = targetRenderer.sharedMaterials;

        if (toonMaterials == null ||
            toonMaterials.Length != originalMaterials.Length)
        {
            Debug.LogError("Material 슬롯 개수가 일치하지 않습니다.");
            enabled = false;
        }
    }

    private void Update()
    {
        // T 키를 누르면 Shader 전환
        if (Keyboard.current != null &&
            Keyboard.current.tKey.wasPressedThisFrame)
        {
            ToggleShader();
        }
    }

    public void ToggleShader()
    {
        SetToon(!isToonEnabled);
    }

    public void SetToon(bool enabled)
    {
        if (targetRenderer == null ||
            originalMaterials == null ||
            toonMaterials == null ||
            originalMaterials.Length != toonMaterials.Length)
            return;

        isToonEnabled = enabled;

        targetRenderer.sharedMaterials = isToonEnabled
            ? toonMaterials
            : originalMaterials;

        Debug.Log(isToonEnabled
            ? "Toon Shader ON"
            : "Toon Shader OFF");
    }
}
