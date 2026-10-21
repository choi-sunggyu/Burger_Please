
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchEffect : MonoBehaviour
{
    [SerializeField] private Image effectImage; //이미지 넣고
    [SerializeField] private float duration = 0.35f; //몇초동안 할거냐
    [SerializeField] private float startScale = 0.5f; //처음 크기
    [SerializeField] private float endScale = 1.8f; //마지막 크기

    private RectTransform rectTransform; //터치 받을 위치

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>(); //호출받을 때마다 터치 위치 받아옴
    }

    private void OnEnable()
    {
        StartCoroutine(PlayEffect()); //코루틴으로 PlayEffect 실행
    }

    private IEnumerator PlayEffect()
    {
        float elapsed = 0f; //이펙트 시간 실시간 측정
        Color originalColor = effectImage.color; //이펙트 색깔

        while (elapsed < duration) // 설정한 시간동안
        {
            elapsed += Time.unscaledDeltaTime; //시간 흐름대로 넣고

            float t = Mathf.Clamp01(elapsed / duration); //전달받은 실수(float) 값을 0과 1 사이의 범위로 제한(Clamp)하여 반환하는 메서드

            float scale = Mathf.Lerp(startScale, endScale, t); //Mathf.Lerp(a, b, t)는 시작값(a)과 끝값(b) 사이를 비율(t)에 따라 선형 보간(Linear Interpolation)하여 그 사이의 값을 구하는 함수
            rectTransform.localScale = Vector3.one * scale; //UI(Canvas) 요소의 상대적 확대 및 축소 배율에 Vector3.one은 (1, 1, 1)에 scale 곱

            Color color = originalColor;
            color.a = originalColor.a * (1f - t);
            effectImage.color = color;

            yield return null;
        }

        Destroy(gameObject); //설정된 시간 지나면 삭제, 페이드 아웃할지는 미정
    }
}
