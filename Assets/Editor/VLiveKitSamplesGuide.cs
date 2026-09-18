using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace VLiveKit.Editor
{
    /// <summary>Project entry point for VLiveKit scenes, including samples not yet imported.</summary>
    [InitializeOnLoad]
    internal sealed class VLiveKitSamplesGuide : EditorWindow
    {
        private const string StartupPreferencePrefix = "VLiveKit.SamplesGuide.ShowOnStartup.";
        private const string SessionKey = "VLiveKit.SamplesGuide.ShownThisSession";
        private const double StartupQuietPeriod = 1.0d;

        private static readonly Sample[] Samples =
        {
            new Sample(
                "01  CAMERA",
                "カメラユニット",
                "カメラリグや Cinemachine の構成を確認できるサンプルです。シーンを開いて、Hierarchy から各カメラの設定を見てみましょう。",
                "VLK_CAMERAUNIT",
                "6179911f5f7cded4ba541b0659b67a53",
                "Packages/com.toshi.vlivekit.cameraunit/Samples/Scenes/VLK_CAMERAUNIT.unity",
                "VLive Camera Unit"),
            new Sample(
                "02  LED / LTCGI",
                "LED スクリーンと光の表現",
                "LED スクリーンの映像と LTCGI の連携を確認するサンプルです。再生して、スクリーンと周囲の照明の変化を見てみましょう。",
                "LTC_GI_Test",
                "e77b8f8623f38df4dbb9e5621e33567f",
                "Packages/com.toshi.vlivekit.ledvision/LTCGI/LTCGI_TestScene/Scenes/LTC_GI_Test.unity",
                "VLive LED Vision")
        };

        private static double readySince = -1d;
        private readonly List<SceneGroup> sceneGroups = new List<SceneGroup>();
        private readonly HashSet<string> expandedGroups = new HashSet<string>();
        private Vector2 scrollPosition;
        [SerializeField] private string searchText = "";
        [SerializeField] private bool availableOnly;
        private bool refreshPending;
        private int sceneCount;
        private int availableCount;
        private string catalogNotice;
        private GUIStyle headingStyle;
        private GUIStyle cardStyle;
        private GUIStyle descriptionStyle;

        private static string StartupPreferenceKey => StartupPreferencePrefix + Application.dataPath;

        static VLiveKitSamplesGuide()
        {
            // Unity can initialize EditorWindow types while restoring serialized objects.
            // Defer native API access until the editor update instead of the constructor.
            EditorApplication.update += TryShowAtStartup;
        }

        [MenuItem("toshi/VLiveKit/Samples", false, 10)]
        private static void OpenWindow()
        {
            SessionState.SetBool(SessionKey, true);
            EditorApplication.update -= TryShowAtStartup;
            var window = GetWindow<VLiveKitSamplesGuide>();
            window.titleContent = new GUIContent("VLiveKit サンプル");
            window.minSize = new Vector2(560f, 520f);
            window.Show();
        }

        private static void TryShowAtStartup()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false) || !EditorPrefs.GetBool(StartupPreferenceKey, true))
            {
                SessionState.SetBool(SessionKey, true);
                EditorApplication.update -= TryShowAtStartup;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                readySince = -1d;
                return;
            }

            if (readySince < 0d)
            {
                readySince = EditorApplication.timeSinceStartup;
                return;
            }

            if (EditorApplication.timeSinceStartup - readySince < StartupQuietPeriod)
            {
                return;
            }

            // Let the existing package-manager welcome finish first. The package is optional,
            // so avoid a compile-time dependency on its private utility-window type.
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().FullName == "VLiveKitInstallerWindow+FirstRunPromptWindow")
                {
                    readySince = EditorApplication.timeSinceStartup;
                    return;
                }
            }

            OpenWindow();
        }

        private void OnEnable()
        {
            minSize = new Vector2(560f, 520f);
            // Restored layouts count as the one automatic appearance for this session too.
            SessionState.SetBool(SessionKey, true);
            EditorApplication.update -= TryShowAtStartup;
            EditorApplication.projectChanged += RequestCatalogRefresh;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            RequestCatalogRefresh();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RequestCatalogRefresh;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= RefreshCatalogWhenReady;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Space(16f);
                GUILayout.Label("VLiveKit Samples", headingStyle);
                EditorGUILayout.LabelField("ここからサンプルを開いて、VLiveKit の構成や表現を試せます。", descriptionStyle);
                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("検索", GUILayout.Width(30f));
                    searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
                    availableOnly = GUILayout.Toggle(availableOnly, "開けるシーンのみ", EditorStyles.toolbarButton, GUILayout.Width(110f));
                    if (GUILayout.Button("更新", EditorStyles.toolbarButton, GUILayout.Width(45f)))
                    {
                        RequestCatalogRefresh();
                    }
                }
                EditorGUILayout.LabelField(
                    refreshPending ? "シーン一覧を読み込み中…" : $"全 {sceneCount} 件  /  開ける {availableCount} 件  /  未インポート {sceneCount - availableCount} 件",
                    EditorStyles.miniLabel);

                using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scroll.scrollPosition;
                    foreach (var sample in Samples)
                    {
                        if (MatchesSearch(sample.Title + " " + sample.Description + " " + sample.PackageDisplayName + " " + sample.PackageScenePath)
                            && (!availableOnly || ResolveScene(sample) != null))
                        {
                            DrawSample(sample);
                            GUILayout.Space(8f);
                        }
                    }

                    GUILayout.Space(8f);
                    GUILayout.Label("パッケージ内のシーン", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("各パッケージを展開するとシーン一覧を確認できます。検索は名前・パッケージ名・保存場所が対象です。", EditorStyles.wordWrappedMiniLabel);
                    var visibleCount = 0;
                    foreach (var group in sceneGroups)
                    {
                        visibleCount += DrawSceneGroup(group);
                    }
                    if (!refreshPending && visibleCount == 0)
                    {
                        EditorGUILayout.HelpBox("条件に一致する追加シーンはありません。", MessageType.Info);
                    }
                    if (!string.IsNullOrEmpty(catalogNotice))
                    {
                        EditorGUILayout.HelpBox(catalogNotice, MessageType.Info);
                    }
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorGUILayout.HelpBox("シーンを切り替えるには、再生を停止してください。", MessageType.Info);
                }

                EditorGUILayout.LabelField("メニューの toshi / VLiveKit / Samples から、いつでも開けます。", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);
                EditorGUI.BeginChangeCheck();
                var showOnStartup = EditorGUILayout.ToggleLeft(
                    "このプロジェクトの起動時に表示する",
                    EditorPrefs.GetBool(StartupPreferenceKey, true));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(StartupPreferenceKey, showOnStartup);
                }
                GUILayout.Space(10f);
            }
        }

        private void RequestCatalogRefresh()
        {
            refreshPending = true;
            EditorApplication.update -= RefreshCatalogWhenReady;
            EditorApplication.update += RefreshCatalogWhenReady;
            Repaint();
        }

        private void RefreshCatalogWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= RefreshCatalogWhenReady;
            refreshPending = false;
            sceneGroups.Clear();
            sceneCount = 0;
            availableCount = 0;
            catalogNotice = null;
            var packages = PackageInfo.GetAllRegisteredPackages();
            if (packages == null)
            {
                catalogNotice = "パッケージの読み込みが終わってから「更新」を押してください。";
                Repaint();
                return;
            }

            foreach (var package in packages)
            {
                if (package.name != "com.toshi.vlivekit" && !package.name.StartsWith("com.toshi.vlivekit.", StringComparison.Ordinal))
                {
                    continue;
                }

                var group = new SceneGroup(package.name, string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName);
                var importedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var importedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (AssetDatabase.IsValidFolder(package.assetPath))
                {
                    // Discovery is cached until the project changes or the user requests a refresh.
                    foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { package.assetPath }))
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (!assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || !importedPaths.Add(assetPath))
                        {
                            continue;
                        }

                        var relativePath = assetPath.Substring(package.assetPath.Length).TrimStart('/');
                        var sourcePath = Path.GetFullPath(Path.Combine(package.resolvedPath, relativePath));
                        importedSources.Add(sourcePath);
                        group.Scenes.Add(new SceneEntry(assetPath, sourcePath, relativePath, null, IsFeatured(guid, assetPath)));
                        availableCount++;
                    }
                }

                // A local submodule can contain legacy/demo scenes outside its exported UPM
                // folder. List their locations without copying files or importing dependencies.
                var sourceRoot = GetSourceRoot(package.resolvedPath);
                try
                {
                    foreach (var sourcePath in EnumerateSourceScenes(sourceRoot, package.resolvedPath))
                    {
                        var fullPath = Path.GetFullPath(sourcePath);
                        if (importedSources.Contains(fullPath))
                        {
                            continue;
                        }

                        var guid = ReadSceneGuid(fullPath);
                        if (IsFeatured(guid, null))
                        {
                            continue;
                        }

                        var relativePath = fullPath.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
                        var inPackage = fullPath.StartsWith(Path.GetFullPath(package.resolvedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                        var reason = !inPackage ? "公開パッケージの範囲外" : relativePath.Contains("~/") ? "未インポート（Samples~ など）" : "未読み込み";
                        group.Scenes.Add(new SceneEntry(null, fullPath, relativePath, reason, false));
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    catalogNotice = "一部の元ファイルを読み取れませんでした。パッケージの配置を確認して「更新」を押してください。";
                }

                group.Scenes.Sort((left, right) => string.Compare(left.RelativePath, right.RelativePath, StringComparison.OrdinalIgnoreCase));
                if (group.Scenes.Count > 0)
                {
                    sceneGroups.Add(group);
                    sceneCount += group.Scenes.Count;
                }
            }

            sceneGroups.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            Repaint();
        }

        private static string GetSourceRoot(string resolvedPath)
        {
            var fullPath = Path.GetFullPath(resolvedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var projectPackages = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages")) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(projectPackages, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = fullPath.Substring(projectPackages.Length);
                var directoryName = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                if (directoryName.StartsWith("VLiveKit", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(projectPackages, directoryName);
                }
            }
            return fullPath;
        }

        private static IEnumerable<string> EnumerateSourceScenes(string sourceRoot, string packageRoot)
        {
            if (string.Equals(sourceRoot, Path.GetFullPath(packageRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            {
                return Directory.EnumerateFiles(sourceRoot, "*.unity", SearchOption.AllDirectories);
            }

            // These sibling folders belong to the submodule's original Unity project.
            // Its generated Library includes copies of unrelated dependency-package scenes.
            var paths = new List<string>(Directory.EnumerateFiles(sourceRoot, "*.unity", SearchOption.TopDirectoryOnly));
            foreach (var directory in Directory.EnumerateDirectories(sourceRoot))
            {
                var name = Path.GetFileName(directory);
                if (name.StartsWith(".", StringComparison.Ordinal) || name == "Library" || name == "Temp" || name == "Logs" || name == "obj")
                {
                    continue;
                }
                paths.AddRange(Directory.EnumerateFiles(directory, "*.unity", SearchOption.AllDirectories));
            }
            return paths;
        }

        private static string ReadSceneGuid(string scenePath)
        {
            if (File.Exists(scenePath + ".meta"))
            {
                foreach (var line in File.ReadLines(scenePath + ".meta"))
                {
                    if (line.StartsWith("guid: ", StringComparison.Ordinal))
                    {
                        return line.Substring(6).Trim();
                    }
                }
            }
            return null;
        }

        private static bool IsFeatured(string guid, string assetPath)
        {
            foreach (var sample in Samples)
            {
                if (sample.SceneGuid == guid || sample.PackageScenePath == assetPath)
                {
                    return true;
                }
            }
            return false;
        }

        private bool MatchesSearch(string text)
        {
            return string.IsNullOrWhiteSpace(searchText) || text.IndexOf(searchText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int DrawSceneGroup(SceneGroup group)
        {
            var matches = group.Scenes.FindAll(scene => !scene.Featured && (!availableOnly || scene.Available)
                && MatchesSearch(group.DisplayName + " " + group.PackageName + " " + scene.RelativePath));
            if (matches.Count == 0)
            {
                return 0;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var label = group.DisplayName + "  —  " + matches.Count + " 件";
                var searching = !string.IsNullOrWhiteSpace(searchText);
                var expanded = expandedGroups.Contains(group.PackageName);
                if (searching)
                {
                    GUILayout.Label(label, EditorStyles.boldLabel);
                }
                else
                {
                    expanded = EditorGUILayout.Foldout(expanded, label, true);
                    if (expanded) expandedGroups.Add(group.PackageName);
                    else expandedGroups.Remove(group.PackageName);
                }

                if (searching || expanded)
                {
                    foreach (var scene in matches)
                    {
                        DrawSceneEntry(scene);
                    }
                }
            }
            return matches.Count;
        }

        private static void DrawSceneEntry(SceneEntry entry)
        {
            GUILayout.Space(7f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Path.GetFileNameWithoutExtension(entry.RelativePath), EditorStyles.boldLabel, GUILayout.MinWidth(0f), GUILayout.ExpandWidth(true));
                using (new EditorGUI.DisabledScope(!entry.Available || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
                {
                    if (GUILayout.Button("開く", GUILayout.Width(55f)))
                    {
                        OpenScene(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.AssetPath));
                    }
                }
                if (GUILayout.Button(entry.Available ? "Project" : "元の場所", GUILayout.Width(75f)))
                {
                    if (entry.Available)
                    {
                        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.AssetPath);
                        EditorGUIUtility.PingObject(scene);
                        Selection.activeObject = scene;
                    }
                    else
                    {
                        EditorUtility.RevealInFinder(entry.SourcePath);
                    }
                }
            }
            EditorGUILayout.LabelField(entry.RelativePath, EditorStyles.wordWrappedMiniLabel);
            if (!entry.Available)
            {
                EditorGUILayout.LabelField(entry.UnavailableReason + "：このプロジェクトでは直接開けません。", EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawSample(Sample sample)
        {
            var scene = ResolveScene(sample);
            using (new EditorGUILayout.VerticalScope(cardStyle))
            {
                GUILayout.Label(sample.Category, EditorStyles.miniBoldLabel);
                GUILayout.Label(sample.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(sample.Description, descriptionStyle);
                GUILayout.Space(6f);
                GUILayout.Label(sample.SceneName + ".unity", EditorStyles.miniLabel);

                if (scene == null)
                {
                    EditorGUILayout.HelpBox(
                        "シーンが見つかりません。「" + sample.PackageDisplayName + "」パッケージの導入・読み込みを確認してください。",
                        MessageType.Info);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(scene == null || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
                    {
                        if (GUILayout.Button("シーンを開く", GUILayout.Height(28f)))
                        {
                            OpenScene(scene);
                        }
                    }

                    using (new EditorGUI.DisabledScope(scene == null))
                    {
                        if (GUILayout.Button("Project で表示", GUILayout.Height(28f), GUILayout.Width(135f)))
                        {
                            EditorGUIUtility.PingObject(scene);
                            Selection.activeObject = scene;
                        }
                    }
                }
            }
        }

        private static SceneAsset ResolveScene(Sample sample)
        {
            var path = AssetDatabase.GUIDToAssetPath(sample.SceneGuid);
            var scene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            return scene != null ? scene : AssetDatabase.LoadAssetAtPath<SceneAsset>(sample.PackageScenePath);
        }

        private static void OpenScene(SceneAsset scene)
        {
            if (scene == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene), OpenSceneMode.Single);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("VLiveKit サンプル", "シーンを開けませんでした。Console の内容を確認してください。", "OK");
            }
        }

        private void EnsureStyles()
        {
            if (headingStyle != null)
            {
                return;
            }

            headingStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, margin = new RectOffset(10, 10, 0, 6) };
            cardStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(14, 14, 12, 12), margin = new RectOffset(8, 8, 0, 0) };
            descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel) { margin = new RectOffset(10, 10, 4, 4) };
        }

        private sealed class Sample
        {
            public readonly string Category;
            public readonly string Title;
            public readonly string Description;
            public readonly string SceneName;
            public readonly string SceneGuid;
            public readonly string PackageScenePath;
            public readonly string PackageDisplayName;

            public Sample(string category, string title, string description, string sceneName, string sceneGuid, string packageScenePath, string packageDisplayName)
            {
                Category = category;
                Title = title;
                Description = description;
                SceneName = sceneName;
                SceneGuid = sceneGuid;
                PackageScenePath = packageScenePath;
                PackageDisplayName = packageDisplayName;
            }
        }

        private sealed class SceneGroup
        {
            public readonly string PackageName;
            public readonly string DisplayName;
            public readonly List<SceneEntry> Scenes = new List<SceneEntry>();

            public SceneGroup(string packageName, string displayName)
            {
                PackageName = packageName;
                DisplayName = displayName;
            }
        }

        private sealed class SceneEntry
        {
            public readonly string AssetPath;
            public readonly string SourcePath;
            public readonly string RelativePath;
            public readonly string UnavailableReason;
            public readonly bool Featured;
            public bool Available => !string.IsNullOrEmpty(AssetPath);

            public SceneEntry(string assetPath, string sourcePath, string relativePath, string unavailableReason, bool featured)
            {
                AssetPath = assetPath;
                SourcePath = sourcePath;
                RelativePath = relativePath;
                UnavailableReason = unavailableReason;
                Featured = featured;
            }
        }
    }
}
