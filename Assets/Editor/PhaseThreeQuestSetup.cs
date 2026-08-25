using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class PhaseThreeQuestSetup
{
    private const string RootName = "PHASE_03_QUEST_SYSTEM";
    private const string ReportPath =
        "Assets/Generated/PhaseThreeBlacksmithUprightReport.txt";

    private const string NavMeshAssetPath =
        "Assets/Generated/Navigation/PhaseThreeNavMesh.asset";

    private const string DemonPrefabPath =
        "Assets/Lil Pupinduy/Character/Demon_Minion/Prefabs/Demon_Minion_2.prefab";

    private const string BlacksmithPrefabPath =
        "Assets/Lil Pupinduy/Character/NPC Blacksmith/Prefabs/NPC_Blacksmith V1.prefab";

    private const string BlacksmithAnimationFbxPath =
        "Assets/Lil Pupinduy/Character/NPC Blacksmith/AnimationClips/Blacksmith/Anim_Blacksmith.fbx";

    private const string BlacksmithStoryControllerPath =
        "Assets/Generated/Navigation/BlacksmithStory.controller";

    private const string ForgingBlacksmithPrefabPath =
        "Assets/Lil Pupinduy/Character/NPC Blacksmith/Prefabs/NPC_Blacksmith V1_Forging.prefab";

    private const string DeadTreePrefabPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_dead.prefab";

    private const string ChainPrefabPath =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chain_hang_A.prefab";

    private const string KeyPrefabPath =
        "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_09_Key_002.prefab";

    [MenuItem(
        "Tools/Dungeon Game/Phase 3/Build Quest and Clue System",
        priority = 400)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before rebuilding the story.",
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

        GameObject demonPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonPrefabPath);

        GameObject blacksmithPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                BlacksmithPrefabPath);

        GameObject forgingPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ForgingBlacksmithPrefabPath);

        if (demonPrefab == null ||
            blacksmithPrefab == null ||
            forgingPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Imported Story Assets Missing",
                "Demon found: " + (demonPrefab != null) +
                "\nBlacksmith found: " + (blacksmithPrefab != null) +
                "\nForging blacksmith found: " + (forgingPrefab != null),
                "OK");
            return;
        }

        List<string> report = new List<string>
        {
            "PHASE 3 BLACKSMITH UPRIGHT ANIMATION FIX REPORT",
            "Scene: " + SceneManager.GetActiveScene().path,
            "Built: " + DateTime.Now,
            ""
        };

        DeleteExistingStory();
        RemoveOldQuestComponents();

        LockGameToFirstPerson(player, report);

        GameObject mainGateArch =
            FindSceneObject("Main Gate Arch");

        GameObject leftGateDoor =
            FindSceneObject("Main Gate Door Left");

        GameObject rightGateDoor =
            FindSceneObject("Main Gate Door Right");

        GameObject emptyKeyBox =
            FindSceneObject("Empty Key Box");

        GameObject warningLetter =
            FindSceneObject("Warning Letter");

        GameObject castleEntrance =
            FindSceneObject("Castle Front Gate");

        GameObject forge =
            FindSceneObject("Forge");

        GameObject finalKeyPreview =
            FindSceneObject("Final Gate Key Preview");

        GameObject oldKeyMaker =
            FindSceneObject("Imprisoned Key Maker");

        Vector3 prisonPosition =
            oldKeyMaker != null
                ? oldKeyMaker.transform.position
                : castleEntrance != null
                    ? castleEntrance.transform.position + Vector3.back * 12f
                    : player.transform.position + Vector3.forward * 20f;

        Quaternion prisonRotation =
            oldKeyMaker != null
                ? oldKeyMaker.transform.rotation
                : Quaternion.identity;

        if (oldKeyMaker != null)
            Undo.DestroyObjectImmediate(oldKeyMaker);

        GameObject oldCastleGuardian =
            FindSceneObject("Castle Guardian Boss");

        if (oldCastleGuardian != null)
        {
            Undo.RecordObject(
                oldCastleGuardian,
                "Disable Old Castle Guardian");

            oldCastleGuardian.SetActive(false);
            report.Add(
                "Disabled old Castle Guardian Boss; demon now guards the rescue story.");
        }

        NavMeshData navMeshData =
            BuildNavigationAsset(
                player,
                report);

        NavMeshDataInstance editorNavMeshInstance =
            default;

        if (navMeshData != null)
        {
            editorNavMeshInstance =
                NavMesh.AddNavMeshData(
                    navMeshData);
        }

        GameObject root =
            new GameObject(RootName);

        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Demon Ambush Story");

        DungeonNavMeshDataLoader navMeshLoader =
            Undo.AddComponent<DungeonNavMeshDataLoader>(
                root);

        navMeshLoader.Configure(
            navMeshData);

        QuestHUD hud =
            Undo.AddComponent<QuestHUD>(root);

        QuestStoryReferences references =
            Undo.AddComponent<QuestStoryReferences>(root);

        QuestManager manager =
            Undo.AddComponent<QuestManager>(root);

        manager.Configure(hud, references);

        Camera firstPersonCamera =
            FindFirstPersonCamera(player);

        FirstPersonQuestInteractor interactor =
            player.GetComponent<FirstPersonQuestInteractor>();

        if (interactor == null)
        {
            interactor =
                Undo.AddComponent<FirstPersonQuestInteractor>(
                    player);
        }

        interactor.Configure(
            firstPersonCamera,
            hud);

        PlayerKnockdownSequence knockdown =
            player.GetComponent<PlayerKnockdownSequence>();

        if (knockdown == null)
        {
            knockdown =
                Undo.AddComponent<PlayerKnockdownSequence>(
                    player);
        }

        Behaviour firstPersonMovement =
            FindBehaviourByTypeName(
                player,
                "FirstPersonPlayer");

        Behaviour firstPersonSword =
            FindBehaviourByTypeName(
                player,
                "FirstPersonSword");

        FirstPersonStealthController stealthController =
            player.GetComponent<FirstPersonStealthController>();

        if (stealthController == null)
        {
            stealthController =
                Undo.AddComponent<FirstPersonStealthController>(
                    player);
        }

        stealthController.Configure(
            firstPersonMovement as MonoBehaviour);

        SwordDamageDealer damageDealer =
            player.GetComponent<SwordDamageDealer>();

        CombatHealth playerHealth =
            player.GetComponent<CombatHealth>();

        knockdown.Configure(
            firstPersonCamera,
            firstPersonMovement,
            firstPersonSword,
            damageDealer,
            playerHealth);

        Transform prisonPoint =
            CreatePoint(
                root.transform,
                "Story Point - Prison",
                prisonPosition,
                prisonRotation,
                true);

        Transform workshopPoint =
            CreateWorkshopPoint(
                root.transform,
                forge,
                prisonPosition);

        Vector3 castlePosition =
            castleEntrance != null
                ? castleEntrance.transform.position
                : prisonPosition + Vector3.forward * 25f;

        Vector3 workshopPosition =
            workshopPoint.position;

        Vector3 routeDirection =
            workshopPosition - castlePosition;

        routeDirection.y = 0f;

        if (routeDirection.sqrMagnitude < 0.001f)
            routeDirection = Vector3.forward;

        routeDirection.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                routeDirection).normalized;

        Vector3 ambushPosition =
            SampleNavMeshPosition(
                castlePosition +
                routeDirection * 26f +
                side * 7f,
                18f);

        Transform ambushPoint =
            CreatePoint(
                root.transform,
                "Story Point - Demon Ambush",
                ambushPosition,
                Quaternion.LookRotation(routeDirection),
                false);

        Vector3 treePosition =
            SampleNavMeshPosition(
                ambushPosition +
                side * 4.4f,
                16f);

        Quaternion treeRotation =
            Quaternion.LookRotation(
                -side,
                Vector3.up);

        GameObject deadTree =
            InstantiateOptionalPrefab(
                DeadTreePrefabPath,
                "Ambush Dead Tree",
                treePosition,
                treeRotation,
                root.transform);

        if (deadTree != null)
        {
            GroundRendererBottomToTerrain(
                deadTree);

            ConfigureTreeObstacle(
                deadTree);

            treePosition =
                deadTree.transform.position;
        }

        Vector3 captivePosition =
            SampleNavMeshPosition(
                treePosition +
                treeRotation *
                Vector3.forward *
                1.05f,
                8f);

        Transform captivePoint =
            CreatePoint(
                root.transform,
                "Story Point - Tree Captive",
                captivePosition,
                treeRotation,
                false);

        Transform demonGuardPoint =
            CreatePoint(
                root.transform,
                "Story Point - Demon Guard",
                SampleNavMeshPosition(
                    captivePosition -
                    treeRotation *
                    Vector3.forward *
                    5.2f +
                    side * 2.2f,
                    12f),
                Quaternion.LookRotation(
                    captivePosition -
                    ambushPosition,
                    Vector3.up),
                false);

        Transform demonSpawnPoint =
            CreatePoint(
                root.transform,
                "Story Point - Demon Spawn",
                SampleNavMeshPosition(
                    ambushPosition +
                    routeDirection * 10f -
                    side * 5f,
                    16f),
                Quaternion.LookRotation(-routeDirection),
                false);

        AnimatorController blacksmithStoryController =
            BuildBlacksmithStoryController(
                report);

        GameObject blacksmith =
            new GameObject(
                "Imprisoned Key Maker");

        Undo.RegisterCreatedObjectUndo(
            blacksmith,
            "Create Blacksmith Actor Root");

        blacksmith.transform.position =
            SampleNavMeshPosition(
                prisonPoint.position,
                12f);

        blacksmith.transform.rotation =
            YawOnly(
                prisonPoint.rotation);

        GameObject blacksmithVisual =
            InstantiatePrefab(
                blacksmithPrefab,
                "Blacksmith Visual",
                blacksmith.transform.position,
                blacksmith.transform.rotation);

        Undo.SetTransformParent(
            blacksmithVisual.transform,
            blacksmith.transform,
            "Parent Blacksmith Visual");

        blacksmithVisual.transform.localPosition =
            Vector3.zero;

        blacksmithVisual.transform.localRotation =
            Quaternion.identity;

        ScaleToHeight(
            blacksmithVisual,
            1.9f);

        AlignVisualFeetToRoot(
            blacksmithVisual,
            blacksmith.transform);

        DisablePrisonBlacksmithProps(
            blacksmithVisual);

        Animator blacksmithAnimator =
            blacksmithVisual.GetComponentInChildren<Animator>(true);

        if (blacksmithAnimator != null)
        {
            blacksmithAnimator.runtimeAnimatorController =
                blacksmithStoryController;

            blacksmithAnimator.applyRootMotion = false;
            blacksmithAnimator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;

            blacksmithAnimator.updateMode =
                AnimatorUpdateMode.Normal;

            EditorUtility.SetDirty(
                blacksmithAnimator);
        }

        NavMeshAgent blacksmithAgent =
            Undo.AddComponent<NavMeshAgent>(
                blacksmith);

        ConfigureNavMeshAgentFromBounds(
            blacksmith,
            blacksmithAgent,
            3.75f,
            0.7f,
            45);

        // Never tilt the actor to the slope normal.
        blacksmithAgent.updateUpAxis = false;

        CapsuleCollider blacksmithCollider =
            Undo.AddComponent<CapsuleCollider>(
                blacksmith);

        ConfigureCapsuleColliderFromBounds(
            blacksmith,
            blacksmithCollider,
            0.72f);

        BlacksmithStoryActor blacksmithActor =
            Undo.AddComponent<BlacksmithStoryActor>(
                blacksmith);

        blacksmithActor.Configure(
            blacksmithVisual.transform,
            blacksmithAnimator,
            blacksmithAgent,
            player.transform);

        report.Add(
            "BLACKSMITH FIX: Wrapper root created; generated Stand/Walk-only Animator Controller assigned.");

        GameObject forgingBlacksmith =
            InstantiatePrefab(
                forgingPrefab,
                "Workshop Blacksmith - Forging",
                workshopPoint.position,
                workshopPoint.rotation);

        ScaleToHeight(
            forgingBlacksmith,
            1.9f);

        forgingBlacksmith.transform.position =
            SampleNavMeshPosition(
                workshopPoint.position,
                10f);

        forgingBlacksmith.SetActive(false);

        GameObject captiveChains =
            CreateCaptiveChains(
                root.transform,
                captivePoint);

        GameObject demon =
            InstantiatePrefab(
                demonPrefab,
                "Demon Ambush Boss",
                demonSpawnPoint.position,
                demonSpawnPoint.rotation);

        ScaleToHeight(
            demon,
            3.45f);

        demon.transform.position =
            SampleNavMeshPosition(
                demonSpawnPoint.position,
                12f);

        Animator demonAnimator =
            demon.GetComponentInChildren<Animator>(true);

        if (demonAnimator != null)
        {
            demonAnimator.applyRootMotion = false;
            demonAnimator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;
        }

        CharacterController oldDemonController =
            demon.GetComponent<CharacterController>();

        if (oldDemonController != null)
            Undo.DestroyObjectImmediate(oldDemonController);

        NavMeshAgent demonAgent =
            demon.GetComponent<NavMeshAgent>();

        if (demonAgent == null)
        {
            demonAgent =
                Undo.AddComponent<NavMeshAgent>(
                    demon);
        }

        ConfigureNavMeshAgentFromBounds(
            demon,
            demonAgent,
            4.8f,
            2.25f,
            35);

        CapsuleCollider demonCollider =
            demon.GetComponent<CapsuleCollider>();

        if (demonCollider == null)
        {
            demonCollider =
                Undo.AddComponent<CapsuleCollider>(
                    demon);
        }

        ConfigureCapsuleColliderFromBounds(
            demon,
            demonCollider,
            0.78f);

        CombatHealth demonHealth =
            demon.GetComponent<CombatHealth>();

        if (demonHealth == null)
        {
            demonHealth =
                Undo.AddComponent<CombatHealth>(
                    demon);
        }

        demonHealth.Configure(
            550f,
            CombatTeam.Enemy,
            0.10f,
            false);

        if (demon.GetComponent<DamageFlash>() == null)
            Undo.AddComponent<DamageFlash>(demon);

        CombatWorldHealthBar demonBar =
            demon.GetComponent<CombatWorldHealthBar>();

        if (demonBar == null)
        {
            demonBar =
                Undo.AddComponent<CombatWorldHealthBar>(
                    demon);
        }

        demonBar.Configure(
            demonHealth,
            false);

        GameObject stealthPoint =
            new GameObject(
                "Demon Stealth Strike Point");

        Undo.RegisterCreatedObjectUndo(
            stealthPoint,
            "Create Demon Stealth Strike Point");

        Undo.SetTransformParent(
            stealthPoint.transform,
            demon.transform,
            "Parent Demon Stealth Strike Point");

        stealthPoint.transform.localPosition =
            new Vector3(
                0f,
                1.7f,
                -0.35f);

        QuestInteractable stealthInteraction =
            Undo.AddComponent<QuestInteractable>(
                stealthPoint);

        stealthInteraction.Configure(
            QuestAction.StealthStrike,
            string.Empty,
            4.5f,
            stealthPoint.transform);

        stealthPoint.SetActive(false);

        DemonBossAI demonAI =
            demon.GetComponent<DemonBossAI>();

        if (demonAI == null)
        {
            demonAI =
                Undo.AddComponent<DemonBossAI>(
                    demon);
        }

        demonAI.Configure(
            demonAnimator,
            demonAgent,
            demonHealth,
            player.transform,
            playerHealth,
            stealthController,
            stealthInteraction);

        demon.SetActive(false);

        Transform[] firstRoute =
            CreateFirstEscortRoute(
                root.transform,
                prisonPoint,
                castlePosition,
                ambushPoint,
                routeDirection,
                side);

        Transform[] secondRoute =
            CreateWorkshopRoute(
                root.transform,
                captivePoint,
                workshopPoint,
                routeDirection,
                side);

        QuestInteractable treeRescue =
            CreateInteractionPoint(
                root.transform,
                "Quest Point - Free Blacksmith From Tree",
                captivePoint.position + Vector3.up * 1.15f,
                QuestAction.FreeTreeCaptive,
                string.Empty,
                5.5f);

        treeRescue.gameObject.SetActive(false);

        DemonAmbushStorySequence sequence =
            Undo.AddComponent<DemonAmbushStorySequence>(
                root);

        sequence.Configure(
            manager,
            hud,
            player.transform,
            blacksmithActor,
            demonAI,
            knockdown,
            prisonPoint,
            ambushPoint,
            captivePoint,
            demonGuardPoint,
            workshopPoint,
            firstRoute,
            secondRoute,
            captiveChains,
            treeRescue,
            forgingBlacksmith);

        GatePassageController gatePassage =
            ConfigureGatePassage(
                root.transform,
                mainGateArch,
                leftGateDoor,
                rightGateDoor,
                report);

        KeyRewardPopup keyPopup =
            Undo.AddComponent<KeyRewardPopup>(
                root);

        GameObject keyPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                KeyPrefabPath);

        keyPopup.Configure(
            keyPrefab,
            firstPersonCamera);

        references.Configure(
            gatePassage,
            sequence,
            keyPopup,
            finalKeyPreview);

        if (finalKeyPreview != null)
            finalKeyPreview.SetActive(false);

        PositionGateGuardians(
            mainGateArch,
            player,
            report);

        CreateStoryInteractions(
            root.transform,
            player,
            mainGateArch,
            emptyKeyBox,
            warningLetter,
            castleEntrance,
            blacksmith,
            workshopPoint,
            report);

        CreateEscapeTrigger(
            root.transform,
            player,
            mainGateArch,
            report);

        QuestManager.ResetSavedProgress();

        if (editorNavMeshInstance.valid)
            editorNavMeshInstance.Remove();

        EnsureFolder("Assets/Generated");
        File.WriteAllLines(
            ReportPath,
            report);

        AssetDatabase.ImportAsset(
            ReportPath);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(references);
        EditorUtility.SetDirty(sequence);
        EditorUtility.SetDirty(blacksmithActor);
        EditorUtility.SetDirty(demonAI);
        EditorUtility.SetDirty(interactor);
        EditorUtility.SetDirty(knockdown);
        EditorUtility.SetDirty(stealthController);
        EditorUtility.SetDirty(navMeshLoader);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        EditorUtility.DisplayDialog(
            "Blacksmith Upright Animation Fix Built",
            "The blacksmith now uses a separate upright navigation root.\n\n" +
            "Only the standing Idle and Walk clips are available during escort scenes.\n" +
            "The long Wait/sleep animation can no longer activate.\n" +
            "Slope alignment can no longer rotate him onto his side.\n" +
            "The tied pose is forced upright in front of the tree.\n" +
            "Captive chains were resized from their real renderer bounds.\n\n" +
            "The quest save was reset.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 3/Reset Quest Progress",
        priority = 401)]
    public static void ResetQuestProgress()
    {
        QuestManager.ResetSavedProgress();

        EditorUtility.DisplayDialog(
            "Quest Progress Reset",
            "Play Mode will begin at the main gate.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 3/Delete Quest System",
        priority = 402)]
    public static void Delete()
    {
        DeleteExistingStory();
        RemoveOldQuestComponents();

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
    }

    private static void CreateStoryInteractions(
        Transform root,
        GameObject player,
        GameObject mainGate,
        GameObject emptyKeyBox,
        GameObject warningLetter,
        GameObject castleEntrance,
        GameObject blacksmith,
        Transform workshopPoint,
        List<string> report)
    {
        CreateTargetInteraction(
            root,
            "Quest Point - Main Gate",
            mainGate,
            player.transform.position,
            QuestAction.MainGate,
            7f,
            report);

        CreateTargetInteraction(
            root,
            "Quest Point - Empty Key Box",
            emptyKeyBox,
            null,
            QuestAction.EmptyKeyBox,
            5f,
            report);

        CreateTargetInteraction(
            root,
            "Quest Point - Warning Letter",
            warningLetter,
            null,
            QuestAction.WarningLetter,
            5f,
            report);

        CreateTargetInteraction(
            root,
            "Quest Point - Castle Entrance",
            castleEntrance,
            player.transform.position,
            QuestAction.CastleEntrance,
            7f,
            report);

        CreateTargetInteraction(
            root,
            "Quest Point - Prison Blacksmith",
            blacksmith,
            null,
            QuestAction.RescueKeyMaker,
            6f,
            report);

        CreateInteractionPoint(
            root,
            "Quest Point - Receive Gate Key",
            workshopPoint.position + Vector3.up * 1.1f,
            QuestAction.ForgeKey,
            string.Empty,
            5.5f);

        report.Add(
            "Created all story interaction points.");
    }

    private static Transform[] CreateFirstEscortRoute(
        Transform root,
        Transform prison,
        Vector3 castleEntrancePosition,
        Transform ambush,
        Vector3 direction,
        Vector3 side)
    {
        Transform group =
            CreateGroup(
                root,
                "ROUTE_01_PRISON_TO_AMBUSH");

        Vector3[] positions =
        {
            Vector3.Lerp(
                prison.position,
                castleEntrancePosition,
                0.45f),

            castleEntrancePosition +
            direction * 6f,

            Vector3.Lerp(
                castleEntrancePosition,
                ambush.position,
                0.58f) -
            side * 4f,

            ambush.position
        };

        return CreateRoutePoints(
            group,
            positions);
    }

    private static Transform[] CreateWorkshopRoute(
        Transform root,
        Transform captive,
        Transform workshop,
        Vector3 direction,
        Vector3 side)
    {
        Transform group =
            CreateGroup(
                root,
                "ROUTE_02_TREE_TO_WORKSHOP");

        Vector3 start = captive.position;
        Vector3 end = workshop.position;

        Vector3[] positions =
        {
            start + direction * 7f,
            Vector3.Lerp(start, end, 0.30f) + side * 12f,
            Vector3.Lerp(start, end, 0.55f) - side * 9f,
            Vector3.Lerp(start, end, 0.78f) + side * 5f,
            end
        };

        return CreateRoutePoints(
            group,
            positions);
    }

    private static Transform[] CreateRoutePoints(
        Transform parent,
        Vector3[] positions)
    {
        Transform[] points =
            new Transform[positions.Length];

        for (int index = 0;
             index < positions.Length;
             index++)
        {
            points[index] =
                CreatePoint(
                    parent,
                    "Waypoint " +
                    (index + 1).ToString("00"),
                    SampleNavMeshPosition(
                        positions[index],
                        18f),
                    Quaternion.identity,
                    false);
        }

        return points;
    }

    private static GameObject CreateCaptiveChains(
        Transform root,
        Transform captivePoint)
    {
        GameObject chainsRoot =
            new GameObject("Captive Tree Chains");

        Undo.RegisterCreatedObjectUndo(
            chainsRoot,
            "Create Captive Tree Chains");

        Undo.SetTransformParent(
            chainsRoot.transform,
            root,
            "Parent Captive Tree Chains");

        chainsRoot.transform.SetPositionAndRotation(
            captivePoint.position,
            captivePoint.rotation);

        GameObject chainPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                ChainPrefabPath);

        if (chainPrefab != null)
        {
            Vector3[] offsets =
            {
                new Vector3(-0.28f, 1.28f, 0.12f),
                new Vector3(0.28f, 1.28f, 0.12f),
                new Vector3(0f, 0.82f, 0.16f)
            };

            Vector3[] rotations =
            {
                new Vector3(88f, 0f, 18f),
                new Vector3(88f, 0f, -18f),
                new Vector3(88f, 90f, 0f)
            };

            for (int index = 0;
                 index < offsets.Length;
                 index++)
            {
                GameObject chain =
                    PrefabUtility.InstantiatePrefab(
                        chainPrefab,
                        SceneManager.GetActiveScene())
                    as GameObject;

                if (chain == null)
                    continue;

                Undo.RegisterCreatedObjectUndo(
                    chain,
                    "Create Captive Chain");

                Undo.SetTransformParent(
                    chain.transform,
                    chainsRoot.transform,
                    "Parent Captive Chain");

                chain.transform.localPosition =
                    offsets[index];

                chain.transform.localRotation =
                    Quaternion.Euler(
                        rotations[index]);

                chain.transform.localScale =
                    Vector3.one;

                ScaleToLargestRendererDimension(
                    chain,
                    index == 2
                        ? 0.95f
                        : 1.15f);
            }
        }

        chainsRoot.SetActive(false);
        return chainsRoot;
    }

    private static void DisablePrisonBlacksmithProps(
        GameObject blacksmith)
    {
        string[] blockedNames =
        {
            "anvil",
            "hammer",
            "forge",
            "whetstone",
            "bellows",
            "sword",
            "table",
            "trough"
        };

        foreach (Transform child in
                 blacksmith.GetComponentsInChildren<Transform>(true))
        {
            if (child == blacksmith.transform)
                continue;

            string lower =
                child.name.ToLowerInvariant();

            if (blockedNames.Any(name =>
                    lower == name ||
                    lower.StartsWith(name + "_")))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static GatePassageController ConfigureGatePassage(
        Transform parent,
        GameObject gateArch,
        GameObject leftDoor,
        GameObject rightDoor,
        List<string> report)
    {
        GameObject controllerObject =
            new GameObject(
                "Main Gate Passage Controller");

        Undo.RegisterCreatedObjectUndo(
            controllerObject,
            "Create Main Gate Passage Controller");

        Undo.SetTransformParent(
            controllerObject.transform,
            parent,
            "Parent Main Gate Passage Controller");

        GatePassageController controller =
            Undo.AddComponent<GatePassageController>(
                controllerObject);

        controller.Configure(
            leftDoor != null
                ? leftDoor.transform
                : null,
            rightDoor != null
                ? rightDoor.transform
                : null,
            2.4f,
            4.7f);

        if (gateArch != null)
            ReplaceGateArchColliders(gateArch);

        controller.SetClosedInstantly();

        report.Add(
            "Configured fully passable sliding main gate.");

        return controller;
    }

    private static void ReplaceGateArchColliders(
        GameObject gateArch)
    {
        foreach (Collider collider in
                 gateArch.GetComponentsInChildren<Collider>(true))
        {
            Undo.RecordObject(
                collider,
                "Disable Original Gate Collider");

            collider.enabled = false;
        }

        Transform existing =
            FindChildByExactName(
                gateArch.transform,
                "Quest Gate Frame Colliders");

        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        if (!TryGetLocalRendererBounds(
                gateArch,
                gateArch.transform,
                out Bounds bounds))
        {
            return;
        }

        GameObject frame =
            new GameObject(
                "Quest Gate Frame Colliders");

        Undo.RegisterCreatedObjectUndo(
            frame,
            "Create Gate Frame Colliders");

        Undo.SetTransformParent(
            frame.transform,
            gateArch.transform,
            "Parent Gate Frame Colliders");

        frame.transform.localPosition = Vector3.zero;
        frame.transform.localRotation = Quaternion.identity;
        frame.transform.localScale = Vector3.one;

        float openingWidth =
            bounds.size.x * 0.48f;

        float openingHeight =
            bounds.size.y * 0.76f;

        float sideWidth =
            Mathf.Max(
                0.35f,
                (bounds.size.x - openingWidth) * 0.5f);

        float topHeight =
            Mathf.Max(
                0.35f,
                bounds.size.y - openingHeight);

        float depth =
            Mathf.Max(
                0.5f,
                bounds.size.z);

        CreateBox(
            frame.transform,
            "Left Pillar Collider",
            new Vector3(
                bounds.min.x + sideWidth * 0.5f,
                bounds.center.y,
                bounds.center.z),
            new Vector3(
                sideWidth,
                bounds.size.y,
                depth));

        CreateBox(
            frame.transform,
            "Right Pillar Collider",
            new Vector3(
                bounds.max.x - sideWidth * 0.5f,
                bounds.center.y,
                bounds.center.z),
            new Vector3(
                sideWidth,
                bounds.size.y,
                depth));

        CreateBox(
            frame.transform,
            "Top Beam Collider",
            new Vector3(
                bounds.center.x,
                bounds.min.y +
                openingHeight +
                topHeight * 0.5f,
                bounds.center.z),
            new Vector3(
                openingWidth,
                topHeight,
                depth));
    }

    private static void CreateBox(
        Transform parent,
        string name,
        Vector3 center,
        Vector3 size)
    {
        GameObject gameObject =
            new GameObject(name);

        Undo.RegisterCreatedObjectUndo(
            gameObject,
            "Create " + name);

        Undo.SetTransformParent(
            gameObject.transform,
            parent,
            "Parent " + name);

        BoxCollider collider =
            Undo.AddComponent<BoxCollider>(
                gameObject);

        collider.center = center;
        collider.size = size;
    }

    private static void PositionGateGuardians(
        GameObject gateArch,
        GameObject player,
        List<string> report)
    {
        if (gateArch == null)
            return;

        GameObject left =
            FindSceneObject("Gate Guardian Left");

        GameObject right =
            FindSceneObject("Gate Guardian Right");

        Vector3 towardPlayer =
            player.transform.position -
            gateArch.transform.position;

        towardPlayer.y = 0f;

        if (towardPlayer.sqrMagnitude < 0.001f)
            towardPlayer = -gateArch.transform.forward;

        towardPlayer.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                towardPlayer).normalized;

        if (left != null)
        {
            MoveGuardian(
                left,
                gateArch.transform.position +
                towardPlayer * 13f -
                side * 11.5f,
                player.transform.position);
        }

        if (right != null)
        {
            MoveGuardian(
                right,
                gateArch.transform.position +
                towardPlayer * 13f +
                side * 11.5f,
                player.transform.position);
        }

        report.Add(
            "Kept both wyverns guarding the main escape gate.");
    }

    private static void MoveGuardian(
        GameObject guardian,
        Vector3 position,
        Vector3 lookTarget)
    {
        guardian.transform.position =
            GroundPosition(position);

        Vector3 direction =
            lookTarget -
            guardian.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            guardian.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
        }

        GroundRendererBottomToTerrain(
            guardian);
    }

    private static void CreateEscapeTrigger(
        Transform parent,
        GameObject player,
        GameObject mainGate,
        List<string> report)
    {
        if (mainGate == null)
            return;

        Vector3 direction =
            mainGate.transform.position -
            player.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = mainGate.transform.forward;

        direction.Normalize();

        GameObject trigger =
            new GameObject(
                "Final Escape Trigger");

        Undo.RegisterCreatedObjectUndo(
            trigger,
            "Create Final Escape Trigger");

        Undo.SetTransformParent(
            trigger.transform,
            parent,
            "Parent Final Escape Trigger");

        trigger.transform.position =
            mainGate.transform.position +
            direction * 15f +
            Vector3.up * 2f;

        SphereCollider sphere =
            Undo.AddComponent<SphereCollider>(
                trigger);

        sphere.isTrigger = true;
        sphere.radius = 5f;

        Undo.AddComponent<QuestEscapeTrigger>(
            trigger);

        report.Add(
            "Created escape trigger beyond the opened gate.");
    }

    private static QuestInteractable CreateTargetInteraction(
        Transform root,
        string name,
        GameObject target,
        Vector3? reference,
        QuestAction action,
        float range,
        List<string> report)
    {
        if (target == null)
        {
            report.Add(
                "Missing interaction target: " + name);

            return null;
        }

        Vector3 position =
            CalculateInteractionPosition(
                target,
                reference);

        return CreateInteractionPoint(
            root,
            name,
            position,
            action,
            string.Empty,
            range);
    }

    private static QuestInteractable CreateInteractionPoint(
        Transform root,
        string name,
        Vector3 position,
        QuestAction action,
        string prompt,
        float range)
    {
        GameObject point =
            new GameObject(name);

        Undo.RegisterCreatedObjectUndo(
            point,
            "Create " + name);

        Undo.SetTransformParent(
            point.transform,
            root,
            "Parent " + name);

        point.transform.position = position;

        QuestInteractable interactable =
            Undo.AddComponent<QuestInteractable>(
                point);

        interactable.Configure(
            action,
            prompt,
            range,
            point.transform);

        return interactable;
    }

    private static Vector3 CalculateInteractionPosition(
        GameObject target,
        Vector3? reference)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            return
                target.transform.position +
                Vector3.up;
        }

        Vector3 position =
            new Vector3(
                bounds.center.x,
                bounds.min.y + 1.2f,
                bounds.center.z);

        if (reference.HasValue)
        {
            Vector3 direction =
                reference.Value -
                bounds.center;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();

                position +=
                    direction *
                    (Mathf.Max(
                        bounds.extents.x,
                        bounds.extents.z) +
                     1.4f);
            }
        }

        return position;
    }

    private static Transform CreateWorkshopPoint(
        Transform root,
        GameObject forge,
        Vector3 fallback)
    {
        Vector3 position = fallback;
        Quaternion rotation = Quaternion.identity;

        if (forge != null)
        {
            Vector3 forward =
                Vector3.ProjectOnPlane(
                    forge.transform.forward,
                    Vector3.up).normalized;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            position =
                forge.transform.position +
                forward * 2.2f +
                forge.transform.right * 0.7f;

            rotation =
                Quaternion.LookRotation(
                    -forward,
                    Vector3.up);
        }

        return CreatePoint(
            root,
            "Story Point - Workshop",
            SampleNavMeshPosition(
                position,
                18f),
            rotation,
            false);
    }

    private static Transform CreatePoint(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        bool preserveHeight)
    {
        GameObject point =
            new GameObject(name);

        Undo.RegisterCreatedObjectUndo(
            point,
            "Create " + name);

        Undo.SetTransformParent(
            point.transform,
            parent,
            "Parent " + name);

        point.transform.position =
            preserveHeight
                ? position
                : SampleNavMeshPosition(
                    position,
                    18f);

        point.transform.rotation = rotation;
        return point.transform;
    }

    private static Transform CreateGroup(
        Transform parent,
        string name)
    {
        GameObject group =
            new GameObject(name);

        Undo.RegisterCreatedObjectUndo(
            group,
            "Create " + name);

        Undo.SetTransformParent(
            group.transform,
            parent,
            "Parent " + name);

        return group.transform;
    }

    private static GameObject InstantiatePrefab(
        GameObject prefab,
        string name,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                SceneManager.GetActiveScene())
            as GameObject;

        if (instance == null)
            throw new InvalidOperationException(
                "Could not instantiate " + prefab.name);

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Create " + name);

        instance.name = name;
        instance.transform.SetPositionAndRotation(
            position,
            rotation);

        return instance;
    }

    private static GameObject InstantiateOptionalPrefab(
        string path,
        string name,
        Vector3 position,
        Quaternion rotation,
        Transform parent)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                path);

        if (prefab == null)
            return null;

        GameObject instance =
            InstantiatePrefab(
                prefab,
                name,
                position,
                rotation);

        Undo.SetTransformParent(
            instance.transform,
            parent,
            "Parent " + name);

        return instance;
    }

    private static AnimatorController BuildBlacksmithStoryController(
        List<string> report)
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder("Assets/Generated/Navigation");

        AnimationClip idleClip =
            LoadAnimationClip(
                BlacksmithAnimationFbxPath,
                "Idle");

        AnimationClip walkClip =
            LoadAnimationClip(
                BlacksmithAnimationFbxPath,
                "Walk");

        if (idleClip == null ||
            walkClip == null)
        {
            report.Add(
                "BLACKSMITH ANIMATION ERROR: Idle or Walk clip was not found.");

            return
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    BlacksmithStoryControllerPath);
        }

        AssetDatabase.DeleteAsset(
            BlacksmithStoryControllerPath);

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(
                BlacksmithStoryControllerPath);

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;

        AnimatorState standState =
            stateMachine.AddState(
                "Stand",
                new Vector3(
                    230f,
                    100f,
                    0f));

        standState.motion = idleClip;
        standState.speed = 1f;
        standState.writeDefaultValues = true;

        AnimatorState walkState =
            stateMachine.AddState(
                "Walk",
                new Vector3(
                    500f,
                    100f,
                    0f));

        walkState.motion = walkClip;
        walkState.speed = 1f;
        walkState.writeDefaultValues = true;

        stateMachine.defaultState =
            standState;

        EditorUtility.SetDirty(
            standState);

        EditorUtility.SetDirty(
            walkState);

        EditorUtility.SetDirty(
            stateMachine);

        EditorUtility.SetDirty(
            controller);

        AssetDatabase.SaveAssets();

        report.Add(
            "BLACKSMITH ANIMATOR: Generated a controller with only upright Stand and Walk states.");

        report.Add(
            "BLACKSMITH ANIMATOR PATH: " +
            BlacksmithStoryControllerPath);

        return controller;
    }

    private static AnimationClip LoadAnimationClip(
        string assetPath,
        string exactClipName)
    {
        return
            AssetDatabase.LoadAllAssetsAtPath(
                assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(
                clip =>
                    !clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        clip.name,
                        exactClipName,
                        StringComparison.OrdinalIgnoreCase));
    }

    private static Quaternion YawOnly(
        Quaternion rotation)
    {
        Vector3 forward =
            rotation * Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            return Quaternion.identity;

        return
            Quaternion.LookRotation(
                forward.normalized,
                Vector3.up);
    }

    private static NavMeshData BuildNavigationAsset(
        GameObject player,
        List<string> report)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            report.Add(
                "NAVMESH ERROR: Active Terrain was not found.");
            return null;
        }

        EnsureFolder("Assets/Generated");
        EnsureFolder("Assets/Generated/Navigation");

        Vector3 terrainSize =
            terrain.terrainData.size;

        Bounds buildBounds =
            new Bounds(
                terrain.transform.position +
                terrainSize * 0.5f,
                terrainSize +
                new Vector3(
                    30f,
                    80f,
                    30f));

        List<NavMeshBuildMarkup> markups =
            new List<NavMeshBuildMarkup>();

        HashSet<Transform> ignoredRoots =
            new HashSet<Transform>();

        if (player != null)
            ignoredRoots.Add(player.transform);

        string[] ignoredNames =
        {
            "RandomNPCs_Generated",
            "PHASE_02_COMBAT",
            "ThirdPersonCharacterRoot",
            "ThirdPersonCameraRig",
            RootName
        };

        foreach (string ignoredName in ignoredNames)
        {
            GameObject ignoredObject =
                FindSceneObject(ignoredName);

            if (ignoredObject != null)
                ignoredRoots.Add(ignoredObject.transform);
        }

        Animator[] animators =
            UnityEngine.Object.FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Animator animator in animators)
        {
            if (animator == null ||
                !animator.gameObject.scene.IsValid())
            {
                continue;
            }

            Transform animatedRoot =
                animator.transform;

            if (animatedRoot != null)
                ignoredRoots.Add(animatedRoot);
        }

        foreach (Transform ignoredRoot in ignoredRoots)
        {
            if (ignoredRoot == null)
                continue;

            NavMeshBuildMarkup markup =
                new NavMeshBuildMarkup
                {
                    root = ignoredRoot,
                    overrideIgnore = true
                };

            markups.Add(markup);
        }

        List<NavMeshBuildSource> sources =
            new List<NavMeshBuildSource>();

        NavMeshBuilder.CollectSources(
            buildBounds,
            ~0,
            NavMeshCollectGeometry.RenderMeshes,
            0,
            markups,
            sources);

        if (sources.Count == 0)
        {
            report.Add(
                "NAVMESH ERROR: No render-mesh navigation sources were collected.");
            return null;
        }

        if (NavMesh.GetSettingsCount() == 0)
        {
            report.Add(
                "NAVMESH ERROR: No NavMesh agent settings exist in the project.");
            return null;
        }

        NavMeshBuildSettings settings =
            NavMesh.GetSettingsByIndex(0);

        settings.agentRadius = 0.43f;
        settings.agentHeight = 2.05f;
        settings.agentSlope = 55f;
        settings.agentClimb = 0.68f;

        NavMeshData data =
            NavMeshBuilder.BuildNavMeshData(
                settings,
                sources,
                buildBounds,
                Vector3.zero,
                Quaternion.identity);

        if (data == null)
        {
            report.Add(
                "NAVMESH ERROR: Unity failed to build navigation data.");
            return null;
        }

        AssetDatabase.DeleteAsset(
            NavMeshAssetPath);

        AssetDatabase.CreateAsset(
            data,
            NavMeshAssetPath);

        AssetDatabase.SaveAssets();

        report.Add(
            "NAVMESH: Baked " +
            sources.Count +
            " render-mesh sources with 55-degree slope support.");

        report.Add(
            "NAVMESH ASSET: " +
            NavMeshAssetPath);

        return data;
    }

    private static Vector3 SampleNavMeshPosition(
        Vector3 desired,
        float radius)
    {
        if (NavMesh.SamplePosition(
                desired,
                out NavMeshHit hit,
                Mathf.Max(0.5f, radius),
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return GroundPosition(desired);
    }

    private static void ConfigureTreeObstacle(
        GameObject tree)
    {
        if (tree == null)
            return;

        NavMeshObstacle obstacle =
            tree.GetComponent<NavMeshObstacle>();

        if (obstacle == null)
        {
            obstacle =
                Undo.AddComponent<NavMeshObstacle>(
                    tree);
        }

        obstacle.shape =
            NavMeshObstacleShape.Capsule;

        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.carvingTimeToStationary = 0.1f;
        obstacle.carvingMoveThreshold = 0.05f;

        if (TryGetRendererBounds(
                tree,
                out Bounds bounds))
        {
            Vector3 scale =
                tree.transform.lossyScale;

            obstacle.center =
                tree.transform.InverseTransformPoint(
                    bounds.center);

            obstacle.height =
                Mathf.Max(
                    1f,
                    SafeDivide(
                        bounds.size.y,
                        Mathf.Abs(scale.y)));

            obstacle.radius =
                Mathf.Clamp(
                    Mathf.Min(
                        SafeDivide(
                            bounds.extents.x,
                            Mathf.Abs(scale.x)),
                        SafeDivide(
                            bounds.extents.z,
                            Mathf.Abs(scale.z))) *
                    0.45f,
                    0.3f,
                    1.2f);
        }
    }

    private static void ConfigureNavMeshAgentFromBounds(
        GameObject target,
        NavMeshAgent agent,
        float speed,
        float stoppingDistance,
        int avoidancePriority)
    {
        if (agent == null)
            return;

        agent.speed = speed;
        agent.acceleration = 20f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance =
            Mathf.Max(0.05f, stoppingDistance);

        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        agent.avoidancePriority =
            Mathf.Clamp(
                avoidancePriority,
                0,
                99);

        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            agent.height = 2f;
            agent.radius = 0.4f;
            agent.baseOffset = 0f;
            return;
        }

        Vector3 scale =
            target.transform.lossyScale;

        float localHeight =
            SafeDivide(
                bounds.size.y,
                Mathf.Abs(scale.y));

        float localRadius =
            Mathf.Min(
                SafeDivide(
                    bounds.extents.x,
                    Mathf.Abs(scale.x)),
                SafeDivide(
                    bounds.extents.z,
                    Mathf.Abs(scale.z)));

        agent.height =
            Mathf.Max(
                1f,
                localHeight * 0.92f);

        agent.radius =
            Mathf.Clamp(
                localRadius * 0.72f,
                0.25f,
                1.35f);

        agent.baseOffset = 0f;
    }

    private static void ConfigureCapsuleColliderFromBounds(
        GameObject target,
        CapsuleCollider collider,
        float radiusMultiplier)
    {
        if (collider == null)
            return;

        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            collider.height = 2f;
            collider.radius = 0.4f;
            collider.center = Vector3.up;
            return;
        }

        Vector3 scale =
            target.transform.lossyScale;

        float localHeight =
            SafeDivide(
                bounds.size.y,
                Mathf.Abs(scale.y));

        float localRadius =
            Mathf.Min(
                SafeDivide(
                    bounds.extents.x,
                    Mathf.Abs(scale.x)),
                SafeDivide(
                    bounds.extents.z,
                    Mathf.Abs(scale.z)));

        collider.direction = 1;

        collider.height =
            Mathf.Max(
                0.8f,
                localHeight * 0.92f);

        collider.radius =
            Mathf.Clamp(
                localRadius * radiusMultiplier,
                0.22f,
                collider.height * 0.45f);

        collider.center =
            target.transform.InverseTransformPoint(
                bounds.center);
    }

    private static void ConfigureCharacterControllerFromBounds(
        GameObject target,
        CharacterController controller)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            controller.height = 3f;
            controller.radius = 0.8f;
            controller.center = Vector3.up * 1.5f;
            return;
        }

        Vector3 scale =
            target.transform.lossyScale;

        controller.height =
            SafeDivide(
                bounds.size.y,
                Mathf.Abs(scale.y));

        controller.radius =
            Mathf.Max(
                0.35f,
                Mathf.Min(
                    SafeDivide(
                        bounds.extents.x,
                        Mathf.Abs(scale.x)),
                    SafeDivide(
                        bounds.extents.z,
                        Mathf.Abs(scale.z))) *
                0.75f);

        controller.center =
            target.transform.InverseTransformPoint(
                bounds.center);

        controller.stepOffset =
            Mathf.Min(
                0.55f,
                controller.height * 0.18f);

        controller.slopeLimit = 48f;
    }

    private static void ScaleToLargestRendererDimension(
        GameObject target,
        float desiredSize)
    {
        if (target == null ||
            !TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            return;
        }

        float currentSize =
            Mathf.Max(
                bounds.size.x,
                bounds.size.y,
                bounds.size.z);

        if (currentSize <= 0.0001f)
            return;

        target.transform.localScale *=
            desiredSize /
            currentSize;
    }

    private static void AlignVisualFeetToRoot(
        GameObject visual,
        Transform actorRoot)
    {
        if (visual == null ||
            actorRoot == null ||
            !TryGetRendererBounds(
                visual,
                out Bounds bounds))
        {
            return;
        }

        float verticalOffset =
            actorRoot.position.y -
            bounds.min.y;

        visual.transform.position +=
            Vector3.up *
            verticalOffset;
    }

    private static void ScaleToHeight(
        GameObject target,
        float desiredHeight)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            return;
        }

        float currentHeight =
            Mathf.Max(
                0.01f,
                bounds.size.y);

        target.transform.localScale *=
            desiredHeight / currentHeight;
    }

    private static void GroundRendererBottomToTerrain(
        GameObject target)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            return;
        }

        float terrainY =
            SampleTerrainHeight(
                target.transform.position);

        target.transform.position +=
            Vector3.up *
            (terrainY - bounds.min.y);
    }

    private static Vector3 GroundPosition(
        Vector3 position)
    {
        position.y =
            SampleTerrainHeight(position);

        return position;
    }

    private static float SampleTerrainHeight(
        Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return position.y;
        }

        return
            terrain.SampleHeight(position) +
            terrain.transform.position.y;
    }

    private static bool TryGetRendererBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer != null &&
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

    private static bool TryGetLocalRendererBounds(
        GameObject root,
        Transform reference,
        out Bounds localBounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer != null &&
                    !(renderer is ParticleSystemRenderer))
                .ToArray();

        if (renderers.Length == 0)
        {
            localBounds = default;
            return false;
        }

        bool initialized = false;
        localBounds = default;

        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 local =
                    reference.InverseTransformPoint(corner);

                if (!initialized)
                {
                    localBounds =
                        new Bounds(
                            local,
                            Vector3.zero);

                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(local);
                }
            }
        }

        return initialized;
    }

    private static float SafeDivide(
        float value,
        float divisor)
    {
        return divisor > 0.0001f
            ? value / divisor
            : value;
    }

    private static Camera FindFirstPersonCamera(
        GameObject player)
    {
        Camera[] cameras =
            player.GetComponentsInChildren<Camera>(true);

        foreach (Camera camera in cameras)
        {
            if (!IsUnderNamedParent(
                    camera.transform,
                    "ThirdPersonCameraRig"))
            {
                return camera;
            }
        }

        return Camera.main;
    }

    private static void LockGameToFirstPerson(
        GameObject player,
        List<string> report)
    {
        foreach (MonoBehaviour behaviour in
                 player.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
                continue;

            string typeName =
                behaviour.GetType().Name;

            if (typeName ==
                "ThirdPersonCombatController")
            {
                behaviour.enabled = false;
            }
            else if (typeName ==
                     "FirstPersonPlayer" ||
                     typeName ==
                     "FirstPersonSword")
            {
                behaviour.enabled = true;
            }
        }

        GameObject thirdPersonCharacter =
            FindSceneObject(
                "ThirdPersonCharacterRoot");

        GameObject thirdPersonCamera =
            FindSceneObject(
                "ThirdPersonCameraRig");

        if (thirdPersonCharacter != null)
            thirdPersonCharacter.SetActive(false);

        if (thirdPersonCamera != null)
            thirdPersonCamera.SetActive(false);

        GameObject swordHolder =
            FindChildByNameContains(
                player.transform,
                "SwordHolder");

        if (swordHolder != null)
            swordHolder.SetActive(true);

        Camera camera =
            FindFirstPersonCamera(player);

        if (camera != null)
            camera.enabled = true;

        SwordDamageDealer damageDealer =
            player.GetComponent<SwordDamageDealer>();

        if (damageDealer != null)
        {
            damageDealer.Configure(
                camera,
                player.GetComponent<CombatHealth>());

            damageDealer.ClearMeleeOrigin(true);
            damageDealer.enabled = true;
        }

        report.Add(
            "Locked the game to first-person mode.");
    }

    private static Behaviour FindBehaviourByTypeName(
        GameObject root,
        string typeName)
    {
        foreach (MonoBehaviour behaviour in
                 root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour != null &&
                behaviour.GetType().Name == typeName)
            {
                return behaviour;
            }
        }

        return null;
    }

    private static bool IsUnderNamedParent(
        Transform transform,
        string name)
    {
        Transform current = transform;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
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

    private static Transform FindChildByExactName(
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

        foreach (MonoBehaviour behaviour in
                 UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
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

    private static GameObject FindSceneObject(
        string exactName)
    {
        foreach (Transform transform in
                 UnityEngine.Object.FindObjectsByType<Transform>(
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

    private static void DeleteExistingStory()
    {
        GameObject root =
            FindSceneObject(RootName);

        if (root != null)
            Undo.DestroyObjectImmediate(root);

        GameObject demon =
            FindSceneObject("Demon Ambush Boss");

        if (demon != null)
            Undo.DestroyObjectImmediate(demon);

        GameObject forging =
            FindSceneObject(
                "Workshop Blacksmith - Forging");

        if (forging != null)
            Undo.DestroyObjectImmediate(forging);
    }

    private static void RemoveOldQuestComponents()
    {
        RemoveComponents<FirstPersonQuestInteractor>();
        RemoveComponents<PlayerKnockdownSequence>();
        RemoveComponents<QuestBossWatcher>();
        RemoveComponents<QuestEscapeTrigger>();
        RemoveComponents<QuestInteractable>();
        RemoveComponents<BlacksmithStoryActor>();
        RemoveComponents<DemonAmbushStorySequence>();
        RemoveComponents<FirstPersonStealthController>();
    }

    private static void RemoveComponents<T>()
        where T : Component
    {
        foreach (T component in
                 UnityEngine.Object.FindObjectsByType<T>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (component != null &&
                component.gameObject.scene.IsValid())
            {
                Undo.DestroyObjectImmediate(component);
            }
        }
    }

    private static void EnsureFolder(
        string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash =
            path.LastIndexOf('/');

        string parent =
            path.Substring(0, slash);

        string folder =
            path.Substring(slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            folder);
    }
}
