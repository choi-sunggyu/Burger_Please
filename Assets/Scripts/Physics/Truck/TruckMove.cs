using System;
using System.Collections;
using UnityEngine;

// 트럭 들어오기/나가기: 씬에 놓인 위치를 도크로 보고, 바깥 지점과 도크 사이를 오간다
public class TruckMove : MonoBehaviour
{
    [SerializeField] private Vector3 outDirection = Vector3.back;   // 도크에서 나가는 방향 (월드)
    [SerializeField] private float travelDistance = 500f;           // 도크에서 바깥 지점까지 거리
    [SerializeField] private float travelTime = 3f;                 // 한 번 이동에 걸리는 시간(초)

    private Vector3 dockPosition;

    public Vector3 OutDirection => outDirection.normalized;
    public bool IsDocked { get; private set; } = true;

    private void Awake()
    {
        dockPosition = transform.position;
    }

    // 바깥 지점에 바로 가져다 둠 (게임 시작 시 들어오는 모습부터 보이도록)
    public void PlaceAway()
    {
        IsDocked = false;
        transform.position = AwayPosition;
    }

    public void Enter(Action onDocked)
    {
        StartCoroutine(Drive(AwayPosition, dockPosition, () =>
        {
            IsDocked = true;
            onDocked?.Invoke();
        }));
    }

    public void Leave(Action onGone)
    {
        IsDocked = false;
        StartCoroutine(Drive(dockPosition, AwayPosition, onGone));
    }

    private Vector3 AwayPosition => dockPosition + OutDirection * travelDistance;

    private IEnumerator Drive(Vector3 from, Vector3 to, Action onFinished)
    {
        for (float t = 0f; t < travelTime; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / travelTime));
            yield return null;
        }

        transform.position = to;
        onFinished?.Invoke();
    }
}
