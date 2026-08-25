using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdPersonCoreFixesV2
{
    private const string CharacterRootName =
        "ThirdPersonCharacterRoot";

    private const string CameraRootName =
        "ThirdPersonCameraRig";

    private const string AttackOriginName =
        "ThirdPersonAttackOrigin";

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Apply Core Fixes V2",
        priority = 308)]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before applying the fixes.",
                "OK");
            return;
        }

        GameObject player = FindPlayer();

        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Player Not Found",
                "The player GameObject could not be found.",
                "OK");
            return;
        }

        GameObject characterRoot =
            FindSceneObjectIncludingInactive(
                CharacterRootName);

        GameObject cameraRoot =
            FindSceneObjectIncludingInactive(
                CameraRootName);

        if (characterRoot == null ||
            cameraRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Third-Person Setup Missing",
                "Build the Knight Combat Starter first.",
                "OK");
            return;
        }

        Animator animator =
            characterRoot.GetComponentInChildren<Animator>(true);

        Camera thirdPersonCamera =
            cameraRoot.GetComponentInChildren<Camera>(true);

        Camera firstPersonCamera =
            FindFirstPersonCamera(
                player,
                thirdPersonCamera);

        GameObject firstPersonVisualRoot =
            FindChildByNameContains(
                player.transform,
                "SwordHolder");

        Behaviour firstPersonMovement =
            FindBehaviourByTypeName(
                player,
                "FirstPersonPlayer");

        Behaviour firstPersonSword =
            FindBehaviourByTypeName(
                player,
                "FirstPersonSword");

        CharacterController characterController =
            player.GetComponent<CharacterController>();

        CombatHealth health =
            player.GetComponent<CombatHealth>();

        SwordDamageDealer damageDealer =
            player.GetComponent<SwordDamageDealer>();

        ThirdPersonCombatController runtime =
            player.GetComponent<ThirdPersonCombatController>();

        if (runtime == null ||
            damageDealer == null)
        {
            EditorUtility.DisplayDialog(
                "Combat Components Missing",
                "ThirdPersonCombatController or SwordDamageDealer was not found.",
                "OK");
            return;
        }

        Transform attackOrigin =
            CreateOrFindAttackOrigin(
                player.transform);

        runtime.Configure(
            characterRoot,
            animator,
            firstPersonCamera,
            thirdPersonCamera,
            firstPersonVisualRoot,
            firstPersonMovement,
            firstPersonSword,
            characterController,
            health,
            damageDealer,
            attackOrigin);

        int hiddenRenderers =
            HidePlayerPrimitiveRenderers(
                player,
                characterRoot.transform,
                firstPersonVisualRoot != null
                    ? firstPersonVisualRoot.transform
                    : null);

        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(damageDealer);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            attackOrigin.gameObject;

        EditorGUIUtility.PingObject(
            attackOrigin.gameObject);

        EditorUtility.DisplayDialog(
            "Third-Person Core Fixes Applied",
            "Fixed:\n" +
            "- Spacebar jump in TPV\n" +
            "- Third-person melee damage origin\n" +
            "- Direct damage calls for light/heavy attacks\n" +
            "- Character faces camera aim before attacking\n" +
            "- Hidden visible player capsule renderers: " +
            hiddenRenderers +
            "\n\nTest the nearby training targets.",
            "OK");
    }

    private static Transform CreateOrFindAttackOrigin(
        Transform player)
    {
        Transform existing =
            FindTransformByExactName(
                player,
                AttackOriginName);

        if (existing != null)
        {
            existing.localPosition =
                new Vector3(
                    0f,
                    1.05f,
                    0.65f);

            existing.localRotation =
                Quaternion.identity;

            return existing;
        }

        GameObject origin =
            new GameObject(
                AttackOriginName);

        Undo.RegisterCreatedObjectUndo(
            origin,
            "Create Third Person Attack Origin");

        Undo.SetTransformParent(
            origin.transform,
            player,
            "Parent Third Person Attack Origin");

        origin.transform.localPosition =
            new Vector3(
                0f,
                1.05f,
                0.65f);

        origin.transform.localRotation =
            Quaternion.identity;

        origin.transform.localScale =
            Vector3.one;

        return origin.transform;
    }

    private static int HidePlayerPrimitiveRenderers(
        GameObject player,
        Transform characterRoot,
        Transform firstPersonVisualRoot)
    {
        int hidden = 0;

        Renderer[] renderers =
            player.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Transform transform =
                renderer.transform;

            if (characterRoot != null &&
                (transform == characterRoot ||
                 transform.IsChildOf(characterRoot)))
            {
                continue;
            }

            if (firstPersonVisualRoot != null &&
                (transform == firstPersonVisualRoot ||
                 transform.IsChildOf(firstPersonVisualRoot)))
            {
                continue;
            }

            string lower =
                renderer.gameObject.name.ToLowerInvariant();

            bool looksLikePlayerPrimitive =
                renderer.gameObject == player ||
                lower.Contains("capsule") ||
                lower == "player body" ||
                lower == "body";

            if (!looksLikePlayerPrimitive)
                continue;

            Undo.RecordObject(
                renderer,
                "Hide Player Primitive Renderer");

            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
            hidden++;
        }

        return hidden;
    }

    private static GameObject FindPlayer()
    {
        GameObject player =
            GameObject.Find("player") ??
            GameObject.Find("Player");

        if (player != null)
            return player;

        MonoBehaviour[] behaviours =
            UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null &&
                behaviour.gameObject.scene.IsValid() &&
                behaviour.GetType().Name ==
                "FirstPersonPlayer")
            {
                return behaviour.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectIncludingInactive(
        string exactName)
    {
        Transform[] transforms =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Transform transform in transforms)
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

    private static Camera FindFirstPersonCamera(
        GameObject player,
        Camera excluded)
    {
        Camera[] cameras =
            player.GetComponentsInChildren<Camera>(true);

        foreach (Camera camera in cameras)
        {
            if (camera != excluded)
                return camera;
        }

        return Camera.main != excluded
            ? Camera.main
            : null;
    }

    private static Behaviour FindBehaviourByTypeName(
        GameObject root,
        string typeName)
    {
        MonoBehaviour[] behaviours =
            root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null &&
                behaviour.GetType().Name == typeName)
            {
                return behaviour;
            }
        }

        return null;
    }

    private static GameObject FindChildByNameContains(
        Transform root,
        string text)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.IndexOf(
                    text,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Transform FindTransformByExactName(
        Transform root,
        string exactName)
    {
        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(
                    child.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }
}
