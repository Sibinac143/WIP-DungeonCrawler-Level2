using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class DemonArenaIsolationAndBossFix
{
    private const string ArenaPath = "Assets/Scenes/DemonArena.unity";
    private static readonly Vector3 ArenaWorldOffset = new Vector3(5000f, 0f, 5000f);

    [MenuItem("Tools/Dungeon Game/Polish/FIX Arena Floating Grass And Demon Spawn", priority = 1080)]
    public static void Fix()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first.", "OK");
            return;
        }

        SceneAsset arenaAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath);
        if (arenaAsset == null)
        {
            EditorUtility.DisplayDialog("Arena Missing",
                "Assets/Scenes/DemonArena.unity was not found.", "OK");
            return;
        }

        string previousPath = SceneManager.GetActiveScene().path;

        Scene arena = EditorSceneManager.OpenScene(ArenaPath, OpenSceneMode.Single);

        GameObject arenaRoot = FindInActiveScene("FINAL_STORY_DEMON_ARENA");
        if (arenaRoot == null)
            arenaRoot = FindInActiveScene("DEMON_ARENA_WORLD");

        if (arenaRoot == null)
        {
            EditorUtility.DisplayDialog("Arena Root Missing",
                "Could not find the arena root object.", "OK");
            return;
        }

        // Put the entire arena far outside the main SampleScene terrain.
        // Use an absolute location so running this menu repeatedly is safe.
        arenaRoot.transform.position = ArenaWorldOffset;

        // Ensure all important markers remain children of the arena root.
        ReparentIfNeeded(arenaRoot.transform, "Arena Player Spawn");
        ReparentIfNeeded(arenaRoot.transform, "Arena Demon Spawn");
        ReparentIfNeeded(arenaRoot.transform, "Arena Respawn Points");

        // Make the collision floor visually match the arena and sit at world y=0.
        GameObject floor = FindInActiveScene("Arena Collision Floor");
        if (floor != null)
        {
            floor.transform.SetParent(arenaRoot.transform, true);
            Vector3 local = floor.transform.localPosition;
            local.y = -1f;
            floor.transform.localPosition = local;
        }

        GameObject emergency = FindInActiveScene("Arena Emergency Catch Floor");
        if (emergency != null)
        {
            emergency.transform.SetParent(arenaRoot.transform, true);
            Vector3 local = emergency.transform.localPosition;
            local.y = -6f;
            emergency.transform.localPosition = local;
        }

        // Keep the player and demon spawn points safely above the floor.
        SetLocalY(arenaRoot.transform, "Arena Player Spawn", 2.0f);
        SetLocalY(arenaRoot.transform, "Arena Demon Spawn", 1.5f);

        Transform respawnRoot = FindChildByName(arenaRoot.transform, "Arena Respawn Points");
        if (respawnRoot != null)
        {
            foreach (Transform child in respawnRoot)
            {
                Vector3 p = child.localPosition;
                p.y = 2.0f;
                child.localPosition = p;
            }
        }

        // Try to add and bake an AI Navigation NavMeshSurface using reflection.
        TryBuildNavMeshSurface(arenaRoot);

        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);

        if (!string.IsNullOrEmpty(previousPath))
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);

        GameObject demon = FindInActiveScene("Demon Ambush Boss");
        if (demon != null && demon.GetComponent<ArenaBossAnchor>() == null)
        {
            Undo.AddComponent<ArenaBossAnchor>(demon);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Arena Isolation Fixed",
            "The entire DemonArena was moved to world position (5000, 0, 5000), far away from SampleScene terrain.\n\n" +
            "This removes the floating grass/main-world overlap.\n\n" +
            "The demon now has an arena anchor that places it at Arena Demon Spawn and holds it there briefly so it is waiting for the player.",
            "OK");
    }

    private static void TryBuildNavMeshSurface(GameObject arenaRoot)
    {
        Type surfaceType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.FullName == "Unity.AI.Navigation.NavMeshSurface");

        if (surfaceType == null)
            return;

        Component surface = arenaRoot.GetComponent(surfaceType);
        if (surface == null)
            surface = Undo.AddComponent(arenaRoot, surfaceType);

        PropertyInfo collectObjects = surfaceType.GetProperty("collectObjects");
        if (collectObjects != null && collectObjects.CanWrite)
        {
            // enum value 0 is All in supported AI Navigation versions.
            object allValue = Enum.ToObject(collectObjects.PropertyType, 0);
            collectObjects.SetValue(surface, allValue);
        }

        MethodInfo build = surfaceType.GetMethod(
            "BuildNavMesh",
            BindingFlags.Instance | BindingFlags.Public);

        if (build != null)
        {
            try { build.Invoke(surface, null); }
            catch (Exception e)
            {
                Debug.LogWarning("[ArenaFix] NavMeshSurface build skipped: " + e.Message);
            }
        }
    }

    private static void ReparentIfNeeded(Transform root, string exactName)
    {
        GameObject obj = FindInActiveScene(exactName);
        if (obj != null && obj.transform.parent != root &&
            !obj.transform.IsChildOf(root))
        {
            obj.transform.SetParent(root, true);
        }
    }

    private static void SetLocalY(Transform root, string exactName, float y)
    {
        Transform t = FindChildByName(root, exactName);
        if (t == null)
            return;

        Vector3 p = t.localPosition;
        p.y = y;
        t.localPosition = p;
    }

    private static Transform FindChildByName(Transform root, string exactName)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => string.Equals(
                t.name,
                exactName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static GameObject FindInActiveScene(string exactName)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null &&
                        t.gameObject.scene.IsValid() &&
                        t.gameObject.scene == SceneManager.GetActiveScene())
            .FirstOrDefault(t => string.Equals(
                t.name,
                exactName,
                StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
