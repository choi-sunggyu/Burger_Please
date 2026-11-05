using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 열린 씬(TestScene)에서 Tools > Warehouse Mockup > Setup 실행 시
// 씬에 배치된 트럭/선반에 목업 컴포넌트를 붙이고 사무실 고용 UI를 만든다 (다시 실행해도 중복 생성 안 함)
public static class WarehouseMockupSetupMenu
{
    private const string RootName = "WarehouseMockup";
    private const string UnloadTruckPath = "Truck/AI_Truck (3)";
    private const string LoadTruckPath = "Truck/AI_Truck (7)";
    private const string ShelfRootPath = "Shelf";
    private const string OfficeTablesPath = "WorkTable";
    private const string FloorPath = "Map/test_back (8)/Plane001";
    private static readonly string[] ShelfNames = { "AI_Shelf (16)", "AI_Shelf (15)", "AI_Shelf (14)", "AI_Shelf (2)" };

    private const string BoxPrefabPath = "Assets/Resource/box.fbx";
    private const string WorkerPrefabPath = "Assets/ALL/Prefabs/Character/Employee.prefab";
    private const string FontPath = "Assets/Fonts/NanumGothic SDF.asset";

    [MenuItem("Tools/Warehouse Mockup/Setup")]
    private static void Setup()
    {
        var boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxPrefabPath);
        var workerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerPrefabPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        GameObject unloadGo = GameObject.Find(UnloadTruckPath);
        GameObject loadGo = GameObject.Find(LoadTruckPath);
        GameObject shelfRoot = GameObject.Find(ShelfRootPath);
        GameObject officeTables = GameObject.Find(OfficeTablesPath);
        GameObject floor = GameObject.Find(FloorPath);
        PlayerCartInteractor player = Object.FindFirstObjectByType<PlayerCartInteractor>();

        if (boxPrefab == null || workerPrefab == null || font == null || unloadGo == null || loadGo == null
            || shelfRoot == null || officeTables == null || floor == null || player == null)
        {
            Debug.LogError("[WarehouseMockup] 필요한 에셋/오브젝트를 찾지 못함: TestScene을 열었는지 확인하세요.");
            return;
        }

        Transform root = FindOrCreateChild(null, RootName);

        var navMesh = GetOrAdd<MockNavMesh>(root.gameObject);
        var so = new SerializedObject(navMesh);
        so.FindProperty("floorHeight").floatValue = BoundsUtil.Of(floor).max.y;
        so.ApplyModifiedProperties();

        CargoTruck unload = SetupTruck(unloadGo, CargoMode.Unload, Vector3.back, boxPrefab, root, font);
        CargoTruck load = SetupTruck(loadGo, CargoMode.Load, Vector3.left, boxPrefab, root, font);
        ShelfStorage storage = SetupStorage(root, shelfRoot, font);
        SetupOffice(root, officeTables, player.transform, workerPrefab, unload, load, storage, font);

