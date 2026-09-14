using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonArenaReturnAfterVictorySetup
{
    [MenuItem(
        "Tools/Dungeon Game/Polish/ADD Return After Demon Victory",
        priority = 1110)]
    public static void Apply()
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

        GameObject demon =
            UnityEngine.Object
                .FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .Where(t =>
                    t != null &&
                    t.gameObject.scene.IsValid())
                .FirstOrDefault(t =>
                    string.Equals(
                        t.name,
                        "Demon Ambush Boss",
                        StringComparison.OrdinalIgnoreCase))
                ?.gameObject;

        if (demon == null)
        {
            EditorUtility.DisplayDialog(
                "Demon Missing",
                "Demon Ambush Boss was not found.",
                "OK");
            return;
        }

        if (demon.GetComponent<DemonArenaReturnAfterVictory>() == null)
            Undo.AddComponent<DemonArenaReturnAfterVictory>(demon);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = demon;

        EditorUtility.DisplayDialog(
            "Victory Return Added",
            "The player's main-world position is now saved before arena teleport.\n\n" +
            "When Demon Ambush Boss dies in DemonArena, the screen fades and the player returns to that position. DemonArena is then unloaded.",
            "OK");
    }
}
