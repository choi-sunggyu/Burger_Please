
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

    [Header("Layout")]
    [SerializeField] private float maxTextWidth = 480f;   // 이보다 길면 줄바꿈
    [SerializeField] private float screenMargin = 16f;    // 화면 가장자리와의 최소 간격

    private bool showRequested;

    private void Awake()
    {
        if (bubbleRect == null)
            bubbleRect = GetComponent<RectTransform>();

        // anchoredPosition은 부모 기준이므로 부모를 좌표계로 사용
        if (canvasRect == null)
            canvasRect = transform.parent as RectTransform;

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

        // 비활성 상태에선 TMP가 초기화되지 않아 크기를 잴 수 없으므로 먼저 켠다
        SetVisualActive(true);
        dialogueText.SetText(message);

        // 말풍선을 글자 크기에 맞춤 (여백은 프리팹의 텍스트 RectTransform 값을 그대로 사용)
        Vector2 textSize = dialogueText.GetPreferredValues(message, maxTextWidth, Mathf.Infinity);
        textSize.x = Mathf.Min(textSize.x, maxTextWidth);
        bubbleRect.sizeDelta = textSize - dialogueText.rectTransform.sizeDelta;

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

        // 5. 캐릭터 머리 위로 배치하되 말풍선 전체가 화면 안에 들어오게 제한
        Vector2 position = localPoint + Vector2.up * uiYOffset;
        Rect area = canvasRect.rect;
        Vector2 size = bubbleRect.rect.size;
        Vector2 pivot = bubbleRect.pivot;

        position.x = Mathf.Clamp(
            position.x,
            area.xMin + size.x * pivot.x + screenMargin,
            area.xMax - size.x * (1f - pivot.x) - screenMargin
        );
        position.y = Mathf.Clamp(
            position.y,
            area.yMin + size.y * pivot.y + screenMargin,
            area.yMax - size.y * (1f - pivot.y) - screenMargin
        );

        bubbleRect.anchoredPosition = position;

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
