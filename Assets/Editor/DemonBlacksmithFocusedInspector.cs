using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DemonBlacksmithFocusedInspector
{
    private const string OutputFolder = "Assets/Generated";
    private const string ReportPath =
        OutputFolder + "/DemonBlacksmithInspection.txt";

    private static readonly string[] SearchTerms =
    {
        "demon",
        "minion",
        "blacksmith",
        "pupinduy",
        "lil_"
    };

    [MenuItem(
        "Tools/Dungeon Game/Phase 3.5/Inspect Demon and Blacksmith Assets",
        priority = 450)]
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
            "DEMON + BLACKSMITH FOCUSED ASSET INSPECTION");

        report.AppendLine(
            "Generated: " +
            DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"));

        report.AppendLine(
            "Unity: " +
            Application.unityVersion);

        report.AppendLine();

        WriteRelevantFolders(
            report,
            relevantPaths);

        WritePrefabs(
            report,
            relevantPaths);

        WriteModels(
            report,
            relevantPaths);

        WriteControllers(
            report,
            relevantPaths);

        WriteAnimationClips(
            report,
            relevantPaths);

        WriteMaterials(
            report,
            relevantPaths);

        File.WriteAllText(
            ReportPath,
            report.ToString());

        AssetDatabase.ImportAsset(
            ReportPath,
            ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();

        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                ReportPath);

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);

        EditorUtility.DisplayDialog(
            "Focused Inspection Complete",
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

    private static void WriteRelevantFolders(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "RELEVANT ROOT FOLDERS");

        report.AppendLine(
            new string('-', 80));

        string[] folders =
            paths
                .Select(GetRelevantRootFolder)
                .Where(folder =>
                    !string.IsNullOrWhiteSpace(folder))
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(folder => folder)
                .ToArray();

        foreach (string folder in folders)
            report.AppendLine(folder);

        if (folders.Length == 0)
            report.AppendLine("NONE FOUND");

        report.AppendLine();
    }

    private static string GetRelevantRootFolder(
        string path)
    {
        string[] parts =
            path.Split('/');

        if (parts.Length < 2)
            return path;

        for (int index = 1;
             index < parts.Length;
             index++)
        {
            string lower =
                parts[index].ToLowerInvariant();

            if (SearchTerms.Any(term =>
                    lower.Contains(
                        term.ToLowerInvariant())))
            {
                return string.Join(
                    "/",
                    parts.Take(
                        Mathf.Min(
                            parts.Length,
                            index + 2)));
            }
        }

        return parts.Length >= 2
            ? "Assets/" + parts[1]
            : path;
    }

    private static void WritePrefabs(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "PREFABS");

        report.AppendLine(
            new string('-', 80));

        string[] prefabPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".prefab",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (prefabPaths.Length == 0)
        {
            report.AppendLine(
                "NONE FOUND");

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

            SkinnedMeshRenderer[] skinned =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            Collider[] colliders =
                prefab.GetComponentsInChildren<Collider>(true);

            int brokenMaterials = 0;
            HashSet<string> materials =
                new HashSet<string>();

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in
                         renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        brokenMaterials++;
                        continue;
                    }

                    materials.Add(
                        material.name +
                        " [" +
                        (material.shader != null
                            ? material.shader.name
                            : "NO SHADER") +
                        "]");

                    if (material.shader == null ||
                        material.shader.name.Contains(
                            "Hidden/InternalErrorShader"))
                    {
                        brokenMaterials++;
                    }
                }
            }

            report.AppendLine(
                "PREFAB: " +
                prefab.name);

            report.AppendLine(
                "Path: " +
                path);

            report.AppendLine(
                "Animators: " +
                animators.Length);

            report.AppendLine(
                "Skinned renderers: " +
                skinned.Length);

            report.AppendLine(
                "Renderers: " +
                renderers.Length);

            report.AppendLine(
                "Colliders: " +
                colliders.Length);

            report.AppendLine(
                "Broken material slots: " +
                brokenMaterials);

            report.AppendLine(
                "Materials: " +
                (materials.Count > 0
                    ? string.Join(
                        ", ",
                        materials)
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
                "Likely bones: " +
                FindLikelyBones(prefab));

            report.AppendLine(
                new string('-', 40));
        }

        report.AppendLine();
    }

    private static void WriteModels(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "MODELS / FBX");

        report.AppendLine(
            new string('-', 80));

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
            report.AppendLine(
                "NONE FOUND");

            report.AppendLine();
            return;
        }

        foreach (string path in modelPaths)
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(path)
                as ModelImporter;

            GameObject model =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    path);

            report.AppendLine(
                "MODEL: " +
                Path.GetFileNameWithoutExtension(
                    path));

            report.AppendLine(
                "Path: " +
                path);

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
                "Clips in model: " +
                GetSubAssetClipNames(path));

            if (model != null)
            {
                Animator animator =
                    model.GetComponentInChildren<Animator>(true);

                report.AppendLine(
                    "Model avatar: " +
                    (animator != null &&
                     animator.avatar != null
                        ? animator.avatar.name +
                          " | valid=" +
                          animator.avatar.isValid +
                          " | human=" +
                          animator.avatar.isHuman
                        : "NONE"));
            }

            report.AppendLine(
                new string('-', 40));
        }

        report.AppendLine();
    }

    private static void WriteControllers(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "ANIMATOR CONTROLLERS");

        report.AppendLine(
            new string('-', 80));

        string[] controllerPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".controller",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (controllerPaths.Length == 0)
        {
            report.AppendLine(
                "NONE FOUND");

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
                "States:");

            foreach (string state in states)
            {
                report.AppendLine(
                    "  - " +
                    state);
            }

            report.AppendLine(
                new string('-', 40));
        }

        report.AppendLine();
    }

    private static void WriteAnimationClips(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "ANIMATION CLIPS");

        report.AppendLine(
            new string('-', 80));

        List<string> clipLines =
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

                clipLines.Add(
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

        foreach (string line in
                 clipLines
                    .Distinct()
                    .OrderBy(line => line))
        {
            report.AppendLine(line);
        }

        if (clipLines.Count == 0)
            report.AppendLine("NONE FOUND");

        report.AppendLine();
    }

    private static void WriteMaterials(
        StringBuilder report,
        List<string> paths)
    {
        report.AppendLine(
            "MATERIAL ASSETS");

        report.AppendLine(
            new string('-', 80));

        string[] materialPaths =
            paths
                .Where(path =>
                    path.EndsWith(
                        ".mat",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        if (materialPaths.Length == 0)
        {
            report.AppendLine(
                "NONE FOUND");

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

    private static string FindLikelyBones(
        GameObject prefab)
    {
        string[] bones =
            prefab
                .GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                {
                    string lower =
                        transform.name.ToLowerInvariant();

                    return
                        lower.Contains("hand") ||
                        lower.Contains("hips") ||
                        lower.Contains("pelvis") ||
                        lower.Contains("root") ||
                        lower.Contains("head");
                })
                .Select(transform =>
                    GetHierarchyPath(
                        transform,
                        prefab.transform))
                .Take(40)
                .ToArray();

        return bones.Length > 0
            ? string.Join(
                ", ",
                bones)
            : "NONE FOUND";
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
            ? string.Join(
                ", ",
                names)
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

        return string.Join(
            "/",
            parts);
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
}
