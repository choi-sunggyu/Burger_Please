using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// FPS_TestScene에서 Tools > Toon > Setup FPS Test Scene 실행 시
// Toon / Outline 버튼 생성 -> ShaderManager에 연결 -> Player 주변에 Employee 배치 -> NavMeshSurface 추가까지 처리
public static class ToonSetupMenu
{
    private const string SceneName = "FPS_TestScene";
    private const string ManagerName = "ShaderManager";
    private const string EmployeePrefabPath = "Assets/ALL/Prefabs/Character/Employee.prefab";
    private const int EmployeeCount = 5;
    private const float EmployeeRadius = 3f;

    [MenuItem("Tools/Toon/Setup FPS Test Scene")]
    private static void Setup()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            Debug.LogError($"[Toon] {SceneName} 씬을 열고 실행하세요.");
            return;
        }

        if (Object.FindFirstObjectByType<ToonOutlineSwitch>() != null)
        {
            Debug.LogWarning("[Toon] 이미 설정된 씬입니다.");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject manager = GameObject.Find(ManagerName);
        GameObject player = GameObject.FindWithTag("Player");
        GameObject employeePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmployeePrefabPath);
        Material plain43 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/CEO/Material #43.mat");
        Material toon43 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/CEO/Material_43_Toon.mat");
        Material plain26 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/ArBa/Default/Material #26.mat");
        Material toon26 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resource/ArBa/Default/Material_26_Toon.mat");
        Material outline = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Outline.mat");

        if (canvas == null || manager == null || player == null || employeePrefab == null ||
            plain43 == null || toon43 == null || plain26 == null || toon26 == null || outline == null)
        {
            Debug.LogError("[Toon] Canvas, ShaderManager, Player, Employee 프리팹, 머티리얼 중 찾지 못한 것이 있습니다.");
            return;
        }

        ToonOutlineSwitch toonSwitch = Undo.AddComponent<ToonOutlineSwitch>(manager);

        SerializedObject so = new SerializedObject(toonSwitch);
        so.FindProperty("toonButton").objectReferenceValue = CreateButton(canvas.transform, "ToonButton", 80f);
        so.FindProperty("outlineButton").objectReferenceValue = CreateButton(canvas.transform, "OutlineButton", 20f);
        so.FindProperty("outlineMaterial").objectReferenceValue = outline;
        SerializedProperty pairs = so.FindProperty("pairs");
        pairs.arraySize = 2;
        SetPair(pairs.GetArrayElementAtIndex(0), plain43, toon43);
        SetPair(pairs.GetArrayElementAtIndex(1), plain26, toon26);
        so.ApplyModifiedProperties();

        for (int i = 0; i < EmployeeCount; i++)
        {
            GameObject employee = (GameObject)PrefabUtility.InstantiatePrefab(employeePrefab);
            Undo.RegisterCreatedObjectUndo(employee, "Place Employee");
            float angle = i * Mathf.PI * 2f / EmployeeCount;
            employee.transform.position = player.transform.position +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * EmployeeRadius;
        }

        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject go = new GameObject("NavMeshSurface");
            Undo.RegisterCreatedObjectUndo(go, "Create NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
        }

        Selection.activeGameObject = surface.gameObject;
        EditorSceneManager.MarkSceneDirty(manager.scene);

        Debug.Log("[Toon] 설정 완료: 선택된 NavMeshSurface에서 Bake를 누른 뒤 씬 저장 후 Play");
    }

    private static Button CreateButton(Transform parent, string name, float y)
    {
        GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        Undo.RegisterCreatedObjectUndo(go, "Create Button");
        go.name = name;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-20f, y);
        rect.sizeDelta = new Vector2(160f, 50f);

        return go.GetComponent<Button>();
    }

    private static void SetPair(SerializedProperty pair, Material plain, Material toon)
    {
        pair.FindPropertyRelative("plain").objectReferenceValue = plain;
        pair.FindPropertyRelative("toon").objectReferenceValue = toon;
    }
}
