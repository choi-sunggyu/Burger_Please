
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CartGrabTrigger : MonoBehaviour
{
    [SerializeField] private CartController cart;

    private void Awake()
    {
        if (cart == null)
            cart = GetComponentInParent<CartController>();

        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerCartInteractor p))
            return;

        p.SetNearbyCart(cart);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PlayerCartInteractor p))
            return;

        p.ClearNearbyCart(cart);
    }
}
