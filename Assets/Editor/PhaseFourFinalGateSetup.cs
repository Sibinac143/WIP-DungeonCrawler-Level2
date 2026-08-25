using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class PhaseFourFinalGateSetup
{
    private const string RootName =
        "PHASE_04_FINAL_GATE";

    private const string ReportPath =
        "Assets/Generated/PhaseFourFinalGateReport.txt";

    private const string GeneratedMaterialFolder =
        "Assets/Generated/PhaseFourMaterials";

    private const string DefaultDemonPrefabPath =
        "Assets/Demon Horror Creature with Weapon/Prefabs/Demon_default.prefab";

    private const string DamagedDemonPrefabPath =
        "Assets/Demon Horror Creature with Weapon/Prefabs/Demon_damaged.prefab";

    private const string MagicSwordPrefabPath =
        "Assets/Hovl Studio/Magic sword/Prefabs/MagicSword_Iron.prefab";

    [MenuItem(
        "Tools/Dungeon Game/Phase 4/Build Final Gate Battle and Guidance",
        priority = 510)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building Phase 4.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        GameObject phaseThreeRoot =
            FindSceneObject(
                "PHASE_03_QUEST_SYSTEM");

        QuestManager questManager =
            phaseThreeRoot != null
                ? phaseThreeRoot.GetComponent<QuestManager>()
                : null;

        QuestStoryReferences storyReferences =
            phaseThreeRoot != null
                ? phaseThreeRoot.GetComponent<QuestStoryReferences>()
                : null;

        QuestHUD hud =
            phaseThreeRoot != null
                ? phaseThreeRoot.GetComponent<QuestHUD>()
                : null;

        GameObject mainGate =
            FindSceneObject(
                "Main Gate Arch");

        GameObject leftDoor =
            FindSceneObject(
                "Main Gate Door Left");

        GameObject rightDoor =
            FindSceneObject(
                "Main Gate Door Right");

        GameObject demonDefaultPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                DefaultDemonPrefabPath);

        GameObject demonDamagedPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                DamagedDemonPrefabPath);

        GameObject magicSwordPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                MagicSwordPrefabPath);

        if (player == null ||
            phaseThreeRoot == null ||
            questManager == null ||
            storyReferences == null ||
            hud == null ||
            mainGate == null ||
            demonDefaultPrefab == null ||
            demonDamagedPrefab == null ||
            magicSwordPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "Phase 4 Requirements Missing",
                "Player: " + (player != null) +
                "\nPhase 3 root: " + (phaseThreeRoot != null) +
                "\nQuestManager: " + (questManager != null) +
                "\nStoryReferences: " + (storyReferences != null) +
                "\nMain gate: " + (mainGate != null) +
                "\nDefault demon: " + (demonDefaultPrefab != null) +
                "\nDamaged demon: " + (demonDamagedPrefab != null) +
                "\nMagic sword: " + (magicSwordPrefab != null),
                "OK");
            return;
        }

        DeleteExistingRoot();

        List<string> report =
            new List<string>
            {
                "PHASE 4 FINAL GATE REPORT",
                "Scene: " +
                SceneManager.GetActiveScene().path,
                "Built: " +
                DateTime.Now,
                ""
            };

        GameObject oldLeftGuardian =
            FindSceneObject(
                "Gate Guardian Left");

        GameObject oldRightGuardian =
            FindSceneObject(
                "Gate Guardian Right");

        if (oldLeftGuardian != null)
        {
            Undo.RecordObject(
                oldLeftGuardian,
                "Disable Left Wyvern");

            oldLeftGuardian.SetActive(false);
        }

        if (oldRightGuardian != null)
        {
            Undo.RecordObject(
                oldRightGuardian,
                "Disable Right Wyvern");

            oldRightGuardian.SetActive(false);
        }

        GameObject root =
            new GameObject(RootName);

        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Phase 4 Final Gate");

        Camera playerCamera =
            FindFirstPersonCamera(player);

        CombatHealth playerHealth =
            player.GetComponent<CombatHealth>();

        SwordDamageDealer damageDealer =
            player.GetComponent<SwordDamageDealer>();

        Behaviour firstPersonMovement =
            FindBehaviourByTypeName(
                player,
                "FirstPersonPlayer");

        Behaviour firstPersonSword =
            FindBehaviourByTypeName(
                player,
                "FirstPersonSword");

        CharacterController playerController =
            player.GetComponent<CharacterController>();

        Vector3 towardPlayer =
            player.transform.position -
            mainGate.transform.position;

        towardPlayer.y = 0f;

        if (towardPlayer.sqrMagnitude < 0.001f)
            towardPlayer = -mainGate.transform.forward;

        towardPlayer.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                towardPlayer).normalized;

        Vector3 leftPosition =
            oldLeftGuardian != null
                ? oldLeftGuardian.transform.position
                : mainGate.transform.position +
                  towardPlayer * 10.5f -
                  side * 7.5f;

        Vector3 rightPosition =
            oldRightGuardian != null
                ? oldRightGuardian.transform.position
                : mainGate.transform.position +
                  towardPlayer * 10.5f +
                  side * 7.5f;

        leftPosition =
            SampleNavMeshPosition(
                leftPosition,
                16f);

        rightPosition =
            SampleNavMeshPosition(
                rightPosition,
                16f);

        Quaternion faceApproach =
            Quaternion.LookRotation(
                towardPlayer,
                Vector3.up);

        GateGuardianAI leftGuardian =
            CreateGateGuardian(
                demonDefaultPrefab,
                "Gate Horror Guardian Left",
                leftPosition,
                faceApproach,
                player.transform,
                playerHealth,
                root.transform,
                460f,
                report);

        GateGuardianAI rightGuardian =
            CreateGateGuardian(
                demonDamagedPrefab,
                "Gate Horror Guardian Right",
                rightPosition,
                faceApproach,
                player.transform,
                playerHealth,
                root.transform,
                460f,
                report);

        Transform strikePoint =
            CreatePoint(
                root.transform,
                "Gate Throw Strike Point",
                SampleNavMeshPosition(
                    mainGate.transform.position +
                    towardPlayer * 7.2f,
                    12f),
                faceApproach);

        Transform battleCenter =
            CreatePoint(
                root.transform,
                "Gate Battle Objective",
                Vector3.Lerp(
                    leftPosition,
                    rightPosition,
                    0.5f),
                faceApproach);

        GateThrowbackSequence throwback =
            player.GetComponent<GateThrowbackSequence>();

        if (throwback == null)
        {
            throwback =
                Undo.AddComponent<GateThrowbackSequence>(
                    player);
        }

        throwback.Configure(
            player.transform,
            playerCamera,
            playerController,
            firstPersonMovement,
            firstPersonSword,
            damageDealer,
            playerHealth);

        GateBattleSequence gateBattle =
            Undo.AddComponent<GateBattleSequence>(
                root);

        gateBattle.Configure(
            questManager,
            hud,
            leftGuardian,
            rightGuardian,
            throwback,
            strikePoint);

        MagicSwordUpgrade swordUpgrade =
            BuildMagicSwordUpgrade(
                player,
                damageDealer,
                magicSwordPrefab,
                report);

        QuestObjectiveIndicator indicator =
            Undo.AddComponent<QuestObjectiveIndicator>(
                root);

        GameObject forge =
            FindSceneObject("Forge");

        GameObject warningLetter =
            FindSceneObject("Warning Letter");

        GameObject castleEntrance =
            FindSceneObject("Castle Front Gate");

        GameObject blacksmith =
            FindSceneObject("Imprisoned Key Maker");

        GameObject castleDemon =
            FindSceneObject("Demon Ambush Boss");

        GameObject captivePoint =
            FindSceneObject(
                "Story Point - Tree Captive");

        GameObject workshopPoint =
            FindSceneObject(
                "Story Point - Workshop");

        GameObject escapePoint =
            FindSceneObject(
                "Final Escape Trigger");

        indicator.Configure(
            questManager,
            playerCamera,
            mainGate.transform,
            forge != null
                ? forge.transform
                : mainGate.transform,
            warningLetter != null
                ? warningLetter.transform
                : mainGate.transform,
            castleEntrance != null
                ? castleEntrance.transform
                : mainGate.transform,
            blacksmith != null
                ? blacksmith.transform
                : mainGate.transform,
            castleDemon != null
                ? castleDemon.transform
                : mainGate.transform,
            captivePoint != null
                ? captivePoint.transform
                : mainGate.transform,
            workshopPoint != null
                ? workshopPoint.transform
                : forge != null
                    ? forge.transform
                    : mainGate.transform,
            battleCenter,
            escapePoint != null
                ? escapePoint.transform
                : mainGate.transform);

        storyReferences.ConfigurePhaseFour(
            gateBattle,
            swordUpgrade,
            indicator);

        GameObject finalKeyPreview =
            FindSceneObject(
                "Final Gate Key Preview");

        if (finalKeyPreview != null)
            finalKeyPreview.SetActive(false);

        GatePassageController passage =
            phaseThreeRoot.GetComponentInChildren<GatePassageController>(
                true);

        if (passage != null)
            passage.SetClosedInstantly();

        QuestManager.ResetSavedProgress();

        EnsureFolder("Assets/Generated");

        report.Add(
            "Disabled both original wyvern gate guardians.");

        report.Add(
            "Created two frozen horror-demon guardians.");

        report.Add(
            "Magic sword prefab: " +
            MagicSwordPrefabPath);

        report.Add(
            "Objective indicator configured for all quest destinations.");

        report.Add(
            "Permanent key preview disabled.");

        File.WriteAllLines(
            ReportPath,
            report);

        AssetDatabase.ImportAsset(
            ReportPath);

        EditorUtility.SetDirty(
            storyReferences);

        EditorUtility.SetDirty(
            gateBattle);

        EditorUtility.SetDirty(
            swordUpgrade);

        EditorUtility.SetDirty(
            indicator);

        EditorUtility.SetDirty(
            throwback);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        EditorUtility.DisplayDialog(
            "Phase 4 Built",
            "Two frozen horror demons now replace the gate wyverns.\n\n" +
            "The blacksmith gives the player the key and stronger magic sword.\n" +
            "Attempting the gate starts the throwback and two-enemy fight.\n" +
            "The gate opens only after both demons die.\n" +
            "Objective arrows and distance markers are active.\n\n" +
            "Quest progress was reset.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 4/Start Final Gate Test",
        priority = 511)]
    public static void StartFinalGateTest()
    {
        PlayerPrefs.SetInt(
            "DungeonGame.Phase3.Stage",
            (int)QuestStage.ReturnToMainGate);

        PlayerPrefs.SetInt(
            "DungeonGame.Phase3.HasKey",
            1);

        PlayerPrefs.SetInt(
            "DungeonGame.Phase3.Complete",
            0);

        PlayerPrefs.Save();

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        GameObject gate =
            FindSceneObject("Main Gate Arch");

        if (player != null &&
            gate != null)
        {
            Vector3 direction =
                player.transform.position -
                gate.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                direction = -gate.transform.forward;

            direction.Normalize();

            Vector3 position =
                SampleNavMeshPosition(
                    gate.transform.position +
                    direction * 18f,
                    12f);

            CharacterController controller =
                player.GetComponent<CharacterController>();

            if (controller != null)
                controller.enabled = false;

            player.transform.position = position;

            if (controller != null)
                controller.enabled = true;
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Final Gate Test Prepared",
            "Press Play. You have the key and magic sword, and the objective begins at the main gate.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 4/Delete Final Gate Systems",
        priority = 512)]
    public static void Delete()
    {
        DeleteExistingRoot();

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        if (player != null)
        {
            RemoveComponent<GateThrowbackSequence>(
                player);

            RemoveComponent<MagicSwordUpgrade>(
                player);
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
    }

    private static GateGuardianAI CreateGateGuardian(
        GameObject prefab,
        string objectName,
        Vector3 position,
        Quaternion rotation,
        Transform player,
        CombatHealth playerHealth,
        Transform parent,
        float hitPoints,
        List<string> report)
    {
        GameObject guardian =
            PrefabUtility.InstantiatePrefab(
                prefab,
                SceneManager.GetActiveScene())
            as GameObject;

        if (guardian == null)
            throw new InvalidOperationException(
                "Could not instantiate " +
                prefab.name);

        Undo.RegisterCreatedObjectUndo(
            guardian,
            "Create " + objectName);

        guardian.name = objectName;

        Undo.SetTransformParent(
            guardian.transform,
            parent,
            "Parent " + objectName);

        guardian.transform.SetPositionAndRotation(
            position,
            rotation);

        ScaleToHeight(
            guardian,
            3.55f);

        guardian.transform.position =
            SampleNavMeshPosition(
                guardian.transform.position,
                12f);

        UpgradeInstanceMaterialsToURP(
            guardian,
            report);

        Animator animator =
            guardian.GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;
        }

        foreach (Collider collider in
                 guardian.GetComponentsInChildren<Collider>(true))
        {
            Undo.DestroyObjectImmediate(collider);
        }

        NavMeshAgent agent =
            guardian.GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            agent =
                Undo.AddComponent<NavMeshAgent>(
                    guardian);
        }

        ConfigureAgentFromBounds(
            guardian,
            agent);

        CapsuleCollider bodyCollider =
            Undo.AddComponent<CapsuleCollider>(
                guardian);

        ConfigureCapsuleFromBounds(
            guardian,
            bodyCollider);

        CombatHealth health =
            guardian.GetComponent<CombatHealth>();

        if (health == null)
        {
            health =
                Undo.AddComponent<CombatHealth>(
                    guardian);
        }

        health.Configure(
            hitPoints,
            CombatTeam.Enemy,
            0.10f,
            false);

        if (guardian.GetComponent<DamageFlash>() == null)
            Undo.AddComponent<DamageFlash>(guardian);

        CombatWorldHealthBar healthBar =
            guardian.GetComponent<CombatWorldHealthBar>();

        if (healthBar == null)
        {
            healthBar =
                Undo.AddComponent<CombatWorldHealthBar>(
                    guardian);
        }

        healthBar.Configure(
            health,
            false);

        GateGuardianAI ai =
            Undo.AddComponent<GateGuardianAI>(
                guardian);

        ai.Configure(
            animator,
            agent,
            health,
            player,
            playerHealth,
            bodyCollider);

        ai.SetFrozen();

        report.Add(
            objectName +
            " created from " +
            AssetDatabase.GetAssetPath(prefab));

        return ai;
    }

    private static MagicSwordUpgrade BuildMagicSwordUpgrade(
        GameObject player,
        SwordDamageDealer damageDealer,
        GameObject magicSwordPrefab,
        List<string> report)
    {
        GameObject swordHolder =
            FindChildByNameContains(
                player.transform,
                "SwordHolder");

        if (swordHolder == null)
            swordHolder = player;

        GameObject existingMagic =
            FindSceneObject(
                "Player Magic Sword");

        if (existingMagic != null)
            Undo.DestroyObjectImmediate(existingMagic);

        Transform oldVisualRoot =
            FindVisibleDirectChild(
                swordHolder.transform);

        GameObject magicSword =
            PrefabUtility.InstantiatePrefab(
                magicSwordPrefab,
                SceneManager.GetActiveScene())
            as GameObject;

        if (magicSword == null)
            throw new InvalidOperationException(
                "Could not instantiate magic sword.");

        Undo.RegisterCreatedObjectUndo(
            magicSword,
            "Create Player Magic Sword");

        magicSword.name =
            "Player Magic Sword";

        Undo.SetTransformParent(
            magicSword.transform,
            swordHolder.transform,
            "Parent Player Magic Sword");

        if (oldVisualRoot != null)
        {
            magicSword.transform.localPosition =
                oldVisualRoot.localPosition;

            magicSword.transform.localRotation =
                oldVisualRoot.localRotation *
                Quaternion.Euler(
                    -90f,
                    0f,
                    0f);

            magicSword.transform.localScale =
                Vector3.one;

            MatchLargestDimension(
                magicSword,
                oldVisualRoot.gameObject,
                1.15f);
        }
        else
        {
            magicSword.transform.localPosition =
                new Vector3(
                    0.22f,
                    -0.42f,
                    0.72f);

            magicSword.transform.localRotation =
                Quaternion.Euler(
                    80f,
                    8f,
                    10f);

            magicSword.transform.localScale =
                Vector3.one * 0.38f;
        }

        foreach (Collider collider in
                 magicSword.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        UpgradeInstanceMaterialsToURP(
            magicSword,
            report);

        MagicSwordUpgrade upgrade =
            player.GetComponent<MagicSwordUpgrade>();

        if (upgrade == null)
        {
            upgrade =
                Undo.AddComponent<MagicSwordUpgrade>(
                    player);
        }

        upgrade.Configure(
            damageDealer,
            oldVisualRoot != null
                ? oldVisualRoot.gameObject
                : null,
            magicSword);

        magicSword.SetActive(false);

        report.Add(
            "Magic sword equipped as a replacement first-person weapon.");

        return upgrade;
    }

    private static void UpgradeInstanceMaterialsToURP(
        GameObject root,
        List<string> report)
    {
        Shader urpShader =
            Shader.Find(
                "Universal Render Pipeline/Lit");

        if (urpShader == null)
        {
            report.Add(
                "WARNING: URP/Lit shader was not found.");

            return;
        }

        EnsureFolder(
            GeneratedMaterialFolder);

        Dictionary<Material, Material> replacements =
            new Dictionary<Material, Material>();

        foreach (Renderer renderer in
                 root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials =
                renderer.sharedMaterials;

            bool changed = false;

            for (int index = 0;
                 index < materials.Length;
                 index++)
            {
                Material original =
                    materials[index];

                if (original == null)
                    continue;

                if (original.shader != null &&
                    original.shader.name.Contains(
                        "Universal Render Pipeline"))
                {
                    continue;
                }

                if (!replacements.TryGetValue(
                        original,
                        out Material converted))
                {
                    converted =
                        CreateURPMaterialCopy(
                            original,
                            urpShader);

                    replacements.Add(
                        original,
                        converted);
                }

                materials[index] = converted;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials =
                    materials;

                EditorUtility.SetDirty(
                    renderer);
            }
        }
    }

    private static Material CreateURPMaterialCopy(
        Material original,
        Shader urpShader)
    {
        string safeName =
            string.Concat(
                original.name.Select(
                    character =>
                        char.IsLetterOrDigit(character)
                            ? character
                            : '_'));

        string path =
            GeneratedMaterialFolder +
            "/" +
            safeName +
            "_URP.mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(
                path);

        if (existing != null)
            return existing;

        Material converted =
            new Material(urpShader);

        converted.name =
            safeName +
            "_URP";

        if (original.HasProperty("_Color"))
        {
            converted.SetColor(
                "_BaseColor",
                original.GetColor("_Color"));
        }

        CopyTexture(
            original,
            "_MainTex",
            converted,
            "_BaseMap");

        CopyTexture(
            original,
            "_BumpMap",
            converted,
            "_BumpMap");

        CopyTexture(
            original,
            "_MetallicGlossMap",
            converted,
            "_MetallicGlossMap");

        CopyTexture(
            original,
            "_EmissionMap",
            converted,
            "_EmissionMap");

        if (original.HasProperty(
                "_EmissionColor"))
        {
            Color emission =
                original.GetColor(
                    "_EmissionColor");

            converted.SetColor(
                "_EmissionColor",
                emission);

            if (emission.maxColorComponent > 0.001f)
                converted.EnableKeyword("_EMISSION");
        }

        if (converted.GetTexture("_BumpMap") != null)
            converted.EnableKeyword("_NORMALMAP");

        AssetDatabase.CreateAsset(
            converted,
            path);

        return converted;
    }

    private static void CopyTexture(
        Material source,
        string sourceProperty,
        Material destination,
        string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) ||
            !destination.HasProperty(destinationProperty))
        {
            return;
        }

        Texture texture =
            source.GetTexture(
                sourceProperty);

        if (texture != null)
        {
            destination.SetTexture(
                destinationProperty,
                texture);
        }
    }

    private static void ConfigureAgentFromBounds(
        GameObject target,
        NavMeshAgent agent)
    {
        agent.speed = 4.7f;
        agent.acceleration = 24f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 2.35f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 65;

        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            agent.height = 3f;
            agent.radius = 0.7f;
            return;
        }

        Vector3 scale =
            target.transform.lossyScale;

        agent.height =
            Mathf.Max(
                1f,
                SafeDivide(
                    bounds.size.y,
                    Mathf.Abs(scale.y)) *
                0.92f);

        agent.radius =
            Mathf.Clamp(
                Mathf.Min(
                    SafeDivide(
                        bounds.extents.x,
                        Mathf.Abs(scale.x)),
                    SafeDivide(
                        bounds.extents.z,
                        Mathf.Abs(scale.z))) *
                0.65f,
                0.3f,
                1.2f);
    }

    private static void ConfigureCapsuleFromBounds(
        GameObject target,
        CapsuleCollider collider)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds bounds))
        {
            collider.height = 3f;
            collider.radius = 0.7f;
            collider.center =
                Vector3.up * 1.5f;
            return;
        }

        Vector3 scale =
            target.transform.lossyScale;

        collider.direction = 1;

        collider.height =
            Mathf.Max(
                1f,
                SafeDivide(
                    bounds.size.y,
                    Mathf.Abs(scale.y)) *
                0.92f);

        collider.radius =
            Mathf.Clamp(
                Mathf.Min(
                    SafeDivide(
                        bounds.extents.x,
                        Mathf.Abs(scale.x)),
                    SafeDivide(
                        bounds.extents.z,
                        Mathf.Abs(scale.z))) *
                0.7f,
                0.28f,
                collider.height * 0.45f);

        collider.center =
            target.transform.InverseTransformPoint(
                bounds.center);
    }

    private static void MatchLargestDimension(
        GameObject target,
        GameObject reference,
        float multiplier)
    {
        if (!TryGetRendererBounds(
                target,
                out Bounds targetBounds) ||
            !TryGetRendererBounds(
                reference,
                out Bounds referenceBounds))
        {
            return;
        }

        float targetSize =
            Mathf.Max(
                targetBounds.size.x,
                targetBounds.size.y,
                targetBounds.size.z);

        float referenceSize =
            Mathf.Max(
                referenceBounds.size.x,
                referenceBounds.size.y,
                referenceBounds.size.z);

        if (targetSize <= 0.0001f)
            return;

        target.transform.localScale *=
            referenceSize *
            multiplier /
            targetSize;
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
            desiredHeight /
            currentHeight;
    }

    private static Transform CreatePoint(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation)
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

        point.transform.SetPositionAndRotation(
            position,
            rotation);

        return point.transform;
    }

    private static Camera FindFirstPersonCamera(
        GameObject player)
    {
        foreach (Camera camera in
                 player.GetComponentsInChildren<Camera>(true))
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

    private static Transform FindVisibleDirectChild(
        Transform root)
    {
        Renderer renderer =
            root.GetComponentInChildren<Renderer>(true);

        if (renderer == null)
            return null;

        Transform current =
            renderer.transform;

        while (current.parent != null &&
               current.parent != root)
        {
            current = current.parent;
        }

        return current;
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

    private static Vector3 SampleNavMeshPosition(
        Vector3 desired,
        float radius)
    {
        if (NavMesh.SamplePosition(
                desired,
                out NavMeshHit hit,
                radius,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain != null &&
            terrain.terrainData != null)
        {
            desired.y =
                terrain.SampleHeight(desired) +
                terrain.transform.position.y;
        }

        return desired;
    }

    private static float SafeDivide(
        float value,
        float divisor)
    {
        return divisor > 0.0001f
            ? value / divisor
            : value;
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

    private static void DeleteExistingRoot()
    {
        GameObject root =
            FindSceneObject(RootName);

        if (root != null)
            Undo.DestroyObjectImmediate(root);
    }

    private static void RemoveComponent<T>(
        GameObject target)
        where T : Component
    {
        T component =
            target.GetComponent<T>();

        if (component != null)
            Undo.DestroyObjectImmediate(component);
    }
}
