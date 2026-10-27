
using TMPro;
using UnityEngine;

public class SpeechBubbleUI : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
            transform.rotation = targetCamera.transform.rotation;
    }

    public void Show(string message)
    {
        if (visualRoot == null || dialogueText == null)
            return;

        dialogueText.SetText(message);
        visualRoot.SetActive(true);
    }

    public void Hide()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }
}
