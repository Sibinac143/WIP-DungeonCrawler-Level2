using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class PhaseFiveSpiderSetup
{
    private const string RootName =
        "PHASE_05_RECOVERY_SPIDERS";

    private const string OldNpcRootName =
        "RandomNPCs_Generated";

    private const string SpiderModelPath =
        "Assets/fantasySpider/spider_myOldOne.FBX";

    private const string OrangeMaterialPath =
        "Assets/fantasySpider/Materials/spider_01.mat";

    private const string BlackMaterialPath =
        "Assets/fantasySpider/Materials/spider_black_Std.mat";

    private const string NavMeshPath =
        "Assets/Generated/Navigation/PhaseThreeNavMesh.asset";

    private const string GeneratedMaterialFolder =
        "Assets/Generated/PhaseFiveMaterials";

    private const string ReportPath =
        "Assets/Generated/PhaseFiveSpiderHealthShieldReport.txt";

    private static readonly string[] RequiredClipNames =
    {
        "idle",
        "walk",
        "run",
        "attack1",
        "attack2",
        "hit1",
        "hit2",
        "death1",
        "death2",
        "jump",
        "taunt"
    };

    [MenuItem(
        "Tools/Dungeon Game/Phase 5/Build Spider Health and Shield System",
        priority = 610)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building Phase 5.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        GameObject spiderModel =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                SpiderModelPath);

        Material orangeSource =
            AssetDatabase.LoadAssetAtPath<Material>(
                OrangeMaterialPath);

        Material blackSource =
            AssetDatabase.LoadAssetAtPath<Material>(
                BlackMaterialPath);

        if (player == null ||
            spiderModel == null ||
            orangeSource == null ||
            blackSource == null)
        {
            EditorUtility.DisplayDialog(
                "Phase 5 Requirements Missing",
                "Player found: " + (player != null) +
                "\nSpider model found: " + (spiderModel != null) +
                "\nOrange material found: " + (orangeSource != null) +
                "\nBlack material found: " + (blackSource != null),
                "OK");
            return;
        }

        Dictionary<string, AnimationClip> clips =
            LoadRequiredClips();

        List<string> missingClips =
            RequiredClipNames
                .Where(name => !clips.ContainsKey(name))
                .ToList();

        if (missingClips.Count > 0)
        {
            EditorUtility.DisplayDialog(
                "Spider Animations Missing",
                "Missing clips:\n" +
                string.Join("\n", missingClips),
                "OK");
            return;
        }

        List<string> report =
            new List<string>
            {
                "PHASE 5 SPIDER HEALTH + SHIELD REPORT",
                "Scene: " + SceneManager.GetActiveScene().path,
                "Built: " + DateTime.Now,
                ""
            };

        DeleteExistingRoot();

        List<Vector3> spawnCandidates =
            CaptureAndRemoveAmbientNpcPositions(report);

        NavMeshData navMeshData =
            AssetDatabase.LoadAssetAtPath<NavMeshData>(
                NavMeshPath);

        NavMeshDataInstance navMeshInstance = default;

        if (navMeshData != null)
        {
            navMeshInstance =
                NavMesh.AddNavMeshData(navMeshData);

            report.Add(
                "Loaded PhaseThreeNavMesh for editor-time spider placement.");
        }
        else
        {
            report.Add(
                "WARNING: PhaseThreeNavMesh.asset was not found. Terrain-height fallback placement was used.");
        }

        PlayerShield shield =
            player.GetComponent<PlayerShield>();

        if (shield == null)
            shield = Undo.AddComponent<PlayerShield>(player);

        shield.Configure(100f, 0f);

        CombatHealth playerHealth =
            player.GetComponent<CombatHealth>();

        CombatHUD combatHud =
            FindFirstSceneComponent<CombatHUD>();

        if (combatHud != null)
            combatHud.Configure(playerHealth, shield);

        GameObject root =
            new GameObject(RootName);

        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Recovery Spiders");

        Material orangeMaterial =
            CreateSpiderURPMaterial(
                orangeSource,
                "Spider_Orange_URP");

        Material blackMaterial =
            CreateSpiderURPMaterial(
                blackSource,
                "Spider_Black_URP");

        ExpandSpawnCandidates(
            spawnCandidates,
            player.transform.position);

        List<Vector3> usableSpawns =
            BuildUsableSpawnList(
                spawnCandidates,
                player.transform.position,
                20);

        int created = 0;

        for (int index = 0;
             index < usableSpawns.Count;
             index++)
        {
            Material material =
                index % 2 == 0
                    ? blackMaterial
                    : orangeMaterial;

            GameObject spider =
                CreateSpider(
                    spiderModel,
                    clips,
                    material,
                    usableSpawns[index],
                    player.transform,
                    playerHealth,
                    root.transform,
                    index + 1,
                    report);

            if (spider != null)
                created++;
        }

        if (navMeshInstance.valid)
            navMeshInstance.Remove();

        EnsureFolder("Assets/Generated");

        report.Add("");
        report.Add("Recovery spiders created: " + created);
        report.Add("Spider HP: 45");
        report.Add("Spider temperament: passive until attacked by the player");
        report.Add("Spider bite damage after provocation: 6");
        report.Add("Kill healing: 25");
        report.Add("Kill shield reward: 20");
        report.Add("Maximum player shield: 100");
        report.Add("Old ambient NPC root removed: " + OldNpcRootName);

        File.WriteAllLines(ReportPath, report);
        AssetDatabase.ImportAsset(ReportPath);

        EditorUtility.SetDirty(shield);

        if (combatHud != null)
            EditorUtility.SetDirty(combatHud);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        EditorUtility.DisplayDialog(
            "Phase 5 Built",
            "The old random NPCs were removed and replaced with " +
            created +
            " passive recovery spiders.\n\n" +
            "Several spiders were prioritized in the wooded terrain around the castle.\n" +
            "They patrol peacefully and attack only after the player attacks first.\n" +
            "Spider kills restore up to 25 health and grant up to 20 shield.\n" +
            "Shield absorbs damage before health and is capped at 100.\n" +
            "A blue shield bar was added to the combat HUD.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 5/Prepare Spider Reward Test",
        priority = 611)]
    public static void PrepareRewardTest()
    {
        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        GameObject root =
            FindSceneObject(RootName);

        if (player == null || root == null)
        {
            EditorUtility.DisplayDialog(
                "Phase 5 Not Built",
                "Build the Spider Health and Shield System first.",
                "OK");
            return;
        }

        Transform firstSpider =
            root.transform.childCount > 0
                ? root.transform.GetChild(0)
                : null;

        if (firstSpider == null)
        {
            EditorUtility.DisplayDialog(
                "No Spider Found",
                "The Phase 5 root does not contain a spider.",
                "OK");
            return;
        }

        CombatHealth health =
            player.GetComponent<CombatHealth>();

        if (health != null)
        {
            SerializedObject serializedHealth =
                new SerializedObject(health);

            SerializedProperty currentHealth =
                serializedHealth.FindProperty("currentHealth");

            if (currentHealth != null)
                currentHealth.floatValue = 45f;

            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
        }

        PlayerShield shield =
            player.GetComponent<PlayerShield>();

        if (shield != null)
            shield.Configure(100f, 0f);

        Vector3 direction =
            player.transform.position -
            firstSpider.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.back;

        direction.Normalize();

        Vector3 targetPosition =
            FindGroundedPosition(
                firstSpider.position +
                direction * 5.5f);

        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        player.transform.position = targetPosition;

        if (controller != null)
            controller.enabled = true;

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Spider Reward Test Prepared",
            "The player now has 45 health and 0 shield and is positioned near a spider. Press Play, kill it, and confirm health and shield increase.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 5/Delete Spider Health and Shield System",
        priority = 612)]
    public static void Delete()
    {
        DeleteExistingRoot();

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        if (player != null)
        {
            PlayerShield shield =
                player.GetComponent<PlayerShield>();

            if (shield != null)
                Undo.DestroyObjectImmediate(shield);
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
    }

    private static GameObject CreateSpider(
        GameObject model,
        Dictionary<string, AnimationClip> clips,
        Material material,
        Vector3 position,
        Transform player,
        CombatHealth playerHealth,
        Transform parent,
        int number,
        List<string> report)
    {
        GameObject root =
            new GameObject(
                "Recovery Spider " +
                number.ToString("00"));

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create Recovery Spider");

        Undo.SetTransformParent(
            root.transform,
            parent,
            "Parent Recovery Spider");

        root.transform.position = position;
        root.transform.rotation =
            Quaternion.Euler(
                0f,
                UnityEngine.Random.Range(0f, 360f),
                0f);

        GameObject visual =
            PrefabUtility.InstantiatePrefab(
                model,
                SceneManager.GetActiveScene())
            as GameObject;

        if (visual == null)
        {
            Undo.DestroyObjectImmediate(root);
            return null;
        }

        Undo.RegisterCreatedObjectUndo(
            visual,
            "Create Spider Visual");

        visual.name = "Spider Visual";

        Undo.SetTransformParent(
            visual.transform,
            root.transform,
            "Parent Spider Visual");

        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        ScaleToHeight(visual, 0.78f);
        AlignVisualBottomToRoot(visual, root.transform);
        AssignMaterial(visual, material);

        Animation animation =
            visual.GetComponent<Animation>();

        if (animation == null)
            animation = Undo.AddComponent<Animation>(visual);

        ConfigureLegacyAnimation(animation, clips);

        NavMeshAgent agent =
            Undo.AddComponent<NavMeshAgent>(root);

        ConfigureAgentFromBounds(root, agent);

        BoxCollider collider =
            Undo.AddComponent<BoxCollider>(root);

        ConfigureBoxColliderFromBounds(root, collider);

        CombatHealth health =
            Undo.AddComponent<CombatHealth>(root);

        health.Configure(
            45f,
            CombatTeam.Enemy,
            0.08f,
            false);

        if (root.GetComponent<DamageFlash>() == null)
            Undo.AddComponent<DamageFlash>(root);

        CombatWorldHealthBar healthBar =
            Undo.AddComponent<CombatWorldHealthBar>(root);

        healthBar.Configure(health, false);

        SpiderEnemyAI ai =
            Undo.AddComponent<SpiderEnemyAI>(root);

        ai.Configure(
            animation,
            agent,
            health,
            player,
            playerHealth);

        SpiderRecoveryReward reward =
            Undo.AddComponent<SpiderRecoveryReward>(root);

        reward.Configure(
            health,
            25f,
            20f);

        report.Add(
            root.name +
            " at " +
            position.ToString("F1") +
            " using " +
            material.name);

        return root;
    }

    private static Dictionary<string, AnimationClip> LoadRequiredClips()
    {
        return
            AssetDatabase.LoadAllAssetsAtPath(SpiderModelPath)
                .OfType<AnimationClip>()
                .Where(clip =>
                    !clip.name.StartsWith(
                        "__preview__",
                        StringComparison.OrdinalIgnoreCase))
                .GroupBy(
                    clip => clip.name,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.First(),
                    StringComparer.OrdinalIgnoreCase);
    }

    private static void ConfigureLegacyAnimation(
        Animation animation,
        Dictionary<string, AnimationClip> clips)
    {
        animation.playAutomatically = false;
        animation.animatePhysics = false;
        animation.cullingType =
            AnimationCullingType.BasedOnRenderers;

        foreach (string name in RequiredClipNames)
        {
            AnimationClip clip = clips[name];

            if (animation.GetClip(name) == null)
                animation.AddClip(clip, name);
        }

        animation.clip = clips["idle"];
    }

    private static List<Vector3> CaptureAndRemoveAmbientNpcPositions(
        List<string> report)
    {
        List<Vector3> positions =
            new List<Vector3>();

        GameObject oldRoot =
            FindSceneObject(OldNpcRootName);

        if (oldRoot == null)
        {
            report.Add(
                "No RandomNPCs_Generated root was found.");
            return positions;
        }

        foreach (Transform child in oldRoot.transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) != null)
                positions.Add(child.position);
        }

        report.Add(
            "Captured " +
            positions.Count +
            " old NPC positions before removal.");

        Undo.DestroyObjectImmediate(oldRoot);
        report.Add("Removed RandomNPCs_Generated.");

        return positions;
    }

    private static void ExpandSpawnCandidates(
        List<Vector3> candidates,
        Vector3 playerPosition)
    {
        // Keep captured legacy positions, but place the new castle-jungle
        // candidates first so they are not excluded by the spawn count.
        List<Vector3> capturedPositions =
            new List<Vector3>(candidates);

        candidates.Clear();

        GameObject castle =
            FindSceneObject("Castle Front Gate");

        GameObject forge =
            FindSceneObject("Forge");

        GameObject rune =
            FindSceneObject("RuneSanctuary_Generated");

        GameObject gate =
            FindSceneObject("Main Gate Arch");

        if (castle != null)
        {
            AddCastleJungleCandidates(
                candidates,
                castle.transform.position);

            AddAnchorOffsets(
                candidates,
                castle.transform.position,
                new[]
                {
                    new Vector3(18f, 0f, 15f),
                    new Vector3(-21f, 0f, 17f),
                    new Vector3(27f, 0f, -8f),
                    new Vector3(-30f, 0f, -11f),
                    new Vector3(12f, 0f, 31f),
                    new Vector3(-15f, 0f, 35f),
                    new Vector3(37f, 0f, 12f),
                    new Vector3(-40f, 0f, 7f)
                });
        }

        if (rune != null)
        {
            AddAnchorOffsets(
                candidates,
                rune.transform.position,
                new[]
                {
                    new Vector3(14f, 0f, 12f),
                    new Vector3(-18f, 0f, 10f),
                    new Vector3(20f, 0f, -16f),
                    new Vector3(-22f, 0f, -15f)
                });
        }

        if (forge != null)
        {
            AddAnchorOffsets(
                candidates,
                forge.transform.position,
                new[]
                {
                    new Vector3(16f, 0f, 11f),
                    new Vector3(-19f, 0f, 13f),
                    new Vector3(23f, 0f, -14f),
                    new Vector3(-25f, 0f, -17f)
                });
        }

        if (gate != null)
        {
            AddAnchorOffsets(
                candidates,
                gate.transform.position,
                new[]
                {
                    new Vector3(20f, 0f, 18f),
                    new Vector3(-23f, 0f, 20f),
                    new Vector3(30f, 0f, -18f),
                    new Vector3(-31f, 0f, -20f)
                });
        }

        AddAnchorOffsets(
            candidates,
            playerPosition,
            new[]
            {
                new Vector3(22f, 0f, 15f),
                new Vector3(-24f, 0f, 17f),
                new Vector3(28f, 0f, -18f),
                new Vector3(-30f, 0f, -20f)
            });

        candidates.AddRange(capturedPositions);
    }

    private static void AddAnchorOffsets(
        List<Vector3> candidates,
        Vector3 anchor,
        IEnumerable<Vector3> offsets)
    {
        foreach (Vector3 offset in offsets)
            candidates.Add(anchor + offset);
    }

    private static void AddCastleJungleCandidates(
        List<Vector3> candidates,
        Vector3 castlePosition)
    {
        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return;
        }

        Vector3 terrainPosition =
            terrain.transform.position;

        Vector3 terrainSize =
            terrain.terrainData.size;

        List<Vector3> nearbyTrees =
            terrain.terrainData.treeInstances
                .Select(tree =>
                    terrainPosition +
                    Vector3.Scale(
                        tree.position,
                        terrainSize))
                .Where(position =>
                {
                    float distance =
                        Vector3.Distance(
                            position,
                            castlePosition);

                    return
                        distance >= 14f &&
                        distance <= 95f;
                })
                .OrderBy(position =>
                    Vector3.Distance(
                        position,
                        castlePosition))
                .ToList();

        if (nearbyTrees.Count == 0)
            return;

        int requested = 14;

        int step =
            Mathf.Max(
                1,
                nearbyTrees.Count /
                requested);

        int selected = 0;

        for (int index = 0;
             index < nearbyTrees.Count &&
             selected < requested;
             index += step)
        {
            Vector3 treePosition =
                nearbyTrees[index];

            float angle =
                selected *
                137.5f;

            float radius =
                4f +
                (selected % 3) *
                1.4f;

            Vector3 offset =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f) *
                Vector3.forward *
                radius;

            Vector3 candidate =
                treePosition +
                offset;

            bool tooClose =
                candidates.Any(existing =>
                    Vector3.Distance(
                        existing,
                        candidate) < 8f);

            if (!tooClose)
            {
                candidates.Add(candidate);
                selected++;
            }
        }
    }

    private static List<Vector3> BuildUsableSpawnList(
        List<Vector3> candidates,
        Vector3 playerPosition,
        int requestedCount)
    {
        List<Vector3> result =
            new List<Vector3>();

        foreach (Vector3 candidate in candidates)
        {
            Vector3 position =
                FindGroundedPosition(candidate);

            if (Vector3.Distance(position, playerPosition) < 9f)
                continue;

            bool tooClose =
                result.Any(existing =>
                    Vector3.Distance(existing, position) < 8f);

            if (tooClose)
                continue;

            result.Add(position);

            if (result.Count >= requestedCount)
                break;
        }

        return result;
    }

    private static Vector3 FindGroundedPosition(
        Vector3 desired)
    {
        if (NavMesh.SamplePosition(
                desired,
                out NavMeshHit hit,
                18f,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null &&
            terrain.terrainData != null)
        {
            desired.y =
                terrain.SampleHeight(desired) +
                terrain.transform.position.y;
        }

        return desired;
    }

    private static Material CreateSpiderURPMaterial(
        Material source,
        string assetName)
    {
        EnsureFolder(GeneratedMaterialFolder);

        string path =
            GeneratedMaterialFolder +
            "/" +
            assetName +
            ".mat";

        Material existing =
            AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
            return existing;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            throw new InvalidOperationException(
                "Universal Render Pipeline/Lit shader was not found.");

        Material material = new Material(shader);
        material.name = assetName;

        Texture baseTexture =
            source.HasProperty("_MainTex")
                ? source.GetTexture("_MainTex")
                : null;

        if (baseTexture != null)
            material.SetTexture("_BaseMap", baseTexture);

        Color baseColor =
            source.HasProperty("_Color")
                ? source.GetColor("_Color")
                : Color.white;

        material.SetColor("_BaseColor", baseColor);

        Texture normal =
            source.HasProperty("_BumpMap")
                ? source.GetTexture("_BumpMap")
                : AssetDatabase.LoadAssetAtPath<Texture>(
                    "Assets/fantasySpider/spider_01_Bump.png");

        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_Cutoff", 0.35f);
        material.EnableKeyword("_ALPHATEST_ON");

        material.SetFloat("_Smoothness", 0.22f);
        material.SetFloat("_Metallic", 0f);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AssignMaterial(
        GameObject target,
        Material material)
    {
        foreach (Renderer renderer in
                 target.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials =
                renderer.sharedMaterials;

            for (int index = 0;
                 index < materials.Length;
                 index++)
            {
                materials[index] = material;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void ConfigureAgentFromBounds(
        GameObject target,
        NavMeshAgent agent)
    {
        agent.speed = 3.4f;
        agent.acceleration = 18f;
        agent.angularSpeed = 900f;
        agent.stoppingDistance = 1.05f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 72;

        if (!TryGetRendererBounds(target, out Bounds bounds))
        {
            agent.height = 0.8f;
            agent.radius = 0.55f;
            return;
        }

        Vector3 scale = target.transform.lossyScale;

        agent.height =
            Mathf.Max(
                0.45f,
                SafeDivide(
                    bounds.size.y,
                    Mathf.Abs(scale.y)) *
                0.82f);

        agent.radius =
            Mathf.Clamp(
                Mathf.Min(
                    SafeDivide(
                        bounds.extents.x,
                        Mathf.Abs(scale.x)),
                    SafeDivide(
                        bounds.extents.z,
                        Mathf.Abs(scale.z))) *
                0.58f,
                0.28f,
                0.85f);

        agent.baseOffset = 0f;
    }

    private static void ConfigureBoxColliderFromBounds(
        GameObject target,
        BoxCollider collider)
    {
        if (!TryGetRendererBounds(target, out Bounds bounds))
        {
            collider.center =
                new Vector3(0f, 0.35f, 0f);

            collider.size =
                new Vector3(1.1f, 0.7f, 1.3f);
            return;
        }

        collider.center =
            target.transform.InverseTransformPoint(bounds.center);

        Vector3 min =
            target.transform.InverseTransformVector(bounds.size);

        collider.size =
            new Vector3(
                Mathf.Abs(min.x) * 0.82f,
                Mathf.Abs(min.y) * 0.85f,
                Mathf.Abs(min.z) * 0.82f);
    }

    private static void ScaleToHeight(
        GameObject target,
        float desiredHeight)
    {
        if (!TryGetRendererBounds(target, out Bounds bounds))
            return;

        float currentHeight =
            Mathf.Max(0.01f, bounds.size.y);

        target.transform.localScale *=
            desiredHeight / currentHeight;
    }

    private static void AlignVisualBottomToRoot(
        GameObject visual,
        Transform root)
    {
        if (!TryGetRendererBounds(visual, out Bounds bounds))
            return;

        visual.transform.position +=
            Vector3.up *
            (root.position.y - bounds.min.y);
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
            bounds.Encapsulate(renderers[index].bounds);
        }

        return true;
    }

    private static float SafeDivide(
        float value,
        float divisor)
    {
        return divisor > 0.0001f
            ? value / divisor
            : value;
    }

    private static T FindFirstSceneComponent<T>()
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
                return component;
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

    private static void DeleteExistingRoot()
    {
        GameObject root = FindSceneObject(RootName);

        if (root != null)
            Undo.DestroyObjectImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string folder = path.Substring(slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }
}
