using UnityEngine;

public enum StuffType
{
    Cube,
    Trash,
    Money,
    CubePack
}

public class Stuff : MonoBehaviour
{
    public StuffType Type { get; protected set; }

    private Collider col;

    // 바닥/더미에 놓여 있어 주울 수 있는 상태인가
    // (손에 들었거나 CubeSpot·손님에게 넘어간 것은 GrabAction 이 콜라이더를 꺼 둠 → 예전 트리거 감지와 같은 기준)
    public bool IsLoose
    {
        get
        {
            if (col == null) TryGetComponent(out col);
            return col != null && col.enabled && CompareTag("Stuff");
        }
    }

    // Carry 가 거리로 찾을 수 있도록 등록 (자식 클래스는 Awake만 쓰므로 여기서 처리)
    protected virtual void OnEnable() => Registry<Stuff>.Add(this);
    protected virtual void OnDisable() => Registry<Stuff>.Remove(this);
}
