using UnityEngine;

/// <summary>
/// 지면/장애물 판정 설정. PoolManager(스폰)와 RabbitWander(이동)가 같은 기준을 쓴다.
/// ※ 지면과 장애물, 토끼는 서로 다른 레이어에 두는 것을 전제로 한다.
/// </summary>
[System.Serializable]
public class GroundSettings
{
    public LayerMask groundMask = 1;           // 걸을 수 있는 바닥 (기본: Default)
    public LayerMask obstacleMask = 0;         // 벽, 바위, 나무 등
    [Range(0f, 89f)] public float maxSlope = 40f;
    public float clearance = 0.4f;             // 스폰 시 장애물과의 최소 여유 반지름
    public float rayHeight = 20f;              // 스폰 레이를 쏘기 시작하는 높이

    public bool IsWalkable(Vector3 normal) => Vector3.Angle(normal, Vector3.up) <= maxSlope;
}

public static class GroundSampler
{
    /// <summary>
    /// center 기준 반경 radius 원 안에서 "바닥이 있고 / 너무 가파르지 않고 / 장애물과 겹치지 않는" 점을 찾는다.
    /// </summary>
    public static bool TryGetPoint(Vector3 center, float radius, GroundSettings s,
                                   out Vector3 result, int maxTries = 30)
    {
        for (int i = 0; i < maxTries; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 origin = center + new Vector3(offset.x, s.rayHeight, offset.y);

            // 1) 위에서 아래로 쏴서 바닥 찾기
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, s.rayHeight * 2f,
                                 s.groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // 2) 경사 체크
            if (!s.IsWalkable(hit.normal))
                continue;

            // 3) 그 자리에 장애물이 겹치는지 체크
            Vector3 checkPos = hit.point + Vector3.up * (s.clearance + 0.05f);
            if (Physics.CheckSphere(checkPos, s.clearance, s.obstacleMask, QueryTriggerInteraction.Ignore))
                continue;

            result = hit.point;
            return true;
        }

        result = center;
        return false;
    }
}