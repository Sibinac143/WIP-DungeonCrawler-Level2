using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixMixamoBlacksmithMaterials
{
    [MenuItem(
        "Tools/Dungeon Game/Polish/Fix Mixamo Blacksmith Materials",
        priority = 1020)]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before fixing the blacksmith materials.",
                "OK");
            return;
        }

        GameObject prisoner =
            FindSceneObject("Imprisoned Key Maker");

        if (prisoner == null)
        {
            EditorUtility.DisplayDialog(
                "Blacksmith Missing",
                "Imprisoned Key Maker was not found.",
                "OK");
            return;
        }

        Transform captiveTransform =
            prisoner.transform.Find(
                "Blacksmith Captive Mixamo");

        if (captiveTransform == null)
        {
            EditorUtility.DisplayDialog(
                "Captive Visual Missing",
                "Blacksmith Captive Mixamo was not found. Build the captive animation system first.",
                "OK");
            return;
        }

        GameObject originalVisual =
            prisoner.transform
                .Cast<Transform>()
                .Select(child => child.gameObject)
                .FirstOrDefault(child =>
                    child != captiveTransform.gameObject &&
                    child.GetComponentInChildren<Renderer>(true) != null &&
                    child.name.IndexOf(
                        "Blacksmith",
                        StringComparison.OrdinalIgnoreCase) >= 0);

        if (originalVisual == null)
        {
            originalVisual =
                prisoner
                    .GetComponentsInChildren<Renderer>(true)
                    .Select(renderer => renderer.transform.root.gameObject)
                    .FirstOrDefault(root =>
                        root != captiveTransform.gameObject);
        }

        if (originalVisual == null)
        {
            EditorUtility.DisplayDialog(
                "Original Visual Missing",
                "The original blacksmith visual could not be found.",
                "OK");
            return;
        }

        Renderer[] sourceRenderers =
            originalVisual
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.sharedMaterials != null &&
                    renderer.sharedMaterials.Length > 0)
                .ToArray();

        Renderer[] targetRenderers =
            captiveTransform
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.sharedMaterials != null)
                .ToArray();

        if (sourceRenderers.Length == 0 ||
            targetRenderers.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Renderer Missing",
                "Could not find both source and target renderers.",
                "OK");
            return;
        }

        List<Material> sourceMaterials =
            sourceRenderers
                .SelectMany(renderer =>
                    renderer.sharedMaterials)
                .Where(material =>
                    material != null)
                .Distinct()
                .ToList();

        Material body1 =
            FindMaterial(
                sourceMaterials,
                "Body_1");

        Material body2 =
            FindMaterial(
                sourceMaterials,
                "Body_2");

        Material legs =
            FindMaterial(
                sourceMaterials,
                "Legs");

        int changed = 0;

        foreach (Renderer target in targetRenderers)
        {
            Material[] current =
                target.sharedMaterials;

            Material[] replacement =
                new Material[
                    Math.Max(
                        1,
                        current.Length)];

            for (int index = 0;
                 index < replacement.Length;
                 index++)
            {
                replacement[index] =
                    ChooseMaterial(
                        target.name,
                        index,
                        replacement.Length,
                        body1,
                        body2,
                        legs,
                        sourceMaterials);
            }

            Undo.RecordObject(
                target,
                "Fix Blacksmith Materials");

            target.sharedMaterials =
                replacement;

            target.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.On;

            target.receiveShadows = true;

            EditorUtility.SetDirty(target);
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Blacksmith Materials Fixed",
            "Applied the original blacksmith body and clothing materials to " +
            changed +
            " Mixamo renderer(s).",
            "OK");
    }

    private static Material ChooseMaterial(
        string rendererName,
        int slotIndex,
        int slotCount,
        Material body1,
        Material body2,
        Material legs,
        List<Material> fallback)
    {
        string lower =
            rendererName.ToLowerInvariant();

        if (lower.Contains("leg") &&
            legs != null)
        {
            return legs;
        }

        if (slotCount >= 3)
        {
            if (slotIndex == 0 &&
                body1 != null)
            {
                return body1;
            }

            if (slotIndex == 1 &&
                body2 != null)
            {
                return body2;
            }

            if (slotIndex >= 2 &&
                legs != null)
            {
                return legs;
            }
        }

        if (slotCount == 2)
        {
            if (slotIndex == 0 &&
                body1 != null)
            {
                return body1;
            }

            if (slotIndex == 1 &&
                body2 != null)
            {
                return body2;
            }
        }

        if (body1 != null)
            return body1;

        return fallback.FirstOrDefault();
    }

    private static Material FindMaterial(
        IEnumerable<Material> materials,
        string keyword)
    {
        return materials.FirstOrDefault(
            material =>
                material.name.IndexOf(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static GameObject FindSceneObject(
        string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Where(transform =>
                transform != null &&
                transform.gameObject.scene.IsValid())
            .FirstOrDefault(transform =>
                string.Equals(
                    transform.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
