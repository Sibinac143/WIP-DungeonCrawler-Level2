using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SpiderHealthShieldInspector
{
    private const string OutputFolder = "Assets/Generated";
    private const string ReportPath =
        OutputFolder + "/SpiderHealthShieldInspection.txt";

    private static readonly string[] SearchTerms =
    {
        "free fantasy spider",
        "fantasy spider",
        "kalamona",
        "spider"
    };

    [MenuItem(
        "Tools/Dungeon Game/Phase 5/Inspect Spider Assets",
        priority = 600)]
    public static void Inspect()
    {
        EnsureFolder(OutputFolder);

        string[] allPaths =
            AssetDatabase.GetAllAssetPaths()
                .Where(path =>
                    path.StartsWith(
                        "Assets/",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        List<string> relevantPaths =
            allPaths
                .Where(IsRelevantPath)
                .Distinct()
                .OrderBy(path => path)
                .ToList();

        StringBuilder report =
            new StringBuilder();

        report.AppendLine(
            "SPIDER HEALTH + SHIELD ASSET INSPECTION");

        report.AppendLine(
            "Generated: " +
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"));

        report.AppendLine(
            "Unity: " +
            Application.unityVersion);

        report.AppendLine();

        WriteMatchedPaths(
            report,
            relevantPaths);

        WritePrefabs(
            report,
            relevantPaths);

        WriteModels(
            report,
            relevantPaths);

        WriteAnimatorControllers(
            report,
            relevantPaths);

        WriteAnimationClips(
            report,
            relevantPaths);

        WriteMaterials(
            report,
            relevantPaths);

        WriteTextures(
            report,
            relevantPaths);

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
            "Spider Inspection Complete",
            "Created:\n" +
            ReportPath +
            "\n\nUpload this text file here.",
            "OK");
    }

    private static bool IsRelevantPath(
        string path)
    {
        string lower =
            path.ToLowerInvariant();

        foreach (string term in SearchTerms)
        {
            if (lower.Contains(
                    term.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteMatchedPaths(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "MATCHED ASSET PATHS");

        report.AppendLine(
            new string('-', 90));

        if (paths.Count == 0)
        {
            report.AppendLine("NONE FOUND");
            report.AppendLine(
                "Import Free Fantasy Spider into this Unity project, then run the inspector again.");
            report.AppendLine();
            return;
        }

        foreach (string path in paths)
            report.AppendLine(path);

        report.AppendLine();
    }

    private static void WritePrefabs(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("PREFABS");
        report.AppendLine(new string('-', 90));

        string[] prefabPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".prefab",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (prefabPaths.Length == 0)
        {
            report.AppendLine("NONE FOUND");
            report.AppendLine();
            return;
        }

        foreach (string path in prefabPaths)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            if (prefab == null)
                continue;

            Animator[] animators =
                prefab.GetComponentsInChildren<Animator>(true);

            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);

            Collider[] colliders =
                prefab.GetComponentsInChildren<Collider>(true);

            Rigidbody[] rigidbodies =
                prefab.GetComponentsInChildren<Rigidbody>(true);

            int brokenSlots = 0;

            HashSet<string> materialLines =
                new HashSet<string>();

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in
                         renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        brokenSlots++;
                        continue;
                    }

                    string shaderName =
                        material.shader != null
                            ? material.shader.name
                            : "NO SHADER";

                    if (material.shader == null ||
                        shaderName.Contains(
                            "Hidden/InternalErrorShader"))
                    {
                        brokenSlots++;
                    }

                    materialLines.Add(
                        material.name +
                        " [" +
                        shaderName +
                        "]");
                }
            }

            report.AppendLine(
                "PREFAB: " +
                prefab.name);

            report.AppendLine(
                "Path: " +
                path);

            report.AppendLine(
                "Renderers: " +
                renderers.Length);

            report.AppendLine(
                "Animators: " +
                animators.Length);

            report.AppendLine(
                "Colliders: " +
                colliders.Length);

            report.AppendLine(
                "Rigidbodies: " +
                rigidbodies.Length);

            report.AppendLine(
                "Broken material slots: " +
                brokenSlots);

            if (TryGetRendererBounds(
                    prefab,
                    out Bounds bounds))
            {
                report.AppendLine(
                    "Visible bounds size: " +
                    bounds.size.x.ToString("0.000") +
                    " x " +
                    bounds.size.y.ToString("0.000") +
                    " x " +
                    bounds.size.z.ToString("0.000"));
            }

            report.AppendLine(
                "Materials: " +
                (materialLines.Count > 0
                    ? string.Join(
                        ", ",
                        materialLines)
                    : "NONE"));

            foreach (Animator animator in animators)
            {
                report.AppendLine(
                    "Animator object: " +
                    GetHierarchyPath(
                        animator.transform,
                        prefab.transform));

                report.AppendLine(
                    "Avatar: " +
                    (animator.avatar != null
                        ? animator.avatar.name +
                          " | valid=" +
                          animator.avatar.isValid +
                          " | human=" +
                          animator.avatar.isHuman
                        : "NONE"));

                report.AppendLine(
                    "Controller: " +
                    (animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController)
                        : "NONE"));
            }

            report.AppendLine(
                "Likely root/bones: " +
                FindLikelyBones(prefab));

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine();
    }

    private static void WriteModels(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("MODELS / FBX");
        report.AppendLine(new string('-', 90));

        string[] modelPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".fbx",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".obj",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".blend",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (modelPaths.Length == 0)
        {
            report.AppendLine("NONE FOUND");
            report.AppendLine();
            return;
        }

        foreach (string path in modelPaths)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(path)
                as ModelImporter;

            report.AppendLine(
                "MODEL: " +
                Path.GetFileNameWithoutExtension(path));

            report.AppendLine("Path: " + path);

            report.AppendLine(
                "Rig type: " +
                (importer != null
                    ? importer.animationType.ToString()
                    : "UNKNOWN"));

            report.AppendLine(
                "Avatar setup: " +
                (importer != null
                    ? importer.avatarSetup.ToString()
                    : "UNKNOWN"));

            report.AppendLine(
                "Import animation: " +
                (importer != null &&
                 importer.importAnimation));

            report.AppendLine(
                "Animation clips: " +
                GetSubAssetClipNames(path));

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine();
    }

    private static void WriteAnimatorControllers(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("ANIMATOR CONTROLLERS");
        report.AppendLine(new string('-', 90));

        string[] controllerPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".controller",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (controllerPaths.Length == 0)
        {
            report.AppendLine("NONE FOUND");
            report.AppendLine();
            return;
        }

        foreach (string path in controllerPaths)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    path);

            if (controller == null)
                continue;

            report.AppendLine(
                "CONTROLLER: " +
                controller.name);

            report.AppendLine("Path: " + path);

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

            report.AppendLine("States:");

            foreach (string state in states)
                report.AppendLine("  - " + state);

            report.AppendLine(
                new string('-', 45));
        }

        report.AppendLine();
    }

    private static void WriteAnimationClips(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("ANIMATION CLIPS");
        report.AppendLine(new string('-', 90));

        List<string> lines =
            new List<string>();

        foreach (string path in paths)
        {
            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(
                    path);

            foreach (AnimationClip clip in
                     assets.OfType<AnimationClip>())
            {
                if (clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                lines.Add(
                    clip.name +
                    " | path=" +
                    path +
                    " | length=" +
                    clip.length.ToString("0.000") +
                    " | loop=" +
                    clip.isLooping +
                    " | humanMotion=" +
                    clip.humanMotion);
            }
        }

        string[] unique =
            lines
                .Distinct()
                .OrderBy(line => line)
                .ToArray();

        if (unique.Length == 0)
            report.AppendLine("NONE FOUND");
        else
            foreach (string line in unique)
                report.AppendLine(line);

        report.AppendLine();
    }

    private static void WriteMaterials(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("MATERIAL ASSETS");
        report.AppendLine(new string('-', 90));

        string[] materialPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".mat",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (materialPaths.Length == 0)
        {
            report.AppendLine("NONE FOUND");
            report.AppendLine();
            return;
        }

        foreach (string path in materialPaths)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    path);

            report.AppendLine(
                path +
                " | shader=" +
                (material != null &&
                 material.shader != null
                    ? material.shader.name
                    : "NONE"));
        }

        report.AppendLine();
    }

    private static void WriteTextures(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine("TEXTURES");
        report.AppendLine(new string('-', 90));

        string[] texturePaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".png",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".jpg",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".jpeg",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".tga",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        foreach (string path in texturePaths)
            report.AppendLine(path);

        if (texturePaths.Length == 0)
            report.AppendLine("NONE FOUND");

        report.AppendLine();
    }

    private static string FindLikelyBones(
        GameObject prefab)
    {
        string[] names =
            prefab
                .GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                {
                    string lower =
                        transform.name.ToLowerInvariant();

                    return
                        lower.Contains("root") ||
                        lower.Contains("body") ||
                        lower.Contains("head") ||
                        lower.Contains("leg") ||
                        lower.Contains("spine") ||
                        lower.Contains("jaw");
                })
                .Select(transform =>
                    GetHierarchyPath(
                        transform,
                        prefab.transform))
                .Take(100)
                .ToArray();

        return names.Length > 0
            ? string.Join(", ", names)
            : "NONE FOUND";
    }

    private static bool TryGetRendererBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer != null &&
                    !(renderer is ParticleSystemRenderer))
                .ToArray();

        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;

        for (int index = 1;
             index < renderers.Length;
             index++)
        {
            bounds.Encapsulate(
                renderers[index].bounds);
        }

        return true;
    }

    private static string GetSubAssetClipNames(
        string path)
    {
        string[] names =
            AssetDatabase.LoadAllAssetsAtPath(path)
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
        AnimatorStateMachine machine,
        string prefix,
        List<string> states)
    {
        foreach (ChildAnimatorState child in
                 machine.states)
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

        foreach (ChildAnimatorStateMachine child in
                 machine.stateMachines)
        {
            CollectStates(
                child.stateMachine,
                prefix +
                "/" +
                child.stateMachine.name,
                states);
        }
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

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash =
            path.LastIndexOf('/');

        string parent =
            path.Substring(0, slash);

        string folder =
            path.Substring(slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            folder);
    }
}
