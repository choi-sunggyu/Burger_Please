using System.Collections;
using UnityEngine;

// TashBox 프리팹의 트리거 영역(DropArea)에 붙이는 스크립트
// 받은 쓰레기를 포물선으로 dropPoint 까지 날려 보낸 뒤 파괴한다.
public class TrashBox : MonoBehaviour
{
    [SerializeField] private Transform dropPoint;      // 쓰레기가 들어갈 위치 (비우면 자기 위치)
    [SerializeField] private float jumpHeight = 1.5f;  // 포물선 꼭대기 높이 (출발-도착 직선 기준)
    [SerializeField] private float duration = 0.4f;    // 날아가는 시간(초)

    private void Awake()
    {
        if (dropPoint == null) dropPoint = transform;
    }

    public void Receive(Stuff trash)
    {
        StartCoroutine(JumpAndDestroy(trash.transform));
    }

    private IEnumerator JumpAndDestroy(Transform trash)
    {
        Vector3 start = trash.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;              // 0 → 1 로 진행
            float clamped = Mathf.Clamp01(t);

            // 1) 출발점 → 도착점 직선 위의 위치
            Vector3 pos = Vector3.Lerp(start, dropPoint.position, clamped);

            // 2) 높이 더하기: 4h·t·(1-t) 는 t=0,1 에서 0, t=0.5 에서 h 가 되는 포물선
            pos.y += 4f * jumpHeight * clamped * (1f - clamped);

            trash.position = pos;
            yield return null;
        }

        Destroy(trash.gameObject);
        Debug.Log("Trash thrown away!");
    }
}
