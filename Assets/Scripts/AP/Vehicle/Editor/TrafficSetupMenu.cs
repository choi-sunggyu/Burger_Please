using UnityEditor;
using UnityEngine;

// Demo 씬에서 Tools > Traffic > Setup Demo Traffic 실행 시
// TrafficManager 생성 -> Road Root(Demo) / 기존 차량(Road_Cars) 지정 -> 경로 Bake 까지 한 번에 처리
public static class TrafficSetupMenu
{
    private const string RoadRootName = "Demo";
    private const string VehicleRootName = "Road_Cars";

    [MenuItem("Tools/Traffic/Setup Demo Traffic")]
    private static void Setup()
    {
        GameObject roadRoot = GameObject.Find(RoadRootName);
        GameObject vehicleRoot = GameObject.Find(VehicleRootName);

        if (roadRoot == null || vehicleRoot == null)
        {
            Debug.LogError($"[Traffic] '{RoadRootName}' 또는 '{VehicleRootName}' 오브젝트를 찾지 못함 (Demo 씬을 열고 실행)");
            return;
        }

        TrafficManager manager = Object.FindFirstObjectByType<TrafficManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("TrafficManager");
            Undo.RegisterCreatedObjectUndo(go, "Create TrafficManager");
            manager = go.AddComponent<TrafficManager>();
        }

        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("roadRoot").objectReferenceValue = roadRoot.transform;
        so.FindProperty("existingVehicleRoot").objectReferenceValue = vehicleRoot.transform;
        so.ApplyModifiedProperties();

        manager.BakeRoutes();

        int vehicleCount = 0;
        foreach (Transform child in vehicleRoot.transform)
        {
            if (child.gameObject.activeInHierarchy)
                vehicleCount++;
        }

        Selection.activeGameObject = manager.gameObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);

        Debug.Log($"[Traffic] 설정 완료: 기존 차량 {vehicleCount}대 사용, 씬 저장 후 Play");
    }
}
