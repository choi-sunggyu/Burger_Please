
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCartInteractor : MonoBehaviour
{
    [Tooltip("터치 기기에서 화면 오른쪽 탭으로 잡기/놓기 (왼쪽은 조이스틱)")]
    [SerializeField] private bool tapRightSideToGrab = true;

    private CartController nearbyCart;
    private CartController grabbedCart;
    private GrabAction grab; // 카트를 잡고 놓을 때 손 ↔ 짐칸 전환을 알려줄 대상

    public bool IsPushing => grabbedCart != null && grabbedCart.IsGrabbed;

    // 지금 밀고 있는 카트의 짐칸 (밀고 있지 않거나 짐칸이 없으면 null)
    public CartCargo PushingCargo => IsPushing ? grabbedCart.Cargo : null;

    private void Awake()
    {
        grab = GetComponentInChildren<GrabAction>();
    }

    // 카트 이동 속도 비율 (0~1), 걷기 애니메이션용
    public float PushSpeed01 => IsPushing ? grabbedCart.Speed01 : 0f;

    private void Update()
    {
        // 카트가 비활성화되는 등 외부에서 놓아진 경우 정리
        if (grabbedCart != null && !grabbedCart.IsGrabbed)
        {
            grabbedCart = null;
            if (grab != null) grab.OnCartReleased();
        }

        if (WasGrabPressed())
            ToggleGrab();
    }

    private bool WasGrabPressed()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        if (!tapRightSideToGrab || Touchscreen.current == null)
            return false;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.press.wasPressedThisFrame &&
                touch.position.ReadValue().x > Screen.width * 0.5f)
                return true;
        }

        return false;
    }

    public void ToggleGrab()
    {
        // 이미 잡은 상태면 해제
        if (grabbedCart != null)
        {
            grabbedCart.Release();
            grabbedCart = null;
            if (grab != null) grab.OnCartReleased();

            Debug.Log("카트 놓기");
            return;
        }

        // 근처 카트가 없다면 무시
        if (nearbyCart == null)
            return;

        // 손에 든 물건을 짐칸으로 옮길 수 없으면(종류가 다르거나 자리가 부족) 카트를 잡지 않는다
        if (grab != null && !grab.CanMoveHandTo(nearbyCart.Cargo))
        {
            Debug.Log("손에 든 물건을 카트에 실을 수 없어서 카트를 잡지 않음");
            return;
        }

        if (nearbyCart.TryGrab(transform))
        {
            grabbedCart = nearbyCart;
            if (grab != null) grab.OnCartGrabbed(nearbyCart.Cargo);
            Debug.Log("카트 잡기");
        }
    }

    public void SetNearbyCart(CartController cart)
    {
        nearbyCart = cart;
    }

    public void ClearNearbyCart(CartController cart)
    {
        if (nearbyCart == cart)
            nearbyCart = null;
    }

    // 원본 이동 입력 (x = A/D 회전, y = W/S 전진/후진), walkSpeed = 플레이어 걷기 속도
    public void UpdateCartInput(Vector2 moveInput, float walkSpeed)
    {
        if (IsPushing)
            grabbedCart.SetInput(moveInput, walkSpeed);
    }
}
