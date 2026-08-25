using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GateKeyVisualLifecycleSetup
{
    [MenuItem(
        "Tools/Dungeon Game/Polish/Hide Gate Key After Use",
        priority = 1030)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before applying the gate-key fix.",
                "OK");
            return;
        }

        QuestManager manager =
            UnityEngine.Object.FindAnyObjectByType<QuestManager>(
                FindObjectsInactive.Include);

        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Quest Manager Missing",
                "QuestManager was not found in the active scene.",
                "OK");
            return;
        }

        List<GameObject> keyVisuals =
            UnityEngine.Object
                .FindObjectsByType<Transform>(
                    FindObjectsInactive.Include)
                .Where(transform =>
                    transform != null &&
                    transform.gameObject.scene.IsValid())
                .Where(transform =>
                {
                    string lower =
                        transform.name.ToLowerInvariant();

                    return
                        lower.Contains("final gate key preview") ||
                        lower.Contains("gate key preview") ||
                        lower.Contains("key popup") ||
                        lower.Contains("key display") ||
                        lower.Contains("held gate key") ||
                        lower.Contains("player gate key");
                })
                .Select(transform =>
                    transform.gameObject)
                .Distinct()
                .ToList();

        GameObject phaseFour =
            FindSceneObject("PHASE_04_FINAL_GATE");

        GameObject host =
            phaseFour != null
                ? phaseFour
                : manager.gameObject;

        GateKeyVisualLifecycle lifecycle =
            host.GetComponent<GateKeyVisualLifecycle>();

        if (lifecycle == null)
        {
            lifecycle =
                Undo.AddComponent<GateKeyVisualLifecycle>(
                    host);
        }

        lifecycle.Configure(
            manager,
            keyVisuals);

        EditorUtility.SetDirty(lifecycle);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string found =
            keyVisuals.Count > 0
                ? string.Join(
                    "\n",
                    keyVisuals.Select(
                        visual =>
                            "• " + visual.name))
                : "No named key preview object was found. The lifecycle component was still added.";

        EditorUtility.DisplayDialog(
            "Gate Key Lifecycle Fixed",
            "The gate key will now disappear as soon as it is used and the quest enters Defeat Gate Guardians.\n\nDetected key visuals:\n" +
            found,
            "OK");
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
