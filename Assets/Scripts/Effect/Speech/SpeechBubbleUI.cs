
using TMPro;
using UnityEngine;

public class SpeechBubbleUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Camera targetCamera;

    [SerializeField]
    private Vector3 worldOffset = new Vector3(0, 2.2f, 0);

    [SerializeField] private float uiYOffset = 15f;

    [Header("UI")]
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private TMP_Text dialogueText;

    private bool showRequested;

    private void Awake()
    {
        if (bubbleRect == null)
            bubbleRect = GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
                canvasRect = canvas.GetComponent<RectTransform>();
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        Hide();
    }

    private void LateUpdate()
    {
        if (showRequested)
            UpdatePosition();
    }

    public void Show(string message)
    {
        if (dialogueText == null || visualRoot == null)
            return;

        dialogueText.SetText(message);
        showRequested = true;

        UpdatePosition();
    }

    public void Hide()
    {
        showRequested = false;
        SetVisualActive(false);
    }

    private void UpdatePosition()
    {
        if (target == null ||
            targetCamera == null ||
            bubbleRect == null ||
            canvasRect == null)
        {
            SetVisualActive(false);
            return;
        }

        // 1. 캐릭터 머리 위 월드 좌표
        Vector3 worldPosition =
            target.position + worldOffset;

        // 2. 월드 좌표 -> 화면 좌표
        Vector3 screenPoint =
            targetCamera.WorldToScreenPoint(worldPosition);

        // 3. 카메라 뒤 또는 화면 밖이면 숨김
        bool isVisible =
            screenPoint.z > 0f &&
            screenPoint.x >= 0f &&
            screenPoint.x <= Screen.width &&
            screenPoint.y >= 0f &&
            screenPoint.y <= Screen.height;

        if (!isVisible)
        {
            SetVisualActive(false);
            return;
        }

        // 4. 화면 좌표 -> Canvas 내부 좌표
        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    null,
                    out Vector2 localPoint
                );

        if (!converted)
        {
            SetVisualActive(false);
            return;
        }

        // 5. 캐릭터 머리 위로 말풍선 배치
        bubbleRect.anchoredPosition =
            localPoint + Vector2.up * uiYOffset;

        SetVisualActive(true);
    }

    private void SetVisualActive(bool visible)
    {
        if (visualRoot != null &&
            visualRoot.activeSelf != visible)
        {
            visualRoot.SetActive(visible);
        }
    }

    public void Bind(Transform followTarget, Vector3 offset)
    {
        target = followTarget;
        worldOffset = offset;
    }
}
