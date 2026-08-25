using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ThirdPersonAssetInspector
{
    private const string GeneratedFolder = "Assets/Generated";
    private const string ReportPath =
        GeneratedFolder + "/ThirdPersonAssetInspection.txt";

    private static readonly HashSet<string> KnownOldTopLevelFolders =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_Recovery",
            "Aletheia",
            "AllSkyFree",
            "Editor",
            "Generated",
            "Malbers Animations",
            "Polytope Studio",
            "PurePoly",
            "Scenes",
            "Scripts",
            "Settings",
            "TutorialInfo",
            "UpDraft Art",
            "UpDraftArt",
            "ZerinLabs_lowpolyPack_ModularDungeons"
        };

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Inspect Imported Character Assets",
        priority = 300)]
    public static void Inspect()
    {
        EnsureFolder(GeneratedFolder);

        StringBuilder report = new StringBuilder();

        report.AppendLine(
            new string('=', 90));

        report.AppendLine(
            "THIRD-PERSON CHARACTER / ANIMATION ASSET INSPECTION");

        report.AppendLine(
            new string('=', 90));

        report.AppendLine(
            "Generated: " +
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        report.AppendLine(
            "Unity: " +
            Application.unityVersion);

        report.AppendLine();

        WritePackageStatus(report);
        WriteTopLevelFolders(report);
        WriteRecentAssets(report);
        WriteHumanoidModels(report);
        WriteAnimatedPrefabs(report);
        WriteAnimatorControllers(report);
        WriteAnimationClips(report);

        File.WriteAllText(
            ReportPath,
            report.ToString());

        AssetDatabase.ImportAsset(
            ReportPath,
            ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();

        UnityEngine.Object reportAsset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                ReportPath);

        Selection.activeObject = reportAsset;
        EditorGUIUtility.PingObject(reportAsset);

        EditorUtility.DisplayDialog(
            "Character Asset Inspection Complete",
            "Report created at:\n" +
            ReportPath +
            "\n\nUpload that report here before we build the third-person player.",
            "OK");

        Debug.Log(
            "Third-person asset inspection complete: " +
            ReportPath);
    }

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Select Inspection Report",
        priority = 301)]
    public static void SelectReport()
    {
        UnityEngine.Object reportAsset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                ReportPath);

        if (reportAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Report Not Found",
                "Run the asset inspection first.",
                "OK");
            return;
        }

        Selection.activeObject = reportAsset;
        EditorGUIUtility.PingObject(reportAsset);
    }

    private static void WritePackageStatus(
        StringBuilder report)
    {
        report.AppendLine(
            "PACKAGE STATUS");

        report.AppendLine(
            new string('-', 90));

        string manifestPath =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Packages",
                "manifest.json");

        string manifest =
            File.Exists(manifestPath)
                ? File.ReadAllText(manifestPath)
                : string.Empty;

        bool hasCinemachine =
            manifest.IndexOf(
                "com.unity.cinemachine",
                StringComparison.OrdinalIgnoreCase) >= 0;

        bool hasInputSystem =
            manifest.IndexOf(
                "com.unity.inputsystem",
                StringComparison.OrdinalIgnoreCase) >= 0;

        report.AppendLine(
            "Cinemachine installed: " +
            (hasCinemachine ? "YES" : "NO"));

        report.AppendLine(
            "Input System installed: " +
            (hasInputSystem ? "YES" : "NO"));

        report.AppendLine();
    }

    private static void WriteTopLevelFolders(
        StringBuilder report)
    {
        report.AppendLine(
            "TOP-LEVEL ASSET FOLDERS");

        report.AppendLine(
            new string('-', 90));

        string assetsAbsolute =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets");

        if (!Directory.Exists(assetsAbsolute))
        {
            report.AppendLine(
                "Assets directory not found.");

            report.AppendLine();
            return;
        }

        string[] folders =
            Directory.GetDirectories(
                assetsAbsolute,
                "*",
                SearchOption.TopDirectoryOnly)
            .OrderBy(path => path)
            .ToArray();

        foreach (string folder in folders)
        {
            string name =
                Path.GetFileName(folder);

            bool isNew =
                !KnownOldTopLevelFolders.Contains(name);

            report.AppendLine(
                (isNew ? "[NEW/UNKNOWN] " : "[KNOWN] ") +
                "Assets/" +
                name);
        }

        report.AppendLine();
    }

    private static void WriteRecentAssets(
        StringBuilder report)
    {
        report.AppendLine(
            "RECENTLY MODIFIED ASSETS — LAST 72 HOURS");

        report.AppendLine(
            new string('-', 90));

        string assetsAbsolute =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets");

        DateTime threshold =
            DateTime.UtcNow.AddHours(-72);

        string[] usefulExtensions =
        {
            ".prefab",
            ".fbx",
            ".obj",
            ".blend",
            ".anim",
            ".controller",
            ".overridecontroller",
            ".mat",
            ".png",
            ".jpg",
            ".jpeg",
            ".tga"
        };

        List<string> recent =
            Directory
                .EnumerateFiles(
                    assetsAbsolute,
                    "*",
                    SearchOption.AllDirectories)
                .Where(path =>
                    !path.EndsWith(
                        ".meta",
                        StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                    usefulExtensions.Contains(
                        Path.GetExtension(path),
                        StringComparer.OrdinalIgnoreCase))
                .Where(path =>
                    File.GetLastWriteTimeUtc(path) >= threshold)
                .OrderByDescending(
                    File.GetLastWriteTimeUtc)
                .Take(500)
                .ToList();

        if (recent.Count == 0)
        {
            report.AppendLine(
                "No matching recently modified assets found.");
        }
        else
        {
            foreach (string absolutePath in recent)
            {
                string relative =
                    ToProjectRelativePath(
                        absolutePath);

                report.AppendLine(
                    File.GetLastWriteTime(
                        absolutePath)
                    .ToString(
                        "yyyy-MM-dd HH:mm:ss") +
                    " | " +
                    relative);
            }
        }

        report.AppendLine();
    }

    private static void WriteHumanoidModels(
        StringBuilder report)
    {
        report.AppendLine(
            "MODEL / FBX INSPECTION");

        report.AppendLine(
            new string('-', 90));

        string[] paths =
            AssetDatabase.GetAllAssetPaths()
                .Where(path =>
                    path.StartsWith(
                        "Assets/",
                        StringComparison.OrdinalIgnoreCase))
                .Where(path =>
                    IsModelPath(path))
                .OrderBy(path => path)
                .ToArray();

        int humanoidCount = 0;
        int animatedCount = 0;

        foreach (string path in paths)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(path)
                as ModelImporter;

            if (importer == null)
                continue;

            bool humanoid =
                importer.animationType ==
                ModelImporterAnimationType.Human;

            bool generic =
                importer.animationType ==
                ModelImporterAnimationType.Generic;

            bool hasAnimation =
                importer.importAnimation;

            if (!humanoid &&
                !generic &&
                !hasAnimation)
            {
                continue;
            }

            if (humanoid)
                humanoidCount++;

            if (hasAnimation)
                animatedCount++;

            GameObject model =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            ModelInspection inspection =
                InspectGameObjectAsset(model);

            report.AppendLine(
                "MODEL: " +
                Path.GetFileNameWithoutExtension(path));

            report.AppendLine(
                "Path: " +
                path);

            report.AppendLine(
                "Rig type: " +
                importer.animationType);

            report.AppendLine(
                "Avatar setup: " +
                importer.avatarSetup);

            report.AppendLine(
                "Import animation: " +
                hasAnimation);

            report.AppendLine(
                "Imported clips: " +
                GetClipNames(path));

            WriteInspectionDetails(
                report,
                inspection);

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine(
            "Humanoid model count: " +
            humanoidCount);

        report.AppendLine(
            "Animation-importing model count: " +
            animatedCount);

        report.AppendLine();
    }

    private static void WriteAnimatedPrefabs(
        StringBuilder report)
    {
        report.AppendLine(
            "ANIMATED CHARACTER PREFABS");

        report.AppendLine(
            new string('-', 90));

        string[] prefabGuids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets" });

        List<string> paths =
            prefabGuids
                .Select(
                    AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToList();

        int count = 0;

        foreach (string path in paths)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (prefab == null)
                continue;

            Animator[] animators =
                prefab.GetComponentsInChildren<Animator>(true);

            SkinnedMeshRenderer[] skinnedMeshes =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (animators.Length == 0 &&
                skinnedMeshes.Length == 0)
            {
                continue;
            }

            count++;

            ModelInspection inspection =
                InspectGameObjectAsset(prefab);

            report.AppendLine(
                "PREFAB: " +
                prefab.name);

            report.AppendLine(
                "Path: " +
                path);

            WriteInspectionDetails(
                report,
                inspection);

            foreach (Animator animator in animators)
            {
                report.AppendLine(
                    "Animator object: " +
                    GetHierarchyPath(
                        animator.transform,
                        prefab.transform));

                report.AppendLine(
                    "Animator avatar: " +
                    (animator.avatar != null
                        ? animator.avatar.name +
                          " | valid=" +
                          animator.avatar.isValid +
                          " | human=" +
                          animator.avatar.isHuman
                        : "NONE"));

                report.AppendLine(
                    "Animator controller: " +
                    (animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController)
                        : "NONE"));
            }

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine(
            "Animated/skinned prefab count: " +
            count);

        report.AppendLine();
    }

    private static void WriteAnimatorControllers(
        StringBuilder report)
    {
        report.AppendLine(
            "ANIMATOR CONTROLLERS");

        report.AppendLine(
            new string('-', 90));

        string[] controllerGuids =
            AssetDatabase.FindAssets(
                "t:AnimatorController",
                new[] { "Assets" });

        foreach (string guid in controllerGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    path);

            if (controller == null)
                continue;

            report.AppendLine(
                "CONTROLLER: " +
                controller.name);

            report.AppendLine(
                "Path: " +
                path);

            report.AppendLine(
                "Parameters: " +
                string.Join(
                    ", ",
                    controller.parameters.Select(
                        parameter =>
                            parameter.name +
                            ":" +
                            parameter.type)));

            List<string> states =
                new List<string>();

            foreach (AnimatorControllerLayer layer in
                     controller.layers)
            {
                CollectStates(
                    layer.stateMachine,
                    layer.name,
                    states);
            }

            report.AppendLine(
                "States: " +
                (states.Count > 0
                    ? string.Join(", ", states)
                    : "NONE"));

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine();
    }

    private static void WriteAnimationClips(
        StringBuilder report)
    {
        report.AppendLine(
            "ANIMATION CLIPS");

        report.AppendLine(
            new string('-', 90));

        string[] clipGuids =
            AssetDatabase.FindAssets(
                "t:AnimationClip",
                new[] { "Assets" });

        Dictionary<string, List<string>> byFolder =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string guid in clipGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    path);

            if (clip == null ||
                clip.name.StartsWith(
                    "__preview__",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string folder =
                Path.GetDirectoryName(path)
                    .Replace('\\', '/');

            if (!byFolder.TryGetValue(
                    folder,
                    out List<string> clips))
            {
                clips = new List<string>();
                byFolder[folder] = clips;
            }

            clips.Add(
                clip.name +
                " | length=" +
                clip.length.ToString(
                    "0.000",
                    CultureInfo.InvariantCulture) +
                "s | loop=" +
                clip.isLooping +
                " | humanMotion=" +
                clip.humanMotion);
        }

        foreach (KeyValuePair<string, List<string>> pair in
                 byFolder.OrderBy(pair => pair.Key))
        {
            report.AppendLine(
                "FOLDER: " +
                pair.Key);

            foreach (string clip in
                     pair.Value.OrderBy(name => name))
            {
                report.AppendLine(
                    "  - " +
                    clip);
            }
        }

        report.AppendLine();
    }

    private static void WriteInspectionDetails(
        StringBuilder report,
        ModelInspection inspection)
    {
        report.AppendLine(
            "Renderers: " +
            inspection.rendererCount);

        report.AppendLine(
            "Skinned renderers: " +
            inspection.skinnedRendererCount);

        report.AppendLine(
            "Animators: " +
            inspection.animatorCount);

        report.AppendLine(
            "Colliders: " +
            inspection.colliderCount);

        report.AppendLine(
            "Bounds size: " +
            inspection.boundsSize);

        report.AppendLine(
            "Materials: " +
            (inspection.materialNames.Count > 0
                ? string.Join(
                    ", ",
                    inspection.materialNames)
                : "NONE"));

        report.AppendLine(
            "Broken material slots: " +
            inspection.brokenMaterialSlots);

        report.AppendLine(
            "Missing scripts: " +
            inspection.missingScripts);

        report.AppendLine(
            "Likely hand bones: " +
            (inspection.handBones.Count > 0
                ? string.Join(
                    ", ",
                    inspection.handBones)
                : "NONE FOUND"));

        report.AppendLine(
            "Likely root/hips bones: " +
            (inspection.rootBones.Count > 0
                ? string.Join(
                    ", ",
                    inspection.rootBones)
                : "NONE FOUND"));
    }

    private static ModelInspection InspectGameObjectAsset(
        GameObject asset)
    {
        ModelInspection result =
            new ModelInspection();

        if (asset == null)
            return result;

        GameObject instance =
            PrefabUtility.InstantiatePrefab(asset)
            as GameObject;

        if (instance == null)
        {
            instance =
                UnityEngine.Object.Instantiate(asset);
        }

        instance.hideFlags =
            HideFlags.HideAndDontSave;

        instance.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);

        Renderer[] renderers =
            instance.GetComponentsInChildren<Renderer>(true);

        SkinnedMeshRenderer[] skinned =
            instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        Animator[] animators =
            instance.GetComponentsInChildren<Animator>(true);

        Collider[] colliders =
            instance.GetComponentsInChildren<Collider>(true);

        result.rendererCount =
            renderers.Length;

        result.skinnedRendererCount =
            skinned.Length;

        result.animatorCount =
            animators.Length;

        result.colliderCount =
            colliders.Length;

        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(
                    renderer.bounds);
            }

            foreach (Material material in
                     renderer.sharedMaterials)
            {
                if (material == null)
                {
                    result.brokenMaterialSlots++;
                    continue;
                }

                if (material.shader == null ||
                    material.shader.name.Contains(
                        "Hidden/InternalErrorShader"))
                {
                    result.brokenMaterialSlots++;
                }

                if (!result.materialNames.Contains(
                        material.name))
                {
                    result.materialNames.Add(
                        material.name);
                }
            }
        }

        result.boundsSize =
            hasBounds
                ? bounds.size.ToString("F3")
                : "NO RENDERER BOUNDS";

        foreach (Transform transform in
                 instance.GetComponentsInChildren<Transform>(true))
        {
            string lower =
                transform.name.ToLowerInvariant();

            if (lower.Contains("hand") ||
                lower.Contains("wrist"))
            {
                result.handBones.Add(
                    GetHierarchyPath(
                        transform,
                        instance.transform));
            }

            if (lower.Contains("hips") ||
                lower == "root" ||
                lower.Contains("pelvis"))
            {
                result.rootBones.Add(
                    GetHierarchyPath(
                        transform,
                        instance.transform));
            }

            result.missingScripts +=
                GameObjectUtility
                    .GetMonoBehavioursWithMissingScriptCount(
                        transform.gameObject);
        }

        UnityEngine.Object.DestroyImmediate(
            instance);

        return result;
    }

    private static string GetClipNames(
        string modelPath)
    {
        UnityEngine.Object[] subAssets =
            AssetDatabase.LoadAllAssetsAtPath(
                modelPath);

        string[] names =
            subAssets
                .OfType<AnimationClip>()
                .Where(clip =>
                    !clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase))
                .Select(clip => clip.name)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

        return names.Length > 0
            ? string.Join(", ", names)
            : "NONE";
    }

    private static void CollectStates(
        AnimatorStateMachine stateMachine,
        string prefix,
        List<string> states)
    {
        foreach (ChildAnimatorState child in
                 stateMachine.states)
        {
            states.Add(
                prefix +
                "/" +
                child.state.name +
                " -> " +
                (child.state.motion != null
                    ? child.state.motion.name
                    : "NO MOTION"));
        }

        foreach (ChildAnimatorStateMachine childMachine in
                 stateMachine.stateMachines)
        {
            CollectStates(
                childMachine.stateMachine,
                prefix +
                "/" +
                childMachine.stateMachine.name,
                states);
        }
    }

    private static bool IsModelPath(
        string path)
    {
        string extension =
            Path.GetExtension(path);

        return
            extension.Equals(
                ".fbx",
                StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(
                ".obj",
                StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(
                ".blend",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string GetHierarchyPath(
        Transform transform,
        Transform root)
    {
        List<string> parts =
            new List<string>();

        Transform current = transform;

        while (current != null)
        {
            parts.Add(current.name);

            if (current == root)
                break;

            current = current.parent;
        }

        parts.Reverse();

        return string.Join("/", parts);
    }

    private static string ToProjectRelativePath(
        string absolutePath)
    {
        string projectRoot =
            Directory.GetCurrentDirectory()
                .Replace('\\', '/');

        string normalized =
            absolutePath.Replace('\\', '/');

        if (normalized.StartsWith(
                projectRoot + "/",
                StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(
                projectRoot.Length + 1);
        }

        return normalized;
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash =
            path.LastIndexOf('/');

        string parent =
            path.Substring(
                0,
                slash);

        string folder =
            path.Substring(
                slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            folder);
    }

    private sealed class ModelInspection
    {
        public int rendererCount;
        public int skinnedRendererCount;
        public int animatorCount;
        public int colliderCount;
        public int brokenMaterialSlots;
        public int missingScripts;
        public string boundsSize =
            "NO RENDERER BOUNDS";

        public readonly List<string> materialNames =
            new List<string>();

        public readonly List<string> handBones =
            new List<string>();

        public readonly List<string> rootBones =
            new List<string>();
    }
}
