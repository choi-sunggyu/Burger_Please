using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// 왼쪽 아래 고정 조이스틱: 조이스틱 근처에서 한 손가락으로 누르고 드래그하면 이동 입력
// 손가락이 두 개가 되면 카메라 핀치 줌에 양보하고 입력 해제
// 그래픽을 지정하지 않으면 실행 시 원형 UI를 직접 만든다.
public class TouchJoystick : MonoBehaviour
{
    [SerializeField] private float radius = 120f;                      // 최대 드래그 거리 (1080p 기준 픽셀)
    [SerializeField] private Vector2 margin = new Vector2(220f, 220f); // 화면 왼쪽 아래에서 조이스틱 중심까지 (1080p 기준)
    [SerializeField] private float touchArea = 2f;                     // 중심에서 radius * touchArea 안에서 시작한 터치만 사용
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
    [SerializeField] private bool showOnDesktop = false;               // 터치스크린이 없는 환경(에디터 등)에서도 표시

    [Header("Visual (비우면 자동 생성)")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    private Finger activeFinger;

    // -1 ~ 1, 크기는 드래그 정도
    public Vector2 Value { get; private set; }

    private float Scale => Screen.height / 1080f;
    private float PixelRadius => radius * Scale;
    private Vector2 Center => margin * Scale;

    private void Awake()
    {
        if (background == null || handle == null)
            BuildVisual();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        SetVisible(Application.isMobilePlatform || showOnDesktop);
        ResetHandle();
    }

    private void OnDisable()
    {
        Release();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        var touches = Touch.activeTouches;

        // 0개 = 손 뗌, 2개 이상 = 카메라 핀치
        if (touches.Count != 1)
        {
            Release();
            return;
        }

        Touch touch = touches[0];
        Vector2 center = Center;

        if (activeFinger == null)
        {
            // 핀치 후 남은 손가락이나 조이스틱 밖에서 시작한 터치는 무시
            if (touch.phase != TouchPhase.Began ||
                Vector2.Distance(touch.screenPosition, center) > PixelRadius * touchArea)
                return;

            activeFinger = touch.finger;
        }

        if (touch.finger != activeFinger ||
            touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            Release();
            return;
        }

        float pixelRadius = PixelRadius;
        Vector2 offset = Vector2.ClampMagnitude(touch.screenPosition - center, pixelRadius);
        Vector2 value = offset / pixelRadius;

        Value = value.magnitude < deadZone ? Vector2.zero : value;
        handle.position = center + offset;
    }

    private void Release()
    {
        activeFinger = null;
        Value = Vector2.zero;
        ResetHandle();
    }

    private void ResetHandle()
    {
        if (background == null) return;
        background.position = Center;
        handle.position = Center;
    }

    private void SetVisible(bool visible)
    {
        background.gameObject.SetActive(visible);
        handle.gameObject.SetActive(visible);
    }

    // Screen Space Overlay 캔버스 + 원형 이미지 2개 (raycastTarget 끔: 다른 UI 클릭을 막지 않음)
    private void BuildVisual()
    {
        var canvasGo = new GameObject("JoystickCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        Sprite circle = CreateCircleSprite(128);
        background = CreateImage("Background", canvasGo.transform, circle, new Color(1f, 1f, 1f, 0.25f));
        handle = CreateImage("Handle", canvasGo.transform, circle, new Color(1f, 1f, 1f, 0.7f));
        background.sizeDelta = Vector2.one * PixelRadius * 2f; // CanvasScaler 기본값(픽셀 고정)이라 픽셀 = 캔버스 단위
        handle.sizeDelta = Vector2.one * PixelRadius;
    }

    private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = Vector2.zero; // position을 화면 픽셀로 직접 지정
        return rect;
    }

    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            byte a = (byte)(Mathf.Clamp01(r - d) * 255f); // 가장자리 1픽셀 안티앨리어싱
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
