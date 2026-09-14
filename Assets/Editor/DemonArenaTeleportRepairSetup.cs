using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonArenaTeleportRepairSetup
{
    private const string ArenaPath = "Assets/Scenes/DemonArena.unity";

    [MenuItem("Tools/Dungeon Game/Polish/REPAIR Demon Arena Teleport", priority = 1045)]
    public static void Repair()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode first.",
                "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) == null)
        {
            EditorUtility.DisplayDialog(
                "DemonArena Missing",
                "Assets/Scenes/DemonArena.unity does not exist.\n\nRun Build Demon Teleport Arena World first.",
                "OK");
            return;
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s =>
            string.Equals(s.path, ArenaPath, StringComparison.OrdinalIgnoreCase)))
        {
            scenes.Add(new EditorBuildSettingsScene(ArenaPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        else
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(
                    scenes[i].path,
                    ArenaPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    scenes[i] = new EditorBuildSettingsScene(ArenaPath, true);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        GameObject host = FindSceneObject("PHASE_03_QUEST_SYSTEM");
        if (host == null)
            host = new GameObject("PHASE_03_QUEST_SYSTEM");

        // Remove older versions so only one system controls the teleport.
        Component oldSequence = host.GetComponents<Component>()
            .FirstOrDefault(c =>
                c != null &&
                c.GetType().Name == "DemonArenaTeleportSequence");

        if (oldSequence != null)
            Undo.DestroyObjectImmediate(oldSequence);

        DemonArenaTeleportRepair repair =
            host.GetComponent<DemonArenaTeleportRepair>();

        if (repair == null)
            repair = Undo.AddComponent<DemonArenaTeleportRepair>(host);

        EditorUtility.SetDirty(repair);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = host;

        EditorUtility.DisplayDialog(
            "Demon Arena Teleport Repaired",
            "The new trigger ignores quest-stage mismatches.\n\n" +
            "When the active Demon Ambush Boss is within 22 meters of the player, " +
            "the arena teleport starts.\n\n" +
            "DemonArena was also forced into Build Settings.",
            "OK");
    }

    [MenuItem("Tools/Dungeon Game/Polish/Check Demon Arena Teleport")]
    public static void Check()
    {
        GameObject player = FindSceneObject("player");
        GameObject demon = FindSceneObject("Demon Ambush Boss");
        GameObject host = FindSceneObject("PHASE_03_QUEST_SYSTEM");

        bool arenaExists =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) != null;

        bool arenaEnabled =
            EditorBuildSettings.scenes.Any(s =>
                s.enabled &&
                string.Equals(
                    s.path,
                    ArenaPath,
                    StringComparison.OrdinalIgnoreCase));

        bool repairAttached =
            host != null &&
            host.GetComponent<DemonArenaTeleportRepair>() != null;

        string result =
            "Player found: " + (player != null) + "\n" +
            "Demon Ambush Boss found: " + (demon != null) + "\n" +
            "Demon active: " + (demon != null && demon.activeInHierarchy) + "\n" +
            "DemonArena scene exists: " + arenaExists + "\n" +
            "DemonArena enabled in Build Settings: " + arenaEnabled + "\n" +
            "Repair component attached: " + repairAttached;

        EditorUtility.DisplayDialog(
            "Demon Arena Check",
            result,
            "OK");
    }

    private static GameObject FindSceneObject(string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t =>
                string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
