using UnityEngine;

// 직원이 하는 "일"의 약속. JobBoard와 Employee는 이 인터페이스만 안다.
public interface IJob
{
    JobRole Role { get; }                      // 이 일의 역할
    bool IsStillNeeded { get; }                // 지금 새로 맡을 필요가 있나 (JobBoard가 배정할 때 확인)
    StuffType? WantedType { get; }             // 이 일을 하는 동안 주울 종류 (null이면 아무것도 줍지 않음)
    Transform GetTarget(Employee worker);      // 이 직원이 지금 갈 곳 (null이면 일 끝)

    void OnAssigned(Employee worker);          // JobBoard가 이 일을 직원에게 배정했을 때
    void OnReleased();                         // 일이 끝나거나 취소되어 반납됐을 때
}
