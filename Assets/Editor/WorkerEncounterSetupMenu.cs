using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열린 씬에서 Tools > Worker Encounter > Setup 실행 시
// 전용 Canvas, EmotionIconManager, WorkerEncounterManager를 만들고 프리팹/스프라이트까지 연결한다 (다시 실행해도 중복 생성 안 함)
public static class WorkerEncounterSetupMenu
{
    private const string BubblePrefabPath = "Assets/ALL/Prefabs/SpeechBubble/SpeechBubbleUI.prefab";
    private const string IconPrefabPath = "Assets/ALL/Prefabs/Emotion/PF_EmotionIcon.prefab";

    // shortcut: Surprised 이미지가 없어 비워 둠 (그 감정은 말풍선만 표시), 이미지를 추가하면 여기에 등록
    private static readonly (EmotionType emotion, string path)[] Sprites =
    {
        (EmotionType.Happy, "Assets/ALL/Arts/활짝 웃는 블러셔 이모지.png"),
        (EmotionType.Angry, "Assets/ALL/Arts/분노한 광택 이모지 얼굴.png"),
        (EmotionType.Tired, "Assets/ALL/Arts/지친 하품 이모지와 한숨 구름.png"),
    };

    [MenuItem("Tools/Worker Encounter/Setup")]
    private static void Setup()
    {
        var bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubblePrefabPath);
        var iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath);

        if (bubblePrefab == null || iconPrefab == null)
        {
            Debug.LogError($"[WorkerEncounter] 프리팹을 찾지 못함: {BubblePrefabPath}, {IconPrefabPath}");
            return;
        }

        RectTransform canvas = FindOrCreateCanvas();

        var encounter = FindOrCreate<WorkerEncounterManager>();
        var so = new SerializedObject(encounter);
        so.FindProperty("canvasRoot").objectReferenceValue = canvas;
        so.FindProperty("bubblePrefab").objectReferenceValue = bubblePrefab.GetComponentInChildren<SpeechBubbleUI>(true);
        so.ApplyModifiedProperties();

        var icons = FindOrCreate<EmotionIconManager>();
        so = new SerializedObject(icons);
        so.FindProperty("canvasRoot").objectReferenceValue = canvas;
        so.FindProperty("iconPrefab").objectReferenceValue = iconPrefab.GetComponent<EmotionIconUI>();

        SerializedProperty list = so.FindProperty("sprites");
        list.ClearArray();
        foreach (var (emotion, path) in Sprites)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[WorkerEncounter] 스프라이트를 찾지 못함: {path}");
                continue;
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            SerializedProperty entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("emotion").enumValueIndex = (int)emotion;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        }
        so.ApplyModifiedProperties();

        Selection.activeGameObject = encounter.gameObject;
        EditorSceneManager.MarkSceneDirty(encounter.gameObject.scene);

        Debug.Log("[WorkerEncounter] 설정 완료: 씬 저장 후 Play");
    }

    private static RectTransform FindOrCreateCanvas()
    {
        const string name = "Canvas_WorkerEncounter";

        GameObject existing = GameObject.Find(name);
        if (existing != null)
            return existing.GetComponent<RectTransform>();

        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(go, "Create Worker Encounter Canvas");

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;   // 게임 UI가 말풍선을 덮도록 아래에 그림

        // 해상도가 달라도 말풍선 비율이 유지되게 함 (가로/세로 어느 쪽이든 match 0.5면 배율이 비슷함)
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return go.GetComponent<RectTransform>();
    }

    private static T FindOrCreate<T>() where T : Component
    {
        T found = Object.FindFirstObjectByType<T>();
        if (found != null)
            return found;

        var go = new GameObject(typeof(T).Name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {typeof(T).Name}");
        return go.AddComponent<T>();
    }
}
