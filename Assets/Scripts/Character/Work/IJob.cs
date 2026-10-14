using UnityEngine;

public interface IJob
{
    JobRole Role { get; }                      // 이 일의 역할
    bool IsStillNeeded { get; }
    StuffType? WantedType { get; }
    Transform GetTarget(Employee worker);
}