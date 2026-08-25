using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LevelTwoPolishInspector
{
    private const string OutputFolder = "Assets/Generated";
    private const string ReportPath =
        OutputFolder + "/LevelTwoPolishInspection.txt";

    [MenuItem(
        "Tools/Dungeon Game/Polish/Inspect FPV, Blacksmith, Castle and Shadows",
        priority = 1000)]
    public static void Inspect()
    {
        EnsureFolder(OutputFolder);

        StringBuilder report = new StringBuilder();

        report.AppendLine("LEVEL 2 POLISH INSPECTION");
        report.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("Unity: " + Application.unityVersion);
        report.AppendLine("Scene: " + SceneManager.GetActiveScene().path);
        report.AppendLine();

        WriteSceneLights(report);
        WritePlayerFpv(report);
        WriteBlacksmith(report);
        WriteCastleAssets(report);
        WriteShadowSummary(report);

        File.WriteAllText(ReportPath, report.ToString());

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
            "Polish Inspection Complete",
            "Created:\n" + ReportPath +
            "\n\nUpload this text file here.",
            "OK");
    }

    private static void WriteSceneLights(StringBuilder report)
    {
        report.AppendLine("SCENE LIGHTS");
        report.AppendLine(new string('-', 90));

        Light[] lights =
            UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include);

        foreach (Light light in lights
                     .Where(light =>
                         light != null &&
                         light.gameObject.scene.IsValid())
                     .OrderBy(light => light.name))
        {
            report.AppendLine(
                light.name +
                " | type=" + light.type +
                " | enabled=" + light.enabled +
                " | intensity=" + light.intensity.ToString("0.###") +
                " | shadows=" + light.shadows +
                " | shadowStrength=" + light.shadowStrength.ToString("0.###") +
                " | range=" + light.range.ToString("0.###") +
                " | color=" + ColorUtility.ToHtmlStringRGBA(light.color));
        }

        report.AppendLine();
        report.AppendLine(
            "RenderSettings | fog=" + RenderSettings.fog +
            " | fogMode=" + RenderSettings.fogMode +
            " | fogDensity=" + RenderSettings.fogDensity.ToString("0.####") +
            " | ambientMode=" + RenderSettings.ambientMode +
            " | ambientIntensity=" + RenderSettings.ambientIntensity.ToString("0.###") +
            " | reflectionIntensity=" + RenderSettings.reflectionIntensity.ToString("0.###"));
        report.AppendLine();
    }

    private static void WritePlayerFpv(StringBuilder report)
    {
        report.AppendLine("FPV PLAYER / HAND / WEAPON");
        report.AppendLine(new string('-', 90));

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        if (player == null)
        {
            report.AppendLine("PLAYER NOT FOUND");
            report.AppendLine();
            return;
        }

        report.AppendLine("Player: " + GetHierarchyPath(player.transform));

        Camera[] cameras =
            player.GetComponentsInChildren<Camera>(true);

        foreach (Camera camera in cameras)
        {
            report.AppendLine(
                "Camera: " + GetHierarchyPath(camera.transform) +
                " | FOV=" + camera.fieldOfView.ToString("0.##") +
                " | near=" + camera.nearClipPlane.ToString("0.###") +
                " | far=" + camera.farClipPlane.ToString("0.###"));
        }

        Transform[] transforms =
            player.GetComponentsInChildren<Transform>(true);

        foreach (Transform transform in transforms)
        {
            string lower = transform.name.ToLowerInvariant();

            if (!(lower.Contains("hand") ||
                  lower.Contains("arm") ||
                  lower.Contains("sword") ||
                  lower.Contains("weapon") ||
                  lower.Contains("holder")))
            {
                continue;
            }

            report.AppendLine(
                "Candidate: " + GetHierarchyPath(transform) +
                " | localPosition=" + transform.localPosition.ToString("F3") +
                " | localRotation=" + transform.localEulerAngles.ToString("F2") +
                " | localScale=" + transform.localScale.ToString("F3"));

            Renderer[] renderers =
                transform.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                report.AppendLine(
                    "  Renderer: " + GetHierarchyPath(renderer.transform) +
                    " | type=" + renderer.GetType().Name +
                    " | enabled=" + renderer.enabled +
                    " | cast=" + renderer.shadowCastingMode +
                    " | receive=" + renderer.receiveShadows +
                    " | materials=" + string.Join(
                        ", ",
                        renderer.sharedMaterials.Select(
                            material =>
                                material != null
                                    ? material.name +
                                      "[" +
                                      (material.shader != null
                                          ? material.shader.name
                                          : "NO SHADER") +
                                      "]"
                                    : "NULL")));
            }

            Animator animator =
                transform.GetComponentInChildren<Animator>(true);

            if (animator != null)
            {
                report.AppendLine(
                    "  Animator: controller=" +
                    (animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController)
                        : "NONE") +
                    " | avatar=" +
                    (animator.avatar != null
                        ? animator.avatar.name
                        : "NONE"));
            }
        }

        report.AppendLine();
    }

    private static void WriteBlacksmith(StringBuilder report)
    {
        report.AppendLine("BLACKSMITH STATES / ANIMATIONS");
        report.AppendLine(new string('-', 90));

        string[] sceneNames =
        {
            "Imprisoned Key Maker",
            "Workshop Blacksmith - Forging",
            "Workshop Blacksmith",
            "NPC_Blacksmith V1",
            "NPC_Blacksmith V1_Forging"
        };

        foreach (string name in sceneNames)
        {
            GameObject found = FindSceneObject(name);

            if (found == null)
                continue;

            report.AppendLine("Scene object: " + GetHierarchyPath(found.transform));

            Animator[] animators =
                found.GetComponentsInChildren<Animator>(true);

            foreach (Animator animator in animators)
            {
                report.AppendLine(
                    "  Animator: " + GetHierarchyPath(animator.transform) +
                    " | enabled=" + animator.enabled +
                    " | applyRootMotion=" + animator.applyRootMotion +
                    " | controller=" +
                    (animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController)
                        : "NONE"));
            }

            Animation[] legacy =
                found.GetComponentsInChildren<Animation>(true);

            foreach (Animation animation in legacy)
            {
                report.AppendLine(
                    "  Legacy Animation: " +
                    GetHierarchyPath(animation.transform));

                foreach (AnimationState state in animation)
                {
                    report.AppendLine(
                        "    Clip: " + state.name +
                        " | length=" + state.length.ToString("0.###") +
                        " | wrap=" + state.wrapMode);
                }
            }
        }

        string[] allPaths = AssetDatabase.GetAllAssetPaths();

        string[] blacksmithPaths =
            allPaths
                .Where(path =>
                    path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                    path.ToLowerInvariant().Contains("blacksmith"))
                .OrderBy(path => path)
                .ToArray();

        report.AppendLine();
        report.AppendLine("Blacksmith asset paths:");

        foreach (string path in blacksmithPaths)
            report.AppendLine(path);

        report.AppendLine();
        report.AppendLine("Blacksmith clips:");

        HashSet<string> clips = new HashSet<string>();

        foreach (string path in blacksmithPaths)
        {
            foreach (AnimationClip clip in
                     AssetDatabase.LoadAllAssetsAtPath(path)
                         .OfType<AnimationClip>())
            {
                if (clip.name.StartsWith("__preview__"))
                    continue;

                clips.Add(
                    clip.name +
                    " | path=" + path +
                    " | length=" + clip.length.ToString("0.###") +
                    " | loop=" + clip.isLooping +
                    " | humanMotion=" + clip.humanMotion);
            }
        }

        foreach (string clip in clips.OrderBy(value => value))
            report.AppendLine(clip);

        report.AppendLine();
    }

    private static void WriteCastleAssets(StringBuilder report)
    {
        report.AppendLine("CASTLE / DUNGEON ASSETS");
        report.AppendLine(new string('-', 90));

        string[] allPaths = AssetDatabase.GetAllAssetPaths();

        string[] keywords =
        {
            "castle",
            "dungeon",
            "ruin",
            "wall",
            "tower",
            "gate",
            "prison",
            "cell",
            "arch"
        };

        string[] matching =
            allPaths
                .Where(path =>
                    path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
                    (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) ||
                     path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase)))
                .Where(path =>
                {
                    string lower = path.ToLowerInvariant();
                    return keywords.Any(keyword => lower.Contains(keyword));
                })
                .OrderBy(path => path)
                .Take(500)
                .ToArray();

        foreach (string path in matching)
            report.AppendLine(path);

        report.AppendLine();
        report.AppendLine("Current castle-related scene objects:");

        Transform[] transforms =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include);

        foreach (Transform transform in transforms
                     .Where(transform =>
                         transform != null &&
                         transform.gameObject.scene.IsValid())
                     .Where(transform =>
                     {
                         string lower = transform.name.ToLowerInvariant();
                         return keywords.Any(keyword => lower.Contains(keyword));
                     })
                     .OrderBy(transform => transform.name)
                     .Take(300))
        {
            report.AppendLine(
                GetHierarchyPath(transform) +
                " | position=" + transform.position.ToString("F2") +
                " | scale=" + transform.lossyScale.ToString("F2"));
        }

        report.AppendLine();
    }

    private static void WriteShadowSummary(StringBuilder report)
    {
        report.AppendLine("SHADOW SUMMARY");
        report.AppendLine(new string('-', 90));

        Renderer[] renderers =
            UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include);

        Renderer[] sceneRenderers =
            renderers
                .Where(renderer =>
                    renderer != null &&
                    renderer.gameObject.scene.IsValid())
                .ToArray();

        int castOn =
            sceneRenderers.Count(renderer =>
                renderer.shadowCastingMode != ShadowCastingMode.Off);

        int castOff =
            sceneRenderers.Length - castOn;

        int receiveOn =
            sceneRenderers.Count(renderer => renderer.receiveShadows);

        int receiveOff =
            sceneRenderers.Length - receiveOn;

        report.AppendLine("Scene renderers: " + sceneRenderers.Length);
        report.AppendLine("Casting shadows: " + castOn);
        report.AppendLine("Not casting shadows: " + castOff);
        report.AppendLine("Receiving shadows: " + receiveOn);
        report.AppendLine("Not receiving shadows: " + receiveOff);

        report.AppendLine();
        report.AppendLine("Largest shadow-casting renderers:");

        foreach (Renderer renderer in sceneRenderers
                     .Where(renderer =>
                         renderer.shadowCastingMode != ShadowCastingMode.Off)
                     .OrderByDescending(renderer =>
                         renderer.bounds.size.sqrMagnitude)
                     .Take(80))
        {
            report.AppendLine(
                GetHierarchyPath(renderer.transform) +
                " | type=" + renderer.GetType().Name +
                " | bounds=" + renderer.bounds.size.ToString("F2") +
                " | cast=" + renderer.shadowCastingMode +
                " | receive=" + renderer.receiveShadows);
        }

        report.AppendLine();
    }

    private static GameObject FindSceneObject(string exactName)
    {
        foreach (Transform transform in
                 UnityEngine.Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include))
        {
            if (transform == null ||
                !transform.gameObject.scene.IsValid())
            {
                continue;
            }

            if (string.Equals(
                    transform.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return transform.gameObject;
            }
        }

        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        List<string> parts = new List<string>();

        Transform current = transform;

        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');

        string parent = path.Substring(0, slash);
        string folder = path.Substring(slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }
}
