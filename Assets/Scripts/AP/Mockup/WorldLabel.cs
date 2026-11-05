using TMPro;
using UnityEngine;

// 대상 위에 떠서 항상 카메라를 보는 글자 (트럭/적재함 수량 표시)
[RequireComponent(typeof(TMP_Text))]
public class WorldLabel : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 50f, 0f);

    private TMP_Text text;
    private Camera cam;

    public void SetText(string value)
    {
        if (text == null) text = GetComponent<TMP_Text>();
        text.text = value;
    }

    private void LateUpdate()
    {
        if (target != null) transform.position = target.position + offset;

        if (cam == null) cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}
