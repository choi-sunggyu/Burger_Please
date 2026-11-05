using UnityEngine;

public static class BoundsUtil
{
    // 오브젝트와 자식의 모든 Renderer를 합친 월드 범위 (FBX 피벗이 모델 중심과 달라서 이 값으로 위치를 계산함)
    public static Bounds Of(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
