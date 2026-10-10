using UnityEngine;

// 드라이브 스루 도로. 웨이포인트를 순서대로 이은 선 위에서
// "시작점부터 몇 m" 라는 거리 값 하나로 위치를 계산한다.
public class DriveThruPath : MonoBehaviour
{
    [SerializeField] Transform[] points;   // 도로를 따라 순서대로 (첫 점 = 진입, 마지막 점 = 퇴장)

    float[] cumulative;                    // 각 점까지의 누적 거리
    public float Length { get; private set; }

    void Awake() => Build();

    bool IsValid()
    {
        if (points == null || points.Length < 2) return false;
        foreach (var p in points) if (p == null) return false;
        return true;
    }

    // 누적 거리 계산. 에디터에서 점을 옮길 때도 Gizmo가 다시 호출함
    public void Build()
    {
        if (!IsValid()) return;
        cumulative = new float[points.Length];
        for (int i = 1; i < points.Length; i++)
            cumulative[i] = cumulative[i - 1] + Vector3.Distance(points[i - 1].position, points[i].position);
        Length = cumulative[points.Length - 1];
    }

    // 시작점에서 distance 만큼 도로를 따라간 위치
    public Vector3 GetPoint(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, Length);
        for (int i = 1; i < points.Length; i++)
        {
            if (distance > cumulative[i]) continue;
            float t = Mathf.InverseLerp(cumulative[i - 1], cumulative[i], distance);
            return Vector3.Lerp(points[i - 1].position, points[i].position, t);
        }
        return points[points.Length - 1].position;
    }

    // 그 지점에서 차가 바라볼 방향 (수평)
    public Vector3 GetDirection(float distance)
    {
        Vector3 dir = GetPoint(distance + 0.1f) - GetPoint(distance - 0.1f);
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
    }

    // 임의의 위치에서 가장 가까운 도로 위 지점의 거리 값 (창구 위치 → 거리 변환용)
    public float GetClosestDistance(Vector3 position)
    {
        float bestDistance = 0f, bestSqr = float.MaxValue;
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 a = points[i - 1].position, ab = points[i].position - a;
            float t = Mathf.Clamp01(Vector3.Dot(position - a, ab) / ab.sqrMagnitude);
            float sqr = (position - (a + ab * t)).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                bestDistance = cumulative[i - 1] + ab.magnitude * t;
            }
        }
        return bestDistance;
    }

    void OnDrawGizmos()
    {
        if (!IsValid()) return;
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.color = i == 0 ? Color.green : (i == points.Length - 1 ? Color.red : Color.magenta);
            Gizmos.DrawWireSphere(points[i].position, 0.3f);
            if (i > 0)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(points[i - 1].position, points[i].position);
            }
        }
    }
}