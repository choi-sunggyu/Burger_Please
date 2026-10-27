using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VehicleDriveSettings
{
    public float maxSpeed = 8f;
    public float speedVariance = 0.15f;
    public float uTurnSpeed = 3f;
    public float acceleration = 5f;
    public float braking = 12f;
    // 앞차와 유지할 최소 거리 (차량 중심 기준)
    public float minGap = 7f;
    // 이 거리부터 감속 시작 (교차로 진입 요청 거리이기도 함)
    public float slowDownGap = 14f;
    // 방향을 계산할 때 앞쪽을 바라보는 거리 (회전 부드럽게)
    public float lookAhead = 2f;
    // 모델의 정면이 +Z가 아닐 경우 보정
    public float modelYawOffset = 0f;
}

// 왕복 2차선 도로 하나
// 우측 차선 정방향 -> 끝에서 유턴 -> 좌측 차선 역방향 -> 시작점에서 유턴
// 으로 이어지는 폐곡선 위에서 차량들이 일렬로 이동한다.
// 추월이 없으므로 앞차와의 거리만 유지하면 같은 도로 안에서는 충돌하지 않고,
// 다른 도로와 겹치는 구간(교차로)은 TrafficZone으로 진입을 제어한다.
public class TrafficRoute
{
    private struct Span
    {
        public TrafficZone zone;
        public float enter;
        public float length;
    }

    private class Vehicle
    {
        public Transform transform;
        public float distance;
        public float speed;
        public float maxSpeed;
        public int nextSpan;
        public bool holding;
        public bool inside;
    }

    private readonly List<Vector3> points = new();
    private readonly List<bool> turnFlags = new();
    private readonly List<float> distances = new();
    private readonly List<Span> spans = new();
    private readonly List<Vehicle> vehicles = new();

    public float Length { get; private set; }
    public int VehicleCount => vehicles.Count;

    public TrafficRoute(Vector3 start, Vector3 end, float laneOffset, int uTurnSegments)
    {
        BuildLoop(start, end, laneOffset, uTurnSegments, points, turnFlags);

        int count = points.Count;
        for (int i = 0; i < count; i++)
        {
            distances.Add(Length);
            Length += Vector3.Distance(points[i], points[(i + 1) % count]);
        }
    }

    public static void BuildLoop(
        Vector3 start, Vector3 end, float laneOffset, int uTurnSegments,
        List<Vector3> outPoints, List<bool> outTurns)
    {
        outPoints.Clear();
        outTurns.Clear();

        Vector3 dir = end - start;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;

        dir.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, dir) * laneOffset;

        // 정방향 (우측 차선)
        AddPoint(start + right, false, outPoints, outTurns);
        AddPoint(end + right, false, outPoints, outTurns);
        AddUTurn(end, dir, laneOffset, uTurnSegments, outPoints, outTurns);

