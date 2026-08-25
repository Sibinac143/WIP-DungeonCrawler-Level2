using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelTwoTitleScreenSetup
{
    private const string GameplayScenePath =
        "Assets/Scenes/SampleScene.unity";

    private const string TitleScenePath =
        "Assets/Scenes/TitleScreen.unity";

    private const string TitleSceneName =
        "TitleScreen";

    [MenuItem(
        "Tools/Dungeon Game/Finalize/Build Level 2 Title Screen and Ending",
        priority = 910)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before creating the title screen.",
                "OK");
            return;
        }

        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        SceneAsset gameplayScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                GameplayScenePath);

        if (gameplayScene == null)
        {
            EditorUtility.DisplayDialog(
                "Gameplay Scene Missing",
                "Could not find:\n" +
                GameplayScenePath,
                "OK");
            return;
        }

        AttachCompletionReturnToGameplay();
        CreateTitleScene();
        ConfigureBuildSettings();

        EditorSceneManager.OpenScene(
            TitleScenePath,
            OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Level 2 Title Screen Built",
            "Created TitleScreen.unity and made it the first scene in Build Settings.\n\n" +
            "The title reads LEVEL II — REALM OF DEATH.\n" +
            "Begin Level II starts a fresh mission.\n" +
            "Continue appears when unfinished progress exists.\n" +
            "Completing the mission shows the escape message, fades out, resets the completed save, and returns to this title screen automatically.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Finalize/Open Level 2 Title Screen",
        priority = 911)]
    public static void OpenTitle()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                TitleScenePath) == null)
        {
            Build();
            return;
        }

        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(
            TitleScenePath,
            OpenSceneMode.Single);
    }

    [MenuItem(
        "Tools/Dungeon Game/Finalize/Open Level 2 Gameplay",
        priority = 912)]
    public static void OpenGameplay()
    {
        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(
            GameplayScenePath,
            OpenSceneMode.Single);
    }

    private static void AttachCompletionReturnToGameplay()
    {
        Scene gameplayScene =
            EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);

        GameObject questRoot =
            FindSceneObject(
                "PHASE_03_QUEST_SYSTEM");

        QuestManager manager =
            questRoot != null
                ? questRoot.GetComponent<QuestManager>()
                : UnityEngine.Object
                    .FindFirstObjectByType<QuestManager>(
                        FindObjectsInactive.Include);

        if (manager == null)
        {
            throw new InvalidOperationException(
                "QuestManager was not found in SampleScene.");
        }

        ReturnToTitleOnCompletion returner =
            manager.GetComponent<ReturnToTitleOnCompletion>();

        if (returner == null)
        {
            returner =
                Undo.AddComponent<ReturnToTitleOnCompletion>(
                    manager.gameObject);
        }

        returner.Configure(
            manager,
            TitleSceneName,
            6f);

        EditorUtility.SetDirty(
            returner);

        EditorSceneManager.MarkSceneDirty(
            gameplayScene);

        EditorSceneManager.SaveScene(
            gameplayScene);
    }

    private static void CreateTitleScene()
    {
        Scene titleScene =
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

        titleScene.name =
            TitleSceneName;

        GameObject cameraObject =
            new GameObject(
                "Title Camera");

        Camera camera =
            cameraObject.AddComponent<Camera>();

        camera.clearFlags =
            CameraClearFlags.SolidColor;

        camera.backgroundColor =
            new Color(
                0.005f,
                0.008f,
                0.018f,
                1f);

        camera.orthographic = true;
        camera.depth = -100f;

        cameraObject.AddComponent<AudioListener>();

        GameObject controllerObject =
            new GameObject(
                "LEVEL_2_TITLE_SCREEN");

        LevelTwoTitleScreen controller =
            controllerObject.AddComponent<LevelTwoTitleScreen>();

        controller.Configure(
            "SampleScene");

        EditorUtility.SetDirty(
            controller);

        EditorSceneManager.MarkSceneDirty(
            titleScene);

        EditorSceneManager.SaveScene(
            titleScene,
            TitleScenePath);
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(
                    TitleScenePath,
                    true),

                new EditorBuildSettingsScene(
                    GameplayScenePath,
                    true)
            };

        foreach (EditorBuildSettingsScene existing in
                 EditorBuildSettings.scenes)
        {
            if (string.Equals(
                    existing.path,
                    TitleScenePath,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    existing.path,
                    GameplayScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            scenes.Add(existing);
        }

        EditorBuildSettings.scenes =
            scenes.ToArray();
    }

    private static GameObject FindSceneObject(
        string exactName)
    {
        foreach (Transform transform in
                 UnityEngine.Object
                     .FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
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
}
