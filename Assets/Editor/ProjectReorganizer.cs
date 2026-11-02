// ProjectReorganizer.cs
// 한 번 쓰고 버리는 정리 도구입니다. 정리가 끝나면 이 파일을 지워도 됩니다.
//
// 왜 탐색기가 아니라 에디터 스크립트로 옮기나?
//   Unity는 에셋끼리의 연결(프리팹 → 머티리얼 → 셰이더 → 텍스처)을 경로가 아니라
//   .meta 파일 안의 GUID로 기억합니다. AssetDatabase.MoveAsset은 파일과 .meta를 같이 옮기고
//   에디터 내부 캐시까지 갱신하므로, 수백 개를 옮겨도 연결이 끊기지 않습니다.
//
// 사용법: 상단 메뉴 Tools ▸ 프로젝트 정리 → [1. 미리보기]로 계획 확인 → [2. 실행]

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ProjectReorganizer : EditorWindow
{
    const string P = "Assets/_Project";

    enum Kind { Move, Archive }

    class Op
    {
        public Kind kind;
        public string group;
        public string src;
        public string dst;
        public string problem; // null이면 실행 대상, 값이 있으면 건너뜀
    }

    readonly List<Op> ops = new List<Op>();
    readonly List<string> warnings = new List<string>();
    bool groupThirdParty;
    bool previewed;
    Vector2 scroll;
    string report = "";

    [MenuItem("Tools/프로젝트 정리")]
    static void Open() => GetWindow<ProjectReorganizer>("프로젝트 정리");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("실행 전에 프로젝트 폴더를 통째로 압축해 두거나 Git 커밋을 해 두세요.", MessageType.Warning);
        EditorGUI.BeginChangeCheck();
        groupThirdParty = EditorGUILayout.ToggleLeft("외부 패키지를 Assets/ThirdParty 아래로 묶기 (패키지 내부 구조는 그대로)", groupThirdParty);
        if (EditorGUI.EndChangeCheck()) previewed = false;

        if (GUILayout.Button("1. 미리보기 (아무것도 옮기지 않음)", GUILayout.Height(28)))
        {
            BuildPlan();
            report = Describe();
            previewed = true;
        }

        GUI.enabled = previewed;
        if (GUILayout.Button("2. 실행", GUILayout.Height(28)) &&
            EditorUtility.DisplayDialog("프로젝트 정리", "미리보기에 나온 대로 옮깁니다. 백업은 해 두셨나요?", "실행", "취소"))
        {
            Execute();
            previewed = false;
        }
        GUI.enabled = true;

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    // ───────────────────────── 계획 ─────────────────────────
    // 순서가 중요합니다. 특정 파일을 빼내는 규칙을 먼저, 폴더 통째 이동을 나중에 둡니다.
    void BuildPlan()
    {
        ops.Clear();
        warnings.Clear();
        string g;

        // 1) 셰이더 — 흩어진 셰이더 그래프와 HLSL을 한곳으로
        g = "Shaders";
        Files(g, "Assets", P + "/Shaders", false, ".shadergraph");          // 루트의 SG_* 6개
        File1(g, "Assets/ALL/Shaders/Scroll.shadergraph", P + "/Shaders");
        File1(g, "Assets/ALL/Resources/3Dart/SG_ForkLift.shadergraph", P + "/Shaders");
        File1(g, "Assets/Resource/Fox/Shaders/FoxToon.shadergraph", P + "/Shaders");
        File1(g, "Assets/ALL/Shaders/Toon/MainLight.hlsl", P + "/Shaders/HLSL");
        File1(g, "Assets/Scripts/Effect/Shaders/MainLightDirection.hlsl", P + "/Shaders/HLSL");

        // 2) 머티리얼 — 캐릭터 / 소품 두 갈래
        g = "Materials";
        File1(g, "Assets/Materials/MAT_FoxEmotion_Head.mat", P + "/Materials/Characters");
        File1(g, "Assets/ALL/Material/EmployeeColor.mat", P + "/Materials/Characters");
        Files(g, "Assets/Resource/Fox/Materials", P + "/Materials/Characters", false, ".mat");
        File1(g, "Assets/Resource/Fox/Textures/Fox_color.mat", P + "/Materials/Characters");
        Files(g, "Assets/Resource/ArBa", P + "/Materials/Characters", false, ".mat");
        Files(g, "Assets/Materials", P + "/Materials/Props", false, ".mat");
        Files(g, "Assets/ALL/Material", P + "/Materials/Props", false, ".mat");
        Files(g, "Assets/ALL/Resources/3Dart", P + "/Materials/Props", true, ".mat");
        File1(g, "Assets/ALL/New Material.mat", P + "/Materials/_Check");

        // 3) 프리팹 — 기존 하위 분류(Character/Effect/Emotion/SpeechBubble/Stuff) 유지
        g = "Prefabs";
        Folder(g, "Assets/ALL/Prefabs", P + "/Prefabs");

        // 4) 아트 원본 — 모델·텍스처, 캐릭터는 모델+애니메이터+PSD를 한 묶음으로
        g = "Art";
        Files(g, "Assets/ALL/Resources/3Dasset", P + "/Art/Models/Props", false, ".fbx");
        Files(g, "Assets/Resource", P + "/Art/Models/Props", false, ".fbx");   // box, bicycle, kickboard, Conveyor_Belt
        Files(g, "Assets/ALL/Resources/3Dart", P + "/Art/Textures/Props", false, ".png", ".jpg", ".jpeg", ".tga", ".psd");
        Files(g, "Assets/ALL/Arts", P + "/Art/Textures/UI", false);
        Files(g, "Assets/Resource/Fox", P + "/Art/Characters/Fox", true);
        Files(g, "Assets/Resource/ArBa", P + "/Art/Characters/ArBa", true);
        Files(g, "Assets/Resource/CEO", P + "/Art/Characters/CEO", true);

        // 5) 씬·설정·스크립트·폰트
        g = "Scenes / Settings / Scripts / Fonts";
        Folder(g, "Assets/ALL/Scenes", P + "/Scenes");
        Folder(g, "Assets/ALL/Settings", P + "/Settings");
        File1(g, "Assets/ALL/InputSystem_Actions.inputactions", P + "/Settings");
        Folder(g, "Assets/Scripts", P + "/Scripts");
        Folder(g, "Assets/Fonts", P + "/Fonts");

        // 6) 위치가 정해져 있는 것들은 Unity·플러그인이 기대하는 자리로
        g = "Fixed locations";
        Folder(g, "Assets/ALL/TextMesh Pro", "Assets/TextMesh Pro");
        File1(g, "Assets/ALL/google-services.json", "Assets");
        Folder(g, "Assets/Resource/ExternalDependencyManager", "Assets/ExternalDependencyManager");

        // 7) 외부 패키지 — 폴더째 옮기므로 안쪽 Mat/Shader/Prefab은 패키지 안에 그대로 남음
        g = "Packages (통째로)";
        string tp = groupThirdParty ? "Assets/ThirdParty" : "Assets";
        Folder(g, "Assets/Resource/RPG Monster DUO PBR Polyart", tp + "/RPG Monster DUO PBR Polyart");
        Folder(g, "Assets/Resource/Stylized Crystal", tp + "/Stylized Crystal");
        Folder(g, "Assets/Resource/Stylized_Astronaut", tp + "/Stylized_Astronaut");
        if (groupThirdParty)
        {
            Folder(g, "Assets/Synty", tp + "/Synty");
            Folder(g, "Assets/SimpleToon", tp + "/SimpleToon");
            Folder(g, "Assets/Toon Shaders Pro", tp + "/Toon Shaders Pro");
        }

        // 8) 보관 — 프로젝트 밖 _Archive_날짜 폴더로 (.meta 포함). 되돌리려면 그대로 다시 넣으면 됨
        g = "Archive";
        Archive(g, "Assets/_Recovery");                       // 크래시 복구용 임시 씬
        Archive(g, "Assets/ALL/TutorialInfo");                // URP 템플릿 안내문
        Archive(g, "Assets/ALL/Readme.asset");
        Archive(g, "Assets/ALL/Plugins");                     // 예전 템플릿의 Android/iOS 설정 (실제 쓰이는 건 Assets/Plugins)
        Archive(g, "Assets/ALL/StreamingAssets");             // Assets/StreamingAssets와 같은 파일
        Archive(g, "Assets/Resource/StreamingAssets");
        Archive(g, "Assets/ALL/NuGet.config");                // 실제 쓰이는 건 Assets/NuGet.config
        Archive(g, "Assets/ALL/packages.config");
        Archive(g, "Assets/ALL/Resources/PerformanceTestRunInfo.json");
        Archive(g, "Assets/ALL/Resources/PerformanceTestRunSettings.json");
        Archive(g, "Assets/Resource/Editor Default Resources"); // 빈 폴더

        Validate();
    }

    // 파일 하나
    void File1(string g, string src, string dstFolder)
    {
        if (!File.Exists(src) || Covered(src)) return;
        Add(Kind.Move, g, src, dstFolder + "/" + Path.GetFileName(src));
    }

    // 폴더 안의 파일들 (확장자 지정이 없으면 전부). recursive면 하위 폴더 파일도 평평하게 모음
    void Files(string g, string srcFolder, string dstFolder, bool recursive, params string[] exts)
    {
        if (!Directory.Exists(srcFolder)) return;
        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var raw in Directory.GetFiles(srcFolder, "*", opt).OrderBy(x => x))
        {
            if (raw.EndsWith(".meta")) continue;
            string f = N(raw);
            if (exts.Length > 0 && !exts.Contains(Path.GetExtension(f).ToLowerInvariant())) continue;
            File1(g, f, dstFolder);
        }
    }

    // 폴더 통째. 대상이 이미 있으면 안쪽을 하나씩 합침
    void Folder(string g, string src, string dst)
    {
        if (!AssetDatabase.IsValidFolder(src) || Covered(src)) return;
        if (!Directory.Exists(dst) && !PlannedInto(dst))
        {
            Add(Kind.Move, g, src, dst);
            return;
        }
        foreach (var f in Directory.GetFiles(src).Where(x => !x.EndsWith(".meta")).OrderBy(x => x))
            File1(g, N(f), dst);
        foreach (var d in Directory.GetDirectories(src).OrderBy(x => x))
            Folder(g, N(d), dst + "/" + Path.GetFileName(d));
    }

    void Archive(string g, string src)
    {
        if ((File.Exists(src) || Directory.Exists(src)) && !Covered(src)) Add(Kind.Archive, g, src, null);
    }

    void Add(Kind k, string g, string src, string dst) =>
        ops.Add(new Op { kind = k, group = g, src = src, dst = dst });

    // 이미 다른 규칙이 가져간 경로인지 (자기 자신 또는 옮겨질 폴더의 안쪽)
    bool Covered(string p) => ops.Any(o => p == o.src || p.StartsWith(o.src + "/"));

    // 앞선 규칙 때문에 이 폴더가 생길 예정인지
    bool PlannedInto(string dst) => ops.Any(o => o.dst != null && (o.dst == dst || o.dst.StartsWith(dst + "/")));

    static string N(string p) => p.Replace('\\', '/');

    // ───────────────────────── 검사 ─────────────────────────
    void Validate()
    {
        // 이름 충돌
        foreach (var dup in ops.Where(o => o.kind == Kind.Move).GroupBy(o => o.dst).Where(x => x.Count() > 1))
            foreach (var o in dup) o.problem = "같은 이름으로 옮겨지는 파일이 여러 개";
        foreach (var o in ops.Where(o => o.kind == Kind.Move && o.problem == null))
            if (File.Exists(o.dst) || Directory.Exists(o.dst)) o.problem = "대상 위치에 같은 이름이 이미 있음";

        // 보관 대상이 아직 어딘가에서 쓰이는지 (쓰이면 보관하지 않음)
        var archiveOps = ops.Where(o => o.kind == Kind.Archive).ToList();
        if (archiveOps.Count > 0)
        {
            var users = BuildUsers();
            System.Func<string, bool> InArchive = p => archiveOps.Any(a => p == a.src || p.StartsWith(a.src + "/"));
            foreach (var a in archiveOps)
            {
                var files = Directory.Exists(a.src)
                    ? Directory.GetFiles(a.src, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")).Select(N)
                    : new[] { a.src };
                var outside = files.SelectMany(f => users.TryGetValue(f, out var l) ? l : new List<string>())
                                   .Where(u => !InArchive(u)).Distinct().ToList();
                if (outside.Count > 0)
                    a.problem = "아직 사용 중 → " + string.Join(", ", outside.Take(3)) + (outside.Count > 3 ? " 외" : "");
            }
        }

        // ALL·Resource 안에 규칙이 빠뜨린 파일이 있는지
        foreach (var root in new[] { "Assets/ALL", "Assets/Resource", "Assets/Materials" })
        {
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(x => !x.EndsWith(".meta")).Select(N))
                if (!Covered(f)) warnings.Add("규칙에 없는 파일(그대로 둠): " + f);
        }

        // Resources 폴더에서 빠지는 파일을 코드가 이름으로 불러오고 있지 않은지
        var leaving = ops.Where(o => o.kind == Kind.Move && o.src.Contains("/Resources/")).ToList();
        if (leaving.Count > 0)
        {
            var rx = new Regex(@"Resources\s*\.\s*Load");
            foreach (var dir in new[] { "Assets/Scripts", P + "/Scripts" })
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var cs in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
                    if (rx.IsMatch(File.ReadAllText(cs)))
                        warnings.Add("Resources.Load 사용 발견 → 옮긴 뒤 경로 확인 필요: " + N(cs));
            }
        }
    }

    // 에셋 → 그 에셋을 쓰는 에셋들 (역참조 표)
    static Dictionary<string, List<string>> BuildUsers()
    {
        var users = new Dictionary<string, List<string>>();
        var all = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)).ToArray();
        try
        {
            for (int i = 0; i < all.Length; i++)
            {
                if (i % 50 == 0) EditorUtility.DisplayProgressBar("참조 검사", all[i], (float)i / all.Length);
                foreach (var d in AssetDatabase.GetDependencies(all[i], false))
                {
                    if (d == all[i]) continue;
                    if (!users.TryGetValue(d, out var list)) users[d] = list = new List<string>();
                    list.Add(all[i]);
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        return users;
    }

    string Describe()
    {
        var sb = new StringBuilder();
        int moves = ops.Count(o => o.kind == Kind.Move && o.problem == null);
        int arch = ops.Count(o => o.kind == Kind.Archive && o.problem == null);
        int bad = ops.Count(o => o.problem != null);
        long resBytes = ops.Where(o => o.kind == Kind.Move && o.src.Contains("/Resources/") && File.Exists(o.src))
                           .Sum(o => new FileInfo(o.src).Length);

        sb.AppendLine($"이동 {moves}건 · 보관 {arch}건 · 건너뜀 {bad}건");
        if (resBytes > 0)
            sb.AppendLine($"Resources 폴더에서 빠지는 파일 {resBytes / (1024f * 1024f):F0} MB (Resources 안의 파일은 사용 여부와 상관없이 빌드에 전부 들어갑니다)");
        sb.AppendLine();

        foreach (var grp in ops.GroupBy(o => o.group))
        {
            sb.AppendLine($"■ {grp.Key}");
            foreach (var o in grp)
            {
                string mark = o.problem != null ? "  ⚠ " : "    ";
                string line = o.kind == Kind.Move ? $"{o.src}  →  {o.dst}" : $"[보관] {o.src}";
                sb.AppendLine(mark + line + (o.problem != null ? $"   ({o.problem})" : ""));
            }
            sb.AppendLine();
        }

        sb.AppendLine("■ 직접 확인할 것");
        foreach (var w in warnings) sb.AppendLine("  · " + w);
        sb.AppendLine("  · Assets/Plugins/AndroidManifest.xml 은 Android 폴더 밖에 있고 파일 끝에 '\\' 문자가 붙어 있습니다. 쓰는 파일인지 확인하세요 (자동으로 건드리지 않음).");
        sb.AppendLine("  · Materials/_Check/New Material.mat 은 이름 없는 머티리얼입니다. 쓰는 곳이 없으면 지우세요.");
        return sb.ToString();
    }

    // ───────────────────────── 실행 ─────────────────────────
    void Execute()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        BuildPlan(); // 미리보기 이후 바뀐 게 있을 수 있으니 다시 계산
        var scenes = EditorBuildSettings.scenes.Select(s => (guid: s.guid.ToString(), on: s.enabled)).ToList();
        var log = new StringBuilder();
        int ok = 0, fail = 0;

        var moves = ops.Where(o => o.kind == Kind.Move && o.problem == null).ToList();
        try
        {
            for (int i = 0; i < moves.Count; i++)
            {
                var o = moves[i];
                EditorUtility.DisplayProgressBar("프로젝트 정리", o.src, (float)i / moves.Count);
                if (!File.Exists(o.src) && !Directory.Exists(o.src)) { log.AppendLine("없음(건너뜀) " + o.src); continue; }
                EnsureFolder(o.dst.Substring(0, o.dst.LastIndexOf('/')));
                string err = AssetDatabase.MoveAsset(o.src, o.dst);
                if (string.IsNullOrEmpty(err)) { ok++; log.AppendLine("이동 " + o.src + " → " + o.dst); }
                else { fail++; log.AppendLine("실패 " + o.src + " : " + err); }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        // 보관: Assets 밖으로 꺼내면 Unity가 더 이상 읽지도, 빌드에 넣지도 않음
        string archiveRoot = "_Archive_" + System.DateTime.Now.ToString("yyyyMMdd_HHmm");
        foreach (var o in ops.Where(o => o.kind == Kind.Archive && o.problem == null))
        {
            string to = Path.Combine(archiveRoot, o.src);
            Directory.CreateDirectory(Path.GetDirectoryName(to));
            if (Directory.Exists(o.src)) Directory.Move(o.src, to);
            else if (File.Exists(o.src)) File.Move(o.src, to);
            else continue;
            if (File.Exists(o.src + ".meta")) File.Move(o.src + ".meta", to + ".meta");
            log.AppendLine("보관 " + o.src + " → " + archiveRoot);
        }
        AssetDatabase.Refresh();

        // 비어 버린 옛 폴더 정리 (파일이 하나라도 남아 있으면 지우지 않음)
        foreach (var f in new[] { "Assets/ALL", "Assets/Resource", "Assets/Materials" })
            DeleteIfEmpty(f, log);

        // Build Settings의 씬 목록을 GUID 기준으로 새 경로에 맞춤
        EditorBuildSettings.scenes = scenes
            .Select(s => new EditorBuildSettingsScene(AssetDatabase.GUIDToAssetPath(s.guid), s.on))
            .Where(s => !string.IsNullOrEmpty(s.path))
            .ToArray();

        AssetDatabase.SaveAssets();
        File.WriteAllText("ProjectReorganize_Log.txt", log.ToString(), Encoding.UTF8);

        report = $"완료: 이동 {ok}건, 실패 {fail}건\n보관 폴더: {archiveRoot} (프로젝트 폴더 바로 아래)\n전체 기록: ProjectReorganize_Log.txt\n\n" + log;
        Debug.Log($"[프로젝트 정리] 이동 {ok}건, 실패 {fail}건. 기록: ProjectReorganize_Log.txt");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    static void DeleteIfEmpty(string folder, StringBuilder log)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return;
        var left = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")).ToList();
        if (left.Count > 0)
        {
            log.AppendLine($"남은 파일 {left.Count}개가 있어 지우지 않음: {folder}");
            return;
        }
        AssetDatabase.DeleteAsset(folder);
        log.AppendLine("빈 폴더 삭제 " + folder);
    }
}
#endif
