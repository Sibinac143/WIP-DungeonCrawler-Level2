using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonArenaFloorFallFix
{
    private const string ArenaPath = "Assets/Scenes/DemonArena.unity";

    [MenuItem("Tools/Dungeon Game/Polish/FIX Demon Arena Floor And Falling", priority = 1070)]
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

        GameObject root = Find("FINAL_STORY_DEMON_ARENA");
        if (root == null)
            root = new GameObject("FINAL_STORY_DEMON_ARENA");

        DestroyIfExists("Arena Collision Floor");
        DestroyIfExists("Arena Emergency Catch Floor");

        Material stone = FindMaterial("ArenaStone");
        if (stone == null)
            stone = FindMaterial("ArenaWall");

        // Primary collision floor: large thick box, much more reliable than cylinder edge collision.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena Collision Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.position = new Vector3(0f, -1.0f, 0f);
        floor.transform.localScale = new Vector3(34f, 2f, 34f);
        floor.layer = 0;

        BoxCollider floorCollider = floor.GetComponent<BoxCollider>();
        floorCollider.isTrigger = false;

        Renderer floorRenderer = floor.GetComponent<Renderer>();
        if (stone != null)
            floorRenderer.sharedMaterial = stone;

        // Emergency catch floor under the arena in case the controller ever tunnels through.
        GameObject catchFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        catchFloor.name = "Arena Emergency Catch Floor";
        catchFloor.transform.SetParent(root.transform, false);
        catchFloor.transform.position = new Vector3(0f, -6f, 0f);
        catchFloor.transform.localScale = new Vector3(42f, 2f, 42f);
        catchFloor.layer = 0;

        BoxCollider catchCollider = catchFloor.GetComponent<BoxCollider>();
        catchCollider.isTrigger = false;

        Renderer catchRenderer = catchFloor.GetComponent<Renderer>();
        if (stone != null)
            catchRenderer.sharedMaterial = stone;

        // Raise all arena spawn points so CharacterController starts clearly above the floor.
        foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || !t.gameObject.scene.IsValid())
                continue;

            if (t.name == "Arena Player Spawn" ||
                t.name.StartsWith("Arena Respawn ", StringComparison.OrdinalIgnoreCase))
            {
                Vector3 p = t.position;
                p.y = 2.0f;
                t.position = p;
                EditorUtility.SetDirty(t);
            }

            if (t.name == "Arena Demon Spawn")
            {
                Vector3 p = t.position;
                p.y = 1.5f;
                t.position = p;
                EditorUtility.SetDirty(t);
            }
        }

        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);

        if (!string.IsNullOrEmpty(previousPath))
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);

        // Attach emergency fall recovery to the demon controller in SampleScene.
        GameObject demon = Find("Demon Ambush Boss");
        if (demon != null && demon.GetComponent<ArenaFallRecovery>() == null)
        {
            Undo.AddComponent<ArenaFallRecovery>(demon);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Arena Floor Fixed",
            "Added a thick 34x34 collision floor, an emergency catch floor, raised arena spawn points, and added runtime fall recovery.",
            "OK");
    }

    private static Material FindMaterial(string contains)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Generated" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null &&
                mat.name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                return mat;
        }

        return null;
    }

    private static GameObject Find(string exactName)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(t.name, exactName, StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }

    private static void DestroyIfExists(string exactName)
    {
        GameObject obj = Find(exactName);
        if (obj != null)
            UnityEngine.Object.DestroyImmediate(obj);
    }
}
