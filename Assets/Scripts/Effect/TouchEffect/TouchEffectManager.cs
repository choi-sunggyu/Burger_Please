
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// 모바일 터치 감지 스크립트
public class TouchEffectManager : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TouchEffect effectPrefab;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        // 모바일 멀티터치 처리
        foreach (var touch in Touch.activeTouches) // 터치감지
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began) // 유니티엔진의 터치 감지와 같은 단계일때
            {
                SpawnEffect(touch.screenPosition); // 해당 위치에 이펙트 불러오기
            }
        }

        // 에디터에서는 마우스 클릭으로 테스트
#if UNITY_EDITOR
        if (Touchscreen.current == null &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnEffect(Mouse.current.position.ReadValue());
        }
#endif
    }

    private void SpawnEffect(Vector2 screenPosition)
    {
        // 화면 좌표를 Canvas 내부 좌표로 변환
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out Vector2 localPosition))
        {
            return;
        }

        // 이펙트 생성
        TouchEffect effect = Instantiate(effectPrefab, canvasRect);

        RectTransform rect = effect.GetComponent<RectTransform>();
        rect.anchoredPosition = localPosition;
    }
}
