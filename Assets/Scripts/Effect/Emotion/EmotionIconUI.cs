
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EmotionIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector2 screenOffset = new(0, 20);

    private RectTransform rect;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private Camera targetCamera;

    private Transform target;
    private Vector3 worldOffset;

    private Coroutine animationRoutine;
    private Action<EmotionIconUI> onFinished;

    private float animationAlpha;

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

        animationAlpha = 0f;
        rect.localScale = Vector3.one * 0.5f;

        animationRoutine = StartCoroutine(
            AnimateRoutine(Mathf.Max(0.4f, duration))
        );
    }

    private void LateUpdate()
    {
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

        Vector3 screenPoint =
            targetCamera.WorldToScreenPoint(
                target.position + worldOffset
            );

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
                rect.anchoredPosition =
                    localPoint + screenOffset;
            else
                visible = false;
        }

        canvasGroup.alpha =
            visible ? animationAlpha : 0f;
    }

    private IEnumerator AnimateRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 등장 0.15초, 퇴장 0.25초
            float fadeIn =
                Mathf.Clamp01(elapsed / 0.15f);

            float fadeOut =
                Mathf.Clamp01((duration - elapsed) / 0.25f);

            animationAlpha = Mathf.Min(fadeIn, fadeOut);

            // Pop: 작게 등장 -> 확대 -> 원래 크기
            float scale;

            if (elapsed < 0.18f)
            {
                float t = Mathf.Clamp01(elapsed / 0.18f);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                scale = Mathf.Lerp(0.5f, 1.15f, eased);
            }
            else
            {
                float t =
                    Mathf.Clamp01((elapsed - 0.18f) / 0.12f);

                scale = Mathf.Lerp(1.15f, 1f, t);
            }

            rect.localScale = Vector3.one * scale;

            yield return null;
        }

        Complete();
    }

    private void Complete()
    {
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
        animationAlpha = 0f;

        canvasGroup.alpha = 0f;
        rect.localScale = Vector3.one;

        gameObject.SetActive(false);
    }
}