        Selection.activeTransform = root;
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);

        Debug.Log("[WarehouseMockup] 설정 완료: 씬 저장 후 Play");
    }

    private static CargoTruck SetupTruck(GameObject truckGo, CargoMode mode, Vector3 outDirection, GameObject boxPrefab, Transform root, TMP_FontAsset font)
    {
        var move = GetOrAdd<TruckMove>(truckGo);
        var so = new SerializedObject(move);
        so.FindProperty("outDirection").vector3Value = outDirection;
        so.ApplyModifiedProperties();

        var truck = GetOrAdd<CargoTruck>(truckGo);
        so = new SerializedObject(truck);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("boxPrefab").objectReferenceValue = boxPrefab;
        so.FindProperty("label").objectReferenceValue = SetupLabel(root, $"Label_{mode}Truck", truckGo.transform, new Vector3(0f, 70f, 0f), font);
        so.ApplyModifiedProperties();

        return truck;
    }

    private static ShelfStorage SetupStorage(Transform root, GameObject shelfRoot, TMP_FontAsset font)
    {
        Transform storageTransform = FindOrCreateChild(root, "ShelfStorage");
        var storage = GetOrAdd<ShelfStorage>(storageTransform.gameObject);

        var so = new SerializedObject(storage);
        SerializedProperty list = so.FindProperty("shelves");
        list.ClearArray();
        Transform labelTarget = null;
        foreach (string shelfName in ShelfNames)
        {
            Transform shelf = shelfRoot.transform.Find(shelfName);
            if (shelf == null)
            {
                Debug.LogWarning($"[WarehouseMockup] 선반을 찾지 못함: {shelfName}");
                continue;
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shelf;
            labelTarget = shelf;
        }
        so.FindProperty("obstacleRoot").objectReferenceValue = shelfRoot.transform;
        so.FindProperty("label").objectReferenceValue = SetupLabel(root, "Label_Storage", labelTarget, new Vector3(0f, 60f, 0f), font);
        so.ApplyModifiedProperties();

        return storage;
    }

    private static void SetupOffice(Transform root, GameObject officeTables, Transform player, GameObject workerPrefab,
        CargoTruck unload, CargoTruck load, ShelfStorage storage, TMP_FontAsset font)
    {
        Bounds tables = BoundsUtil.Of(officeTables);

        Transform office = FindOrCreateChild(root, "OfficeHireZone");
        office.position = tables.center;

        Transform spawn = FindOrCreateChild(office, "WorkerSpawnPoint");
        spawn.position = tables.center + Vector3.right * (tables.extents.x + 30f);

        Transform canvas = SetupCanvas(root);
        Transform panel = SetupPanel(canvas);
        TMP_Text countText = SetupText(panel, "CountText", new Vector2(0f, 70f), new Vector2(560f, 70f), 44f, font);
        Button button = SetupButton(panel, font);

        var zone = GetOrAdd<OfficeHireZone>(office.gameObject);
        var so = new SerializedObject(zone);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("hirePanel").objectReferenceValue = panel.gameObject;
        so.FindProperty("hireButton").objectReferenceValue = button;
        so.FindProperty("countText").objectReferenceValue = countText;
        so.FindProperty("workerPrefab").objectReferenceValue = workerPrefab;
        so.FindProperty("workerSpeed").floatValue = 100f;
        so.FindProperty("spawnPoint").objectReferenceValue = spawn;
        so.FindProperty("unloadTruck").objectReferenceValue = unload;
        so.FindProperty("loadTruck").objectReferenceValue = load;
        so.FindProperty("storage").objectReferenceValue = storage;
        so.ApplyModifiedProperties();
    }

    private static WorldLabel SetupLabel(Transform root, string name, Transform target, Vector3 offset, TMP_FontAsset font)
    {
        Transform t = root.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            t = go.transform;
            t.SetParent(root, false);
        }

        var text = t.GetComponent<TextMeshPro>();
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.fontSize = 300f;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var label = GetOrAdd<WorldLabel>(t.gameObject);
        var so = new SerializedObject(label);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("offset").vector3Value = offset;
        so.ApplyModifiedProperties();

        return label;
    }

    private static Transform SetupCanvas(Transform root)
    {
        const string name = "Canvas_Hire";

        Transform t = root.Find(name);
        if (t != null) return t;

        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Hire Canvas");
        go.transform.SetParent(root, false);

        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return go.transform;
    }

    private static Transform SetupPanel(Transform canvas)
    {
        const string name = "HirePanel";

        Transform t = canvas.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create Hire Panel");
            t = go.transform;
            t.SetParent(canvas, false);
        }

        t.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var rect = t.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(640f, 320f);

        return t;
    }

    private static TMP_Text SetupText(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, TMP_FontAsset font)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            t = go.transform;
            t.SetParent(parent, false);
        }

        var rect = t.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var text = t.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static Button SetupButton(Transform panel, TMP_FontAsset font)
    {
        const string name = "HireButton";

        Transform t = panel.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(go, "Create Hire Button");
            t = go.transform;
            t.SetParent(panel, false);
        }

        var rect = t.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, -60f);
        rect.sizeDelta = new Vector2(480f, 130f);

        t.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.3f);

        TMP_Text label = SetupText(t, "Label", Vector2.zero, rect.sizeDelta, 52f, font);
        label.text = "알바 고용";

        return t.GetComponent<Button>();
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        GameObject existing = parent == null ? GameObject.Find(name) : parent.Find(name)?.gameObject;
        if (existing != null) return existing.transform;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T found = go.GetComponent<T>();
        return found != null ? found : Undo.AddComponent<T>(go);
    }
}
