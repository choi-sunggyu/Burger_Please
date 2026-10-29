
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EmotionIconUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector2 screenOffset = new(0, 20);

    [Header("Pop Animation")]
    [SerializeField, Range(0.25f, 3f)]
    private float popSpeed = 1f;

    [SerializeField] private float bounceHeight = 18f;
    [SerializeField] private float restingHeight = 10f;

    private RectTransform rect;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private Camera targetCamera;

    private Transform target;
    private Vector3 worldOffset;

    private Coroutine animationRoutine;
    private Action<EmotionIconUI> onFinished;

    private float animatedYOffset;
    private float animationAlpha;
    private bool isPlaying;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
    }

    public void Play(
        Transform followTarget,
        Vector3 offset,
        Sprite sprite,
        float duration,
        RectTransform root,
        Camera camera,
        Action<EmotionIconUI> completed)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        gameObject.SetActive(true);

        target = followTarget;
        worldOffset = offset;
        canvasRect = root;
        targetCamera = camera;
        onFinished = completed;

        if (iconImage == null)
        {
            Debug.LogError("EmotionIconUI: IconImage가 없습니다.");
            Complete();
            return;
        }

        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;

        // 풀링 재사용 시 초기화
        animatedYOffset = 0f;
        animationAlpha = 0f;

        rect.localScale = new Vector3(0.35f, 0.2f, 1f);
        canvasGroup.alpha = 0f;

        isPlaying = true;

        animationRoutine = StartCoroutine(
            AnimateRoutine(Mathf.Max(0.4f, duration))
        );
    }

    private void LateUpdate()
    {
        if (!isPlaying)
            return;

        if (target == null)
        {
            Complete();
            return;
        }

        if (targetCamera == null || canvasRect == null)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        // 캐릭터 머리 위 월드 좌표
        Vector3 worldPosition =
            target.position + worldOffset;

        // 월드 -> 스크린 좌표
        Vector3 screenPoint =
            targetCamera.WorldToScreenPoint(worldPosition);

        bool visible =
            screenPoint.z > 0f &&
            screenPoint.x >= 0f &&
            screenPoint.x <= Screen.width &&
            screenPoint.y >= 0f &&
            screenPoint.y <= Screen.height;

        if (visible)
        {
            bool converted =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPoint,
                        null,
                        out Vector2 localPoint
                    );

            if (converted)
            {
                // 위치 추적 + 애니메이션 오프셋
                rect.anchoredPosition =
                    localPoint +
                    screenOffset +
                    Vector2.up * animatedYOffset;
            }
            else
            {
                visible = false;
            }
        }

        canvasGroup.alpha =
            visible ? animationAlpha : 0f;
    }

    
    private IEnumerator AnimateRoutine(float duration)
    {
        float elapsed = 0f;
        float popElapsed = 0f;

        while (elapsed < duration)
        {
            // 1. Alpha (전체 표시 시간 기준)
            float fadeIn = Mathf.Clamp01(elapsed / 0.12f);
            float fadeOut = Mathf.Clamp01(
                (duration - elapsed) / 0.25f
            );

            animationAlpha = Mathf.Min(fadeIn, fadeOut);

            // 2. Squash & Stretch (Pop 속도 적용)
            Vector3 scale;

            if (popElapsed < 0.12f)
            {
                float p = popElapsed / 0.12f;
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                float sx = Mathf.Lerp(0.35f, 1.15f, eased);
                float sy = Mathf.Lerp(0.2f, 1.3f, eased);

                scale = new Vector3(sx, sy, 1f);
            }
            else if (popElapsed < 0.22f)
            {
                float p = (popElapsed - 0.12f) / 0.10f;

                float sx = Mathf.Lerp(1.15f, 1.25f, p);
                float sy = Mathf.Lerp(1.3f, 0.85f, p);

                scale = new Vector3(sx, sy, 1f);
            }
            else if (popElapsed < 0.35f)
            {
                float p = (popElapsed - 0.22f) / 0.13f;
                float eased = 1f - Mathf.Pow(1f - p, 2f);

                float sx = Mathf.Lerp(1.25f, 1f, eased);
                float sy = Mathf.Lerp(0.85f, 1f, eased);

                scale = new Vector3(sx, sy, 1f);
            }
            else
            {
                scale = Vector3.one;
            }

            rect.localScale = scale;

            // 3. Y Bounce (Pop 속도 적용)
            if (popElapsed < 0.2f)
            {
                float p = popElapsed / 0.2f;
                float eased = 1f - Mathf.Pow(1f - p, 3f);

                animatedYOffset =
                    Mathf.Lerp(0f, bounceHeight, eased);
            }
            else if (popElapsed < 0.4f)
            {
                float p = (popElapsed - 0.2f) / 0.2f;
                float eased = p * p * (3f - 2f * p);

                animatedYOffset =
                    Mathf.Lerp(bounceHeight, restingHeight, eased);
            }
            else
            {
                animatedYOffset = restingHeight;
            }

            yield return null;

            float delta = Time.deltaTime;

            // 전체 표시 시간
            elapsed += delta;

            // Pop 애니메이션 시간
            popElapsed += delta * popSpeed;
        }

        rect.localScale = Vector3.one;
        animatedYOffset = restingHeight;
        animationAlpha = 0f;

        Complete();
    }


    private void Complete()
    {
        if (!isPlaying && onFinished == null)
            return;

        isPlaying = false;
        animationRoutine = null;

        var callback = onFinished;
        onFinished = null;

        callback?.Invoke(this);
    }

    public void HideImmediately()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = null;
        onFinished = null;
        target = null;

        isPlaying = false;

        // 다음 재사용을 위한 초기화
        animatedYOffset = 0f;
        animationAlpha = 0f;

        canvasGroup.alpha = 0f;
        rect.localScale = Vector3.one;

        gameObject.SetActive(false);
    }
}
