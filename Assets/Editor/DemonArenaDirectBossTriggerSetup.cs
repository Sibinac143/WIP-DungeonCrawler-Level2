
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonArenaDirectBossTriggerSetup
{
    const string ArenaPath = "Assets/Scenes/DemonArena.unity";

    [MenuItem("Tools/Dungeon Game/Polish/FORCE Attach Arena Trigger To Demon Boss")]
    public static void Attach()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode","Exit Play Mode first.","OK");
            return;
        }

        var demon = Find("Demon Ambush Boss");
        var player = Find("player");

        if (demon == null || player == null)
        {
            EditorUtility.DisplayDialog("Wrong Scene",
                "Open SampleScene first.\nPlayer found: " + (player != null) +
                "\nDemon found: " + (demon != null), "OK");
            return;
        }

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => string.Equals(s.path,ArenaPath,StringComparison.OrdinalIgnoreCase)))
            scenes.Add(new EditorBuildSettingsScene(ArenaPath,true));
        else
            scenes = scenes.Select(s => string.Equals(s.path,ArenaPath,StringComparison.OrdinalIgnoreCase)
                ? new EditorBuildSettingsScene(ArenaPath,true) : s).ToList();
        EditorBuildSettings.scenes = scenes.ToArray();

        foreach (var c in UnityEngine.Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
        {
            if (c == null || !c.gameObject.scene.IsValid()) continue;
            var n = c.GetType().Name;
            if (n == "DemonArenaTeleportSequence" || n == "DemonArenaTeleportRepair")
                Undo.DestroyObjectImmediate(c);
        }

        if (demon.GetComponent<DemonArenaDirectBossTrigger>() == null)
            Undo.AddComponent<DemonArenaDirectBossTrigger>(demon);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = demon;

        EditorUtility.DisplayDialog("Attached",
            "Direct arena trigger is now on Demon Ambush Boss.\nIt triggers within 26m and does not depend on quest stage.",
            "OK");
    }

    [MenuItem("Tools/Dungeon Game/Polish/VERIFY Direct Demon Arena Trigger")]
    public static void Verify()
    {
        var demon = Find("Demon Ambush Boss");
        var player = Find("player");
        bool trigger = demon != null && demon.GetComponent<DemonArenaDirectBossTrigger>() != null;
        bool arena = AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaPath) != null;
        bool enabled = EditorBuildSettings.scenes.Any(s => s.enabled &&
            string.Equals(s.path,ArenaPath,StringComparison.OrdinalIgnoreCase));

        EditorUtility.DisplayDialog("Direct Arena Verification",
            "Scene: " + SceneManager.GetActiveScene().name +
            "\nPlayer found: " + (player != null) +
            "\nDemon found: " + (demon != null) +
            "\nDirect trigger on demon: " + trigger +
            "\nDemonArena exists: " + arena +
            "\nDemonArena enabled: " + enabled, "OK");
    }

    static GameObject Find(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
