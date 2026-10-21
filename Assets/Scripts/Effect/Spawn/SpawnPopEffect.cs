
using System.Collections;
using UnityEngine;

public class SpawnPopEffect : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float duration = 0.45f; // 애니메이션 지속 시간

    [Header("XZ Scale")] // Stretch 크기 조절
    [SerializeField] private AnimationCurve horizontalCurve =
        new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 0.75f),
            new Keyframe(0.65f, 1.3f),
            new Keyframe(1f, 1f)
        );

    [Header("Y Scale")] // Stretch 크기 조절
    [SerializeField] private AnimationCurve verticalCurve =
        new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 1.5f),
            new Keyframe(0.65f, 0.7f),
            new Keyframe(1f, 1f)
        );

    private Vector3 originalScale;
    private Coroutine animationRoutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void Play()
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation() // IEnumerator : C#의 반복자 인터페이스이며, 비동기처럼 특정 작업을 여러 프레임에 걸쳐 나누어 실행하는 코루틴(Coroutine)을 만들 때 반환 타입으로 사용
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float horizontal = horizontalCurve.Evaluate(t);
            float vertical = verticalCurve.Evaluate(t);

            transform.localScale = new Vector3(
                originalScale.x * horizontal,
                originalScale.y * vertical,
                originalScale.z * horizontal
            );

            yield return null;
        }

        transform.localScale = originalScale;
        animationRoutine = null;
    }
}
