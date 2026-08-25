using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdPersonKnightSetup
{
    private const string KnightPrefabPath =
        "Assets/BackRock Studios/LowPoly-Knight/Prefab/Knight_Editable.prefab";

    private const string SwordPrefabPath =
        "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_11_Sword_One-Handed_003.prefab";

    private const string GeneratedFolder =
        "Assets/Generated/ThirdPerson";

    private const string ControllerPath =
        GeneratedFolder + "/KnightCombat.controller";

    private const string CharacterRootName =
        "ThirdPersonCharacterRoot";

    private const string CameraRootName =
        "ThirdPersonCameraRig";

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Build Knight Combat Starter",
        priority = 310)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building the third-person character.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GameObject player = FindPlayer();

        if (player == null)
        {
            EditorUtility.DisplayDialog(
                "Player Not Found",
                "The player GameObject could not be found.",
                "OK");
            return;
        }

        GameObject knightPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                KnightPrefabPath);

        if (knightPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Knight Prefab Missing",
                KnightPrefabPath,
                "OK");
            return;
        }

        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedFolder);

        AnimationClip idle =
            FindAnimationClip(
                "Assets/BackRock Studios/LowPoly-Knight/Animations",
                "Idle");

        AnimationClip walk =
            FindAnimationClip(
                "Assets/BackRock Studios/LowPoly-Knight/Animations",
                "Walk");

        AnimationClip run =
            FindAnimationClip(
                "Assets/BackRock Studios/LowPoly-Knight/Animations",
                "Run");

        AnimationClip battle =
            FindAnimationClip(
                "Assets/EEJANAI_Team/FreeSwordAnimations/Animations",
                "battle stance");

        AnimationClip block =
            FindAnimationClip(
                "Assets/EEJANAI_Team/FreeSwordAnimations/Animations",
                "deffensive stance");

        AnimationClip slash1 =
            FindAnimationClip(
                "Assets/EEJANAI_Team/FreeSwordAnimations/Animations",
                "slash1");

        AnimationClip slash2 =
            FindAnimationClip(
                "Assets/EEJANAI_Team/FreeSwordAnimations/Animations",
                "slash2");

        AnimationClip heavy =
            FindAnimationClip(
                "Assets/EEJANAI_Team/FreeSwordAnimations/Animations",
                "slash7");

        List<string> missing =
            new List<string>();

        AddMissing(missing, "Idle", idle);
        AddMissing(missing, "Walk", walk);
        AddMissing(missing, "Run", run);
        AddMissing(missing, "battle stance", battle);
        AddMissing(missing, "deffensive stance", block);
        AddMissing(missing, "slash1", slash1);
        AddMissing(missing, "slash2", slash2);
        AddMissing(missing, "slash7", heavy);

        if (missing.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Animation Clips Missing",
                string.Join("\n", missing),
                "OK");
            return;
        }

        ConfigureLooping(idle, true);
        ConfigureLooping(walk, true);
        ConfigureLooping(run, true);
        ConfigureLooping(battle, true);
        ConfigureLooping(block, true);
        ConfigureLooping(slash1, false);
        ConfigureLooping(slash2, false);
        ConfigureLooping(heavy, false);

        AnimatorController controller =
            BuildAnimatorController(
                idle,
                walk,
                run,
                battle,
                block,
                slash1,
                slash2,
                heavy);

        DeleteOldGeneratedChildren(player.transform);

        GameObject characterRoot =
            new GameObject(CharacterRootName);

        Undo.RegisterCreatedObjectUndo(
            characterRoot,
            "Create Third Person Character Root");

        Undo.SetTransformParent(
            characterRoot.transform,
            player.transform,
            "Parent Third Person Character");

        characterRoot.transform.localPosition =
            Vector3.zero;

        characterRoot.transform.localRotation =
            Quaternion.identity;

        GameObject knight =
            PrefabUtility.InstantiatePrefab(
                knightPrefab,
                SceneManager.GetActiveScene())
            as GameObject;

        if (knight == null)
        {
            EditorUtility.DisplayDialog(
                "Knight Creation Failed",
                "The knight prefab could not be instantiated.",
                "OK");
            return;
        }

        Undo.RegisterCreatedObjectUndo(
            knight,
            "Create Knight Character");

        Undo.SetTransformParent(
            knight.transform,
            characterRoot.transform,
            "Parent Knight Character");

        knight.name = "Player Knight";
        knight.transform.localPosition = Vector3.zero;
        knight.transform.localRotation = Quaternion.identity;

        ScaleCharacterToHeight(
            knight,
            1.82f);

        GroundVisualToPlayerRoot(
            knight,
            player.transform.position.y);

        Animator animator =
            knight.GetComponentInChildren<Animator>(true);

        if (animator == null ||
            animator.avatar == null ||
            !animator.avatar.isValid ||
            !animator.avatar.isHuman)
        {
            EditorUtility.DisplayDialog(
                "Knight Avatar Invalid",
                "Knight_Editable did not provide a valid Humanoid Animator.",
                "OK");
            return;
        }

        animator.runtimeAnimatorController =
            controller;

        animator.applyRootMotion = false;
        animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;

        ThirdPersonKnightMaterialFixer.ConvertCharacterMaterialsToUrp(
            characterRoot);

        AttachSword(animator);

        GameObject cameraRoot =
            new GameObject(CameraRootName);

        Undo.RegisterCreatedObjectUndo(
            cameraRoot,
            "Create Third Person Camera Rig");

        Undo.SetTransformParent(
            cameraRoot.transform,
            player.transform,
            "Parent Third Person Camera Rig");

        cameraRoot.transform.localPosition =
            Vector3.zero;

        cameraRoot.transform.localRotation =
            Quaternion.identity;

        GameObject cameraObject =
            new GameObject("ThirdPersonCombatCamera");

        Undo.RegisterCreatedObjectUndo(
            cameraObject,
            "Create Third Person Camera");

        Undo.SetTransformParent(
            cameraObject.transform,
            cameraRoot.transform,
            "Parent Third Person Camera");

        Camera thirdPersonCamera =
            Undo.AddComponent<Camera>(
                cameraObject);

        Camera firstPersonCamera =
            FindFirstPersonCamera(
                player,
                thirdPersonCamera);

        if (firstPersonCamera != null)
        {
            thirdPersonCamera.CopyFrom(
                firstPersonCamera);

            thirdPersonCamera.depth =
                firstPersonCamera.depth + 1f;
        }
        else
        {
            thirdPersonCamera.fieldOfView = 65f;
            thirdPersonCamera.nearClipPlane = 0.05f;
            thirdPersonCamera.farClipPlane = 1000f;
        }

        thirdPersonCamera.enabled = false;

        Behaviour firstPersonMovement =
            FindBehaviourByTypeName(
                player,
                "FirstPersonPlayer");

        Behaviour firstPersonSword =
            FindBehaviourByTypeName(
                player,
                "FirstPersonSword");

        GameObject firstPersonVisualRoot =
            FindChildByNameContains(
                player.transform,
                "SwordHolder");

        CharacterController characterController =
            player.GetComponent<CharacterController>();

        CombatHealth health =
            player.GetComponent<CombatHealth>();

        SwordDamageDealer damageDealer =
            player.GetComponent<SwordDamageDealer>();

        Transform attackOrigin =
            CreateThirdPersonAttackOrigin(
                player.transform);

        ThirdPersonCombatController runtime =
            player.GetComponent<ThirdPersonCombatController>();

        if (runtime == null)
        {
            runtime =
                Undo.AddComponent<ThirdPersonCombatController>(
                    player);
        }

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

        HidePlayerPrimitiveRenderers(
            player,
            characterRoot.transform,
            firstPersonVisualRoot != null
                ? firstPersonVisualRoot.transform
                : null);

        characterRoot.SetActive(false);

        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(thirdPersonCamera);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            characterRoot;

        EditorGUIUtility.PingObject(
            characterRoot);

        EditorUtility.DisplayDialog(
            "Third-Person Knight Ready",
            "Press Play.\n\n" +
            "T switches FPV / TPV.\n" +
            "WASD moves, Shift sprints, Space jumps.\n" +
            "LMB attacks, E heavy attacks, RMB blocks.\n\n" +
            "The setup uses the Humanoid Knight_Editable prefab and the " +
            "standalone human-motion sword clips, not the Generic FBX files.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Delete Knight Combat Starter",
        priority = 311)]
    public static void Delete()
    {
        GameObject player = FindPlayer();

        if (player == null)
            return;

        DeleteOldGeneratedChildren(player.transform);

        ThirdPersonCombatController runtime =
            player.GetComponent<ThirdPersonCombatController>();

        if (runtime != null)
            Undo.DestroyObjectImmediate(runtime);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
    }

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Select Third Person Sword",
        priority = 312)]
    public static void SelectSword()
    {
        GameObject player = FindPlayer();

        if (player == null)
            return;

        Transform socket =
            FindTransformByExactName(
                player.transform,
                "ThirdPersonWeaponSocket");

        if (socket == null ||
            socket.childCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Sword Not Found",
                "Build the Knight Combat Starter first.",
                "OK");
            return;
        }

        Selection.activeGameObject =
            socket.GetChild(0).gameObject;

        EditorGUIUtility.PingObject(
            Selection.activeGameObject);
    }

    private static AnimatorController BuildAnimatorController(
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip battle,
        AnimationClip block,
        AnimationClip slash1,
        AnimationClip slash2,
        AnimationClip heavy)
    {
        AssetDatabase.DeleteAsset(
            ControllerPath);

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);

        controller.AddParameter(
            "Speed",
            AnimatorControllerParameterType.Float);

        controller.AddParameter(
            "Combat",
            AnimatorControllerParameterType.Bool);

        controller.AddParameter(
            "Block",
            AnimatorControllerParameterType.Bool);

        controller.AddParameter(
            "Attack1",
            AnimatorControllerParameterType.Trigger);

        controller.AddParameter(
            "Attack2",
            AnimatorControllerParameterType.Trigger);

        controller.AddParameter(
            "Heavy",
            AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine =
            controller.layers[0].stateMachine;

        AnimatorState locomotion =
            machine.AddState(
                "Locomotion",
                new Vector3(240f, 30f));

        BlendTree blendTree =
            new BlendTree
            {
                name = "Locomotion Blend Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

        AssetDatabase.AddObjectToAsset(
            blendTree,
            controller);

        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, 0.5f);
        blendTree.AddChild(run, 1f);

        locomotion.motion = blendTree;
        machine.defaultState = locomotion;

        AnimatorState combatIdle =
            AddState(
                machine,
                "Combat Idle",
                battle,
                new Vector3(500f, 30f));

        AnimatorState blockState =
            AddState(
                machine,
                "Block",
                block,
                new Vector3(500f, 140f));

        AnimatorState attack1 =
            AddState(
                machine,
                "Attack 1",
                slash1,
                new Vector3(760f, -40f));

        AnimatorState attack2 =
            AddState(
                machine,
                "Attack 2",
                slash2,
                new Vector3(760f, 50f));

        AnimatorState heavyState =
            AddState(
                machine,
                "Heavy Attack",
                heavy,
                new Vector3(760f, 140f));

        AddConditionTransition(
            locomotion,
            combatIdle,
            AnimatorConditionMode.If,
            0f,
            "Combat",
            false,
            0.12f);

        AddConditionTransition(
            combatIdle,
            locomotion,
            AnimatorConditionMode.IfNot,
            0f,
            "Combat",
            false,
            0.12f);

        AnimatorStateTransition blockTransition =
            machine.AddAnyStateTransition(
                blockState);

        blockTransition.hasExitTime = false;
        blockTransition.duration = 0.08f;
        blockTransition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            "Block");

        AddConditionTransition(
            blockState,
            combatIdle,
            AnimatorConditionMode.IfNot,
            0f,
            "Block",
            false,
            0.08f);

        AddTriggerTransition(
            machine,
            attack1,
            "Attack1");

        AddTriggerTransition(
            machine,
            attack2,
            "Attack2");

        AddTriggerTransition(
            machine,
            heavyState,
            "Heavy");

        AddExitTransition(
            attack1,
            combatIdle,
            0.88f,
            0.08f);

        AddExitTransition(
            attack2,
            combatIdle,
            0.90f,
            0.08f);

        AddExitTransition(
            heavyState,
            combatIdle,
            0.92f,
            0.10f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine machine,
        string name,
        Motion motion,
        Vector3 position)
    {
        AnimatorState state =
            machine.AddState(
                name,
                position);

        state.motion = motion;
        state.writeDefaultValues = true;

        return state;
    }

    private static void AddConditionTransition(
        AnimatorState from,
        AnimatorState to,
        AnimatorConditionMode mode,
        float threshold,
        string parameter,
        bool exitTime,
        float duration)
    {
        AnimatorStateTransition transition =
            from.AddTransition(to);

        transition.hasExitTime = exitTime;
        transition.duration = duration;
        transition.AddCondition(
            mode,
            threshold,
            parameter);
    }

    private static void AddTriggerTransition(
        AnimatorStateMachine machine,
        AnimatorState target,
        string parameter)
    {
        AnimatorStateTransition transition =
            machine.AddAnyStateTransition(
                target);

        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            parameter);
    }

    private static void AddExitTransition(
        AnimatorState from,
        AnimatorState to,
        float exitTime,
        float duration)
    {
        AnimatorStateTransition transition =
            from.AddTransition(to);

        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static AnimationClip FindAnimationClip(
        string folder,
        string exactName)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "t:AnimationClip",
                new[] { folder });

        List<AnimationClip> matches =
            new List<AnimationClip>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            UnityEngine.Object[] assets =
                AssetDatabase.LoadAllAssetsAtPath(
                    path);

            foreach (AnimationClip clip in
                     assets.OfType<AnimationClip>())
            {
                if (clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(
                        clip.name,
                        exactName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(clip);
                }
            }
        }

        return matches
            .OrderByDescending(clip =>
                AssetDatabase.GetAssetPath(clip)
                    .EndsWith(
                        ".anim",
                        StringComparison.OrdinalIgnoreCase))
            .ThenBy(clip =>
                AssetDatabase.GetAssetPath(clip)
                    .Contains("/FBX/"))
            .FirstOrDefault();
    }

    private static void ConfigureLooping(
        AnimationClip clip,
        bool loop)
    {
        if (clip == null)
            return;

        AnimationClipSettings settings =
            AnimationUtility.GetAnimationClipSettings(
                clip);

        settings.loopTime = loop;
        settings.loopBlend = loop;

        AnimationUtility.SetAnimationClipSettings(
            clip,
            settings);

        EditorUtility.SetDirty(clip);
    }

    private static void AttachSword(
        Animator animator)
    {
        GameObject swordPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SwordPrefabPath);

        if (swordPrefab == null)
            return;

        Transform rightHand =
            animator.GetBoneTransform(
                HumanBodyBones.RightHand);

        if (rightHand == null)
            return;

        GameObject socket =
            new GameObject(
                "ThirdPersonWeaponSocket");

        Undo.RegisterCreatedObjectUndo(
            socket,
            "Create Third Person Weapon Socket");

        Undo.SetTransformParent(
            socket.transform,
            rightHand,
            "Parent Third Person Weapon Socket");

        socket.transform.localPosition =
            new Vector3(
                0.02f,
                0.01f,
                0f);

        socket.transform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                90f);

        GameObject sword =
            PrefabUtility.InstantiatePrefab(
                swordPrefab,
                SceneManager.GetActiveScene())
            as GameObject;

        if (sword == null)
            return;

        Undo.RegisterCreatedObjectUndo(
            sword,
            "Create Third Person Sword");

        Undo.SetTransformParent(
            sword.transform,
            socket.transform,
            "Parent Third Person Sword");

        sword.name = "Third Person Sword";
        sword.transform.localPosition = Vector3.zero;
        sword.transform.localRotation = Quaternion.identity;
        sword.transform.localScale = Vector3.one;
    }

    private static void ScaleCharacterToHeight(
        GameObject character,
        float targetHeight)
    {
        if (!TryGetRendererBounds(
                character,
                out Bounds bounds))
        {
            return;
        }

        float height =
            Mathf.Max(
                0.01f,
                bounds.size.y);

        float scale =
            targetHeight / height;

        character.transform.localScale *= scale;
    }

    private static void GroundVisualToPlayerRoot(
        GameObject visual,
        float rootWorldY)
    {
        if (!TryGetRendererBounds(
                visual,
                out Bounds bounds))
        {
            return;
        }

        visual.transform.position +=
            Vector3.up *
            (rootWorldY - bounds.min.y);
    }

    private static bool TryGetRendererBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer))
                .ToArray();

        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;

        for (int index = 1;
             index < renderers.Length;
             index++)
        {
            bounds.Encapsulate(
                renderers[index].bounds);
        }

        return true;
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
                behaviour.GetType().Name ==
                "FirstPersonPlayer")
            {
                return behaviour.gameObject;
            }
        }

        return null;
    }

    private static void DeleteOldGeneratedChildren(
        Transform player)
    {
        Transform character =
            FindTransformByExactName(
                player,
                CharacterRootName);

        if (character != null)
            Undo.DestroyObjectImmediate(
                character.gameObject);

        Transform camera =
            FindTransformByExactName(
                player,
                CameraRootName);

        if (camera != null)
            Undo.DestroyObjectImmediate(
                camera.gameObject);
    }

    private static Transform CreateThirdPersonAttackOrigin(
        Transform player)
    {
        Transform existing =
            FindTransformByExactName(
                player,
                "ThirdPersonAttackOrigin");

        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject origin =
            new GameObject(
                "ThirdPersonAttackOrigin");

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

            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
            hidden++;
        }

        return hidden;
    }

    private static void AddMissing(
        List<string> missing,
        string label,
        UnityEngine.Object asset)
    {
        if (asset == null)
            missing.Add("- " + label);
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash =
            path.LastIndexOf('/');

        string parent =
            path.Substring(
                0,
                slash);

        string folder =
            path.Substring(
                slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            folder);
    }
}
