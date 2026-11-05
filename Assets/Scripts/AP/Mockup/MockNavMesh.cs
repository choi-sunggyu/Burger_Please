using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 목업 시작 시 창고 바닥 높이에 평평한 NavMesh를 메모리에 만든다 (에셋을 만들거나 덮어쓰지 않음)
// 씬 스케일이 커서 복셀을 크게 잡아 빠르게 굽는다. 선반은 ShelfStorage의 NavMeshObstacle이 뚫어 준다
public class MockNavMesh : MonoBehaviour
{
    private const float SourceThickness = 1f;

    [SerializeField] private float floorHeight;                                    // 바닥 윗면의 월드 Y (Setup 메뉴가 바닥 메쉬에서 읽어 옴)
    [SerializeField] private Vector3 areaCenter = new Vector3(0f, 0f, -300f);      // XZ만 사용
    [SerializeField] private Vector3 areaSize = new Vector3(1400f, 100f, 1600f);
    [SerializeField] private float voxelSize = 1f;

    private NavMeshDataInstance instance;

    private void Awake()
    {
        var source = new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(areaSize.x, SourceThickness, areaSize.z),
            transform = Matrix4x4.Translate(new Vector3(areaCenter.x, floorHeight - SourceThickness * 0.5f, areaCenter.z)),
        };

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        settings.overrideVoxelSize = true;
        settings.voxelSize = voxelSize;

        var bounds = new Bounds(new Vector3(areaCenter.x, floorHeight, areaCenter.z), areaSize);
        NavMeshData data = NavMeshBuilder.BuildNavMeshData(
            settings, new List<NavMeshBuildSource> { source }, bounds, Vector3.zero, Quaternion.identity);

        instance = NavMesh.AddNavMeshData(data);
    }

    private void OnDestroy()
    {
        instance.Remove();
    }
}