        // 역방향 (좌측 차선)
        AddPoint(end - right, false, outPoints, outTurns);
        AddPoint(start - right, false, outPoints, outTurns);
        AddUTurn(start, -dir, laneOffset, uTurnSegments, outPoints, outTurns);
    }

    private static void AddPoint(Vector3 point, bool turn, List<Vector3> outPoints, List<bool> outTurns)
    {
        outPoints.Add(point);
        outTurns.Add(turn);
    }

    // 진행 방향 기준 우측 차선 -> 좌측 차선으로 넘어가는 반원
    private static void AddUTurn(
        Vector3 center, Vector3 dir, float radius, int segments,
        List<Vector3> outPoints, List<bool> outTurns)
    {
        Vector3 right = Vector3.Cross(Vector3.up, dir);

        for (int k = 1; k < segments; k++)
        {
            float angle = Mathf.PI * k / segments;
            Vector3 offset = (right * Mathf.Cos(angle) + dir * Mathf.Sin(angle)) * radius;
            AddPoint(center + offset, true, outPoints, outTurns);
        }
    }

    public Rect GetArea(float padding)
    {
        float minX = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxZ = float.MinValue;

        foreach (Vector3 p in points)
        {
            minX = Mathf.Min(minX, p.x);
            minZ = Mathf.Min(minZ, p.z);
            maxX = Mathf.Max(maxX, p.x);
            maxZ = Mathf.Max(maxZ, p.z);
        }

        return Rect.MinMaxRect(minX - padding, minZ - padding, maxX + padding, maxZ + padding);
    }

    // 경로를 따라가며 교차로 영역을 지나는 구간을 기록
    public void BuildSpans(List<TrafficZone> zones, float step)
    {
        spans.Clear();
        if (Length <= 0f)
            return;

        TrafficZone current = null;
        float enter = 0f;

        for (float s = 0f; s < Length; s += step)
        {
            TrafficZone zone = null;
            Vector3 p = Evaluate(s);

            foreach (TrafficZone z in zones)
            {
                if (z.Contains(p))
                {
                    zone = z;
                    break;
                }
            }

            if (zone == current)
                continue;

            if (current != null)
                spans.Add(new Span { zone = current, enter = enter, length = s - enter });

            current = zone;
            enter = s;
        }

        if (current != null)
        {
            // 시작점(0)에 걸친 구간이면 첫 구간과 합침
            if (spans.Count > 0 && spans[0].zone == current && spans[0].enter == 0f)
            {
                Span first = spans[0];
                spans[0] = new Span { zone = current, enter = enter, length = Length - enter + first.length };
            }
            else
            {
                spans.Add(new Span { zone = current, enter = enter, length = Length - enter });
            }
        }

        spans.Sort((a, b) => a.enter.CompareTo(b.enter));
    }

    // 교차로 밖에 일정 간격으로 차량 배치, 실제 배치된 수 반환
    public int Spawn(int count, Func<Transform> create, VehicleDriveSettings drive)
    {
        if (count <= 0 || Length <= 0f)
            return 0;

        float spacing = Mathf.Max(drive.minGap, Length / count);
        float first = PushOutOfSpans(0f);
        float s = first;

        int spawned = 0;
        while (spawned < count && s + drive.minGap <= Length + first)
        {
            Vehicle v = new Vehicle
            {
                transform = create(),
                distance = Mathf.Repeat(s, Length),
                speed = 0f,
                maxSpeed = drive.maxSpeed *
                    (1f + UnityEngine.Random.Range(-drive.speedVariance, drive.speedVariance)),
            };

            v.nextSpan = FindNextSpan(v.distance);
            ApplyTransform(v, drive);
            vehicles.Add(v);
            spawned++;

            s = PushOutOfSpans(s + spacing);
        }

        // 리스트 순서 = 경로상 순서 (앞차 = 다음 인덱스)
        vehicles.Sort((a, b) => a.distance.CompareTo(b.distance));
        return spawned;
    }

    // destroy = false 이면 차량 오브젝트는 남겨두고 경로에서만 제외 (씬에 배치된 차량 재사용 시)
    public void Clear(bool destroy)
    {
        foreach (Vehicle v in vehicles)
        {
            if (v.holding)
                spans[v.nextSpan].zone.Exit();

            if (destroy && v.transform != null)
                UnityEngine.Object.Destroy(v.transform.gameObject);
        }

        vehicles.Clear();
    }

    public void Simulate(float dt, VehicleDriveSettings drive)
    {
        int count = vehicles.Count;
        if (count == 0)
            return;

        float brakeDistance =
            (drive.maxSpeed * drive.maxSpeed - drive.uTurnSpeed * drive.uTurnSpeed) / (2f * drive.braking);

        for (int i = 0; i < count; i++)
        {
            Vehicle v = vehicles[i];

            // 앞차와의 거리
            float gap = Length;
            if (count > 1)
                gap = Mathf.Repeat(vehicles[(i + 1) % count].distance - v.distance, Length);

            if (spans.Count > 0)
                gap = Mathf.Min(gap, UpdateZone(v, gap, drive));

            float targetSpeed = v.maxSpeed;

            // 유턴 구간이거나 곧 유턴이면 감속
            if (IsTurnAt(v.distance) || IsTurnAt(v.distance + brakeDistance))
                targetSpeed = Mathf.Min(targetSpeed, drive.uTurnSpeed);

            if (gap < drive.slowDownGap)
            {
                float t = Mathf.InverseLerp(drive.minGap, drive.slowDownGap, gap);
                targetSpeed = Mathf.Min(targetSpeed, v.maxSpeed * t);
            }

            float rate = targetSpeed < v.speed ? drive.braking : drive.acceleration;
            v.speed = Mathf.MoveTowards(v.speed, targetSpeed, rate * dt);

            // 어떤 경우에도 최소 간격 안으로는 들어가지 않음
            float move = Mathf.Min(v.speed * dt, Mathf.Max(0f, gap - drive.minGap));
            v.distance = Mathf.Repeat(v.distance + move, Length);

            ApplyTransform(v, drive);
        }
    }

    // 교차로 진입/진출 처리, 진입 불가 시 교차로 입구까지의 거리를 gap 형태로 반환
    private float UpdateZone(Vehicle v, float gap, VehicleDriveSettings drive)
    {
        Span span = spans[v.nextSpan];

        if (v.holding)
        {
            float passed = Mathf.Repeat(v.distance - span.enter, Length);

            if (passed < span.length)
            {
                v.inside = true;
                return float.MaxValue;
            }

            if (!v.inside)
                return float.MaxValue;

            // 교차로 통과 완료
            span.zone.Exit();
            v.holding = false;
            v.inside = false;
            v.nextSpan = (v.nextSpan + 1) % spans.Count;
            span = spans[v.nextSpan];
        }

        float toEnter = Mathf.Repeat(span.enter - v.distance, Length);
        if (toEnter > drive.slowDownGap)
            return float.MaxValue;

        // 교차로 안에서 멈추지 않도록, 통과 후 공간이 있을 때만 진입
        bool fits = gap - drive.minGap >= toEnter + span.length;

        if (fits && span.zone.TryEnter(this))
        {
            v.holding = true;
            return float.MaxValue;
        }

        return toEnter + drive.minGap;
    }

    private float PushOutOfSpans(float s)
    {
        for (int iteration = 0; iteration < spans.Count + 1; iteration++)
        {
            bool moved = false;

            foreach (Span span in spans)
            {
                float passed = Mathf.Repeat(s - span.enter, Length);
                if (passed < span.length)
                {
                    s += span.length - passed + 0.5f;
                    moved = true;
                }
            }

            if (!moved)
                break;
        }

        return s;
    }

    private int FindNextSpan(float distance)
    {
        int best = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < spans.Count; i++)
        {
            float d = Mathf.Repeat(spans[i].enter - distance, Length);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }

        return best;
    }

    private void ApplyTransform(Vehicle v, VehicleDriveSettings drive)
    {
        Vector3 pos = Evaluate(v.distance);
        Vector3 aim = Evaluate(v.distance + drive.lookAhead) - pos;
        aim.y = 0f;

        v.transform.position = pos;

        if (aim.sqrMagnitude > 0.0001f)
        {
            v.transform.rotation =
                Quaternion.LookRotation(aim) * Quaternion.Euler(0f, drive.modelYawOffset, 0f);
        }
    }

    // distance 위치가 속한 구간의 시작 인덱스
    private int SegmentAt(float distance)
    {
        int lo = 0;
        int hi = distances.Count - 1;

        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (distances[mid] <= distance)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }

    private Vector3 Evaluate(float distance)
    {
        distance = Mathf.Repeat(distance, Length);

        int i = SegmentAt(distance);
        int next = (i + 1) % points.Count;

        float segStart = distances[i];
        float segEnd = next == 0 ? Length : distances[next];
        float t = Mathf.InverseLerp(segStart, segEnd, distance);

        return Vector3.Lerp(points[i], points[next], t);
    }

    private bool IsTurnAt(float distance)
    {
        distance = Mathf.Repeat(distance, Length);

        int i = SegmentAt(distance);
        return turnFlags[i] || turnFlags[(i + 1) % turnFlags.Count];
    }
}
