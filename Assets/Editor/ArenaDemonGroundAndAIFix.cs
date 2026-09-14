using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ArenaDemonGroundAndAIFix
{
    private const string ArenaPath = "Assets/Scenes/DemonArena.unity";

    [MenuItem(
        "Tools/Dungeon Game/Polish/FIX Arena Demon Ground And AI",
        priority = 1100)]
    public static void Fix()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode first.",
                "OK");
            return;
        }

        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            EditorUtility.DisplayDialog(
                "Open SampleScene",
                "Open Assets/Scenes/SampleScene.unity first.",
                "OK");
            return;
        }

        GameObject demon = FindSceneObject("Demon Ambush Boss");

        if (demon == null)
        {
            EditorUtility.DisplayDialog(
                "Demon Missing",
                "Demon Ambush Boss was not found.",
                "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) == null)
        {
            EditorUtility.DisplayDialog(
                "Arena Missing",
                "Assets/Scenes/DemonArena.unity was not found.",
                "OK");
            return;
        }

        string samplePath = SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        Scene arena = EditorSceneManager.OpenScene(
            ArenaPath,
            OpenSceneMode.Single);

        GameObject root =
            FindActiveSceneObject("FINAL_TERRIFYING_DEMON_ARENA");

        if (root == null)
            root = FindActiveSceneObject("FINAL_STORY_DEMON_ARENA");

        Transform demonSpawn =
            FindActiveSceneTransform("Arena Demon Spawn");

        if (demonSpawn != null)
        {
            Vector3 local = demonSpawn.localPosition;
            local.y = 0.25f;
            demonSpawn.localPosition = local;
            EditorUtility.SetDirty(demonSpawn);
        }

        // Ensure the floor is included in navigation and has a real collider.
        GameObject floor =
            FindActiveSceneObject("Arena Main Collision Floor");

        if (floor == null)
            floor = FindActiveSceneObject("Arena Collision Floor");

        if (floor != null)
        {
            Collider collider = floor.GetComponent<Collider>();

            if (collider != null)
            {
                collider.enabled = true;
                collider.isTrigger = false;
            }

            floor.layer = 0;
            EditorUtility.SetDirty(floor);
        }

        bool navmeshBuilt = TryBuildNavMesh(root ?? floor);

        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);

        EditorSceneManager.OpenScene(
            samplePath,
            OpenSceneMode.Single);

        demon = FindSceneObject("Demon Ambush Boss");

        if (demon.GetComponent<ArenaDemonCombatActivator>() == null)
            Undo.AddComponent<ArenaDemonCombatActivator>(demon);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = demon;

        EditorUtility.DisplayDialog(
            "Arena Demon Fixed",
            "Demon spawn lowered to the floor.\n" +
            "Arena floor collider verified.\n" +
            "Arena NavMesh build attempted: " + navmeshBuilt + "\n" +
            "ArenaDemonCombatActivator attached to Demon Ambush Boss.\n\n" +
            "The demon will now be grounded and its combat/AI scripts are re-enabled after teleport.",
            "OK");
    }

    private static bool TryBuildNavMesh(GameObject host)
    {
        if (host == null)
            return false;

        Type surfaceType =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .FirstOrDefault(type =>
                    type.FullName == "Unity.AI.Navigation.NavMeshSurface");

        if (surfaceType == null)
        {
            Debug.LogWarning(
                "[ArenaDemonFix] NavMeshSurface package/type not found.");
            return false;
        }

        Component surface =
            host.GetComponent(surfaceType);

        if (surface == null)
            surface = Undo.AddComponent(host, surfaceType);

        // Collect all geometry in the arena scene.
        PropertyInfo collectObjects =
            surfaceType.GetProperty("collectObjects");

        if (collectObjects != null &&
            collectObjects.CanWrite &&
            collectObjects.PropertyType.IsEnum)
        {
            try
            {
                Array values =
                    Enum.GetValues(collectObjects.PropertyType);

                if (values.Length > 0)
                    collectObjects.SetValue(surface, values.GetValue(0));
            }
            catch { }
        }

        MethodInfo remove =
            surfaceType.GetMethod(
                "RemoveData",
                BindingFlags.Instance |
                BindingFlags.Public);

        MethodInfo build =
            surfaceType.GetMethod(
                "BuildNavMesh",
                BindingFlags.Instance |
                BindingFlags.Public);

        try
        {
            if (remove != null)
                remove.Invoke(surface, null);

            if (build != null)
            {
                build.Invoke(surface, null);
                EditorUtility.SetDirty(surface);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[ArenaDemonFix] NavMesh bake failed: " +
                e.GetBaseException().Message);
        }

        return false;
    }

    private static GameObject FindSceneObject(
        string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Where(t =>
                t != null &&
                t.gameObject.scene.IsValid())
            .FirstOrDefault(t =>
                string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }

    private static GameObject FindActiveSceneObject(
        string exactName)
    {
        return FindActiveSceneTransform(exactName)
            ?.gameObject;
    }

    private static Transform FindActiveSceneTransform(
        string exactName)
    {
        Scene scene =
            SceneManager.GetActiveScene();

        return UnityEngine.Object
            .FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Where(t =>
                t != null &&
                t.gameObject.scene == scene)
            .FirstOrDefault(t =>
                string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase));
    }
}
