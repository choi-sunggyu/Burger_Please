using System.Collections.Generic;
using UnityEngine;

// 씬에 하나: 일 목록과 "누가 맡았는지"를 한 곳에서 관리
public class JobBoard : MonoBehaviour
{
    [SerializeField] bool allHands;                              // "다 해!" 모드

    readonly List<IJob> jobs = new List<IJob>();
    readonly Dictionary<IJob, Employee> claims = new Dictionary<IJob, Employee>();

    public bool AllHands => allHands;

    public void Register(IJob job) { if (!jobs.Contains(job)) jobs.Add(job); }

    public void Unregister(IJob job)
    {
        jobs.Remove(job);
        if (claims.Remove(job)) job.OnReleased();
    }

    // UI Toggle의 OnValueChanged(bool)에 연결
    public void SetAllHands(bool on) => allHands = on;

    // 지금 이 직원에게 적용할 역할 (원래 역할은 건드리지 않음)
    JobRole EffectiveRoles(Employee worker) => allHands ? JobRole.All : worker.Roles;

    public bool TryClaim(Employee worker, out IJob job)
    {
        JobRole roles = EffectiveRoles(worker);

        foreach (IJob candidate in jobs)
        {
            if ((roles & candidate.Role) == 0) continue;   // 내 역할이 아님
            if (claims.ContainsKey(candidate)) continue;    // 다른 직원이 이미 맡음
            if (!candidate.IsStillNeeded) continue;         // 지금은 할 필요 없음

            claims[candidate] = worker;                     // 확인과 동시에 차지
            candidate.OnAssigned(worker);
            job = candidate;
            return true;
        }

        job = null;
        return false;
    }

    public void Release(IJob job)
    {
        if (job != null && claims.Remove(job)) job.OnReleased();   // 두 번 반납돼도 OnReleased는 한 번만
    }
}
