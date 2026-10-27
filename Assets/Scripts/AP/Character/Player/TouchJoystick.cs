using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// 플로팅 조이스틱: 화면 아무 곳이나 한 손가락으로 누르고 드래그하면 그 방향으로 이동 입력
// 손가락이 두 개가 되면 카메라 제스처(줌/회전/틸트)에 양보하고 입력 해제
public class TouchJoystick : MonoBehaviour
{
    [SerializeField] private float radius = 80f;                      // 최대 드래그 거리 (캔버스 단위)
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.1f;
    [SerializeField] private bool simulateTouchInEditor = false;       // 에디터에서 마우스를 터치로 사용

    [Header("Visual (선택, Screen Space - Overlay 캔버스)")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    private Finger activeFinger;
    private Vector2 origin;
    private Canvas canvas;

    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointerData;

    // -1 ~ 1, 크기는 드래그 정도
    public Vector2 Value { get; private set; }

    private float PixelRadius => radius * (canvas != null ? canvas.scaleFactor : Screen.height / 1080f);

    private void Awake()
    {
        if (background != null)
            canvas = background.GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        if (simulateTouchInEditor && Application.isEditor)
            TouchSimulation.Enable();

        SetVisible(false);
    }

    private void OnDisable()
    {
        Release();

        if (simulateTouchInEditor && Application.isEditor)
            TouchSimulation.Disable();

        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        var touches = Touch.activeTouches;

        // 0개 = 손 뗌, 2개 이상 = 카메라 제스처
        if (touches.Count != 1)
        {
            Release();
            return;
        }

        Touch touch = touches[0];

        if (activeFinger == null)
        {
            // 두 손가락 제스처 후 남은 손가락으로는 시작하지 않음 (갑자기 움직이는 것 방지)
            if (touch.phase != TouchPhase.Began || IsOverUI(touch.screenPosition))
                return;

            activeFinger = touch.finger;
            origin = touch.screenPosition;
            SetVisible(true);
        }

        if (touch.finger != activeFinger ||
            touch.phase == TouchPhase.Ended ||
            touch.phase == TouchPhase.Canceled)
        {
            Release();
            return;
        }

        float pixelRadius = PixelRadius;
        Vector2 offset = Vector2.ClampMagnitude(touch.screenPosition - origin, pixelRadius);
        Vector2 value = offset / pixelRadius;

        Value = value.magnitude < deadZone ? Vector2.zero : value;

        if (background != null) background.position = origin;
        if (handle != null) handle.position = origin + offset;
    }

    private void Release()
    {
        activeFinger = null;
        Value = Vector2.zero;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (background != null) background.gameObject.SetActive(visible);
        if (handle != null) handle.gameObject.SetActive(visible);
    }

    // 버튼 등 UI 위에서 시작한 터치는 이동으로 쓰지 않음
    private bool IsOverUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        pointerData ??= new PointerEventData(eventSystem);
        pointerData.position = screenPosition;

        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            // 조이스틱 자체 그래픽은 제외
            if (result.gameObject.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }
}
