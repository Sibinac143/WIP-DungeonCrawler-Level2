using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhaseTwoCombatSetup
{
    private const string RootName = "PHASE_02_COMBAT";
    private const string PhaseOneRootName = "PHASE_01_ACTUAL_ASSETS";
    private const string ReportPath =
        "Assets/Generated/PhaseTwoCombatReport.txt";

    private const string BarrelPrefabPath =
        "Assets/Aletheia/Prefabs/barrel.prefab";

    [MenuItem(
        "Tools/Dungeon Game/Phase 2/Build Combat Foundation",
        priority = 200)]
    public static void BuildCombatFoundation()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building Phase 2.",
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
                "A GameObject named player/Player or a GameObject with " +
                "FirstPersonPlayer could not be found.",
                "OK");
            return;
        }

        DeleteGeneratedRoot(false);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(
            root,
            "Build Phase 2 Combat");

        List<string> report = new List<string>
        {
            "PHASE 2 COMBAT FOUNDATION REPORT",
            "Scene: " + SceneManager.GetActiveScene().path,
            "Built: " + DateTime.Now,
            ""
        };

        Transform respawnPoint =
            CreateRespawnPoint(root.transform, player);

        ConfigurePlayer(
            player,
            respawnPoint,
            report);

        CreateCombatHUD(
            root.transform,
            player,
            report);

        CreatePracticeTargets(
            root.transform,
            player,
            report);

        ConfigureDragons(report);

        EnsureFolder("Assets/Generated");
        File.WriteAllLines(ReportPath, report);
        AssetDatabase.ImportAsset(ReportPath);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        EditorUtility.DisplayDialog(
            "Phase 2 Combat Built",
            "Player health, sword damage, blocking, respawn, HUD, " +
            "practice targets, dragon health, and dragon contact damage " +
            "were configured.\n\n" +
            "Press Play and test:\n" +
            "LMB = light attack\n" +
            "E = heavy attack\n" +
            "RMB = block\n\n" +
            "Report:\n" +
            ReportPath,
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 2/Delete Combat Foundation",
        priority = 201)]
    public static void DeleteCombatFoundation()
    {
        DeleteGeneratedRoot(true);
        RemoveManagedCombatComponents();
        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 2/Reconfigure Dragons",
        priority = 202)]
    public static void ReconfigureDragons()
    {
        List<string> report = new List<string>();
        int count = ConfigureDragons(report);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog(
            "Dragon Combat Updated",
            "Configured dragons: " + count,
            "OK");
    }

    private static void ConfigurePlayer(
        GameObject player,
        Transform respawnPoint,
        List<string> report)
    {
        CombatHealth health =
            GetOrAddComponent<CombatHealth>(player);

        health.Configure(
            100f,
            CombatTeam.Player,
            0.15f,
            false);

        PlayerDefense defense =
            GetOrAddComponent<PlayerDefense>(player);

        Camera playerCamera =
            player.GetComponentInChildren<Camera>(true);

        if (playerCamera == null)
            playerCamera = Camera.main;

        SwordDamageDealer swordDamage =
            GetOrAddComponent<SwordDamageDealer>(player);

        swordDamage.Configure(
            playerCamera,
            health);

        PlayerRespawn respawn =
            GetOrAddComponent<PlayerRespawn>(player);

        respawn.Configure(
            health,
            respawnPoint);

        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(defense);
        EditorUtility.SetDirty(swordDamage);
        EditorUtility.SetDirty(respawn);

        report.Add(
            "PLAYER: " +
            player.name +
            " | HP=100" +
            " | camera=" +
            (playerCamera != null
                ? playerCamera.name
                : "NONE"));
    }

    private static Transform CreateRespawnPoint(
        Transform parent,
        GameObject player)
    {
        GameObject respawn =
            new GameObject("Player Respawn Point");

        Undo.RegisterCreatedObjectUndo(
            respawn,
            "Create Player Respawn Point");

        Undo.SetTransformParent(
            respawn.transform,
            parent,
            "Parent Player Respawn Point");

        respawn.transform.SetPositionAndRotation(
            player.transform.position,
            player.transform.rotation);

        respawn.AddComponent<PhaseTwoManagedObject>();

        return respawn.transform;
    }

    private static void CreateCombatHUD(
        Transform parent,
        GameObject player,
        List<string> report)
    {
        GameObject hudObject =
            new GameObject("Combat HUD");

        Undo.RegisterCreatedObjectUndo(
            hudObject,
            "Create Combat HUD");

        Undo.SetTransformParent(
            hudObject.transform,
            parent,
            "Parent Combat HUD");

        CombatHUD hud =
            Undo.AddComponent<CombatHUD>(
                hudObject);

        hud.Configure(
            player.GetComponent<CombatHealth>());

        Undo.AddComponent<PhaseTwoManagedObject>(
            hudObject);

        report.Add("HUD: CombatHUD created.");
    }

    private static void CreatePracticeTargets(
        Transform parent,
        GameObject player,
        List<string> report)
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                BarrelPrefabPath);

        if (prefab == null)
        {
            report.Add(
                "WARNING: Practice target prefab missing: " +
                BarrelPrefabPath);
            return;
        }

        Transform targetsRoot =
            CreateGroup(
                "Practice Targets",
                parent);

        Vector3 forward =
            Vector3.ProjectOnPlane(
                player.transform.forward,
                Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        Vector3[] positions =
        {
            player.transform.position +
            forward * 3.2f -
            right * 1.3f,

            player.transform.position +
            forward * 4.2f +
            right * 1.5f
        };

        for (int index = 0;
             index < positions.Length;
             index++)
        {
            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    prefab,
                    SceneManager.GetActiveScene()) as GameObject;

            if (instance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(
                instance,
                "Create Practice Target");

            Undo.SetTransformParent(
                instance.transform,
                targetsRoot,
                "Parent Practice Target");

            instance.name =
                "Training Target " +
                (char)('A' + index);

            instance.transform.position =
                GroundPositionToTerrain(
                    positions[index]);

            instance.transform.rotation =
                Quaternion.Euler(
                    0f,
                    index * 27f,
                    0f);

            instance.transform.localScale *=
                index == 0 ? 1.1f : 1.35f;

            GroundRendererBottomToTerrain(instance);
            AddBoundsColliderIfMissing(instance);

            CombatHealth health =
                GetOrAddComponent<CombatHealth>(
                    instance);

            health.Configure(
                index == 0 ? 60f : 90f,
                CombatTeam.Enemy,
                0.05f,
                false);

            DamageFlash flash =
                GetOrAddComponent<DamageFlash>(
                    instance);

            EnemyRespawner respawner =
                GetOrAddComponent<EnemyRespawner>(
                    instance);

            respawner.Configure(
                health,
                4f + index);

            CombatWorldHealthBar healthBar =
                GetOrAddComponent<CombatWorldHealthBar>(
                    instance);

            healthBar.Configure(
                health,
                true);

            GetOrAddComponent<PhaseTwoManagedObject>(
                instance);

            EditorUtility.SetDirty(health);
            EditorUtility.SetDirty(flash);
            EditorUtility.SetDirty(respawner);
            EditorUtility.SetDirty(healthBar);

            report.Add(
                "PRACTICE TARGET: " +
                instance.name +
                " | HP=" +
                health.MaxHealth);
        }
    }

    private static int ConfigureDragons(
        List<string> report)
    {
        GameObject phaseOneRoot =
            GameObject.Find(PhaseOneRootName);

        if (phaseOneRoot == null)
        {
            report.Add(
                "WARNING: " +
                PhaseOneRootName +
                " not found. No dragons configured.");
            return 0;
        }

        Transform[] transforms =
            phaseOneRoot.GetComponentsInChildren<Transform>(true);

        HashSet<GameObject> dragons =
            new HashSet<GameObject>();

        foreach (Transform candidate in transforms)
        {
            string lower =
                candidate.name.ToLowerInvariant();

            bool nameMatches =
                lower.Contains("guardian") ||
                lower.Contains("kuzar") ||
                lower.Contains("wyvern") ||
                lower.Contains("dragon");

            if (!nameMatches)
                continue;

            Animator animator =
                candidate.GetComponentInChildren<Animator>(true);

            SkinnedMeshRenderer skinned =
                candidate.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (animator == null && skinned == null)
                continue;

            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    candidate.gameObject);

            GameObject dragon =
                prefabRoot != null
                    ? prefabRoot
                    : candidate.gameObject;

            if (dragon.transform.IsChildOf(
                    phaseOneRoot.transform))
            {
                dragons.Add(dragon);
            }
        }

        foreach (GameObject dragon in dragons)
        {
            string lower =
                dragon.name.ToLowerInvariant();

            bool isBoss =
                lower.Contains("boss") ||
                IsUnderNamedParent(
                    dragon.transform,
                    "CASTLE_BOSS_ARENA");

            AddBoundsColliderIfMissing(dragon);

            CombatHealth health =
                GetOrAddComponent<CombatHealth>(
                    dragon);

            health.Configure(
                isBoss ? 600f : 350f,
                CombatTeam.Enemy,
                0.10f,
                false);

            DamageFlash flash =
                GetOrAddComponent<DamageFlash>(
                    dragon);

            EnemyRespawner respawner =
                GetOrAddComponent<EnemyRespawner>(
                    dragon);

            respawner.Configure(
                health,
                isBoss ? 15f : 10f);

            CombatWorldHealthBar healthBar =
                GetOrAddComponent<CombatWorldHealthBar>(
                    dragon);

            healthBar.Configure(
                health,
                false);

            ContactDamage contact =
                GetOrAddComponent<ContactDamage>(
                    dragon);

            contact.Configure(
                health,
                isBoss ? 32f : 22f,
                isBoss ? 4.5f : 3.6f,
                isBoss ? 0.85f : 1.05f);

            EditorUtility.SetDirty(health);
            EditorUtility.SetDirty(flash);
            EditorUtility.SetDirty(respawner);
            EditorUtility.SetDirty(healthBar);
            EditorUtility.SetDirty(contact);

            report.Add(
                "DRAGON: " +
                dragon.name +
                " | HP=" +
                health.MaxHealth +
                " | contact damage=" +
                (isBoss ? 32 : 22));
        }

        return dragons.Count;
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
            if (behaviour == null ||
                !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            if (behaviour.GetType().Name ==
                "FirstPersonPlayer")
            {
                return behaviour.gameObject;
            }
        }

        return null;
    }

    private static T GetOrAddComponent<T>(
        GameObject gameObject)
        where T : Component
    {
        T existing =
            gameObject.GetComponent<T>();

        if (existing != null)
            return existing;

        return Undo.AddComponent<T>(
            gameObject);
    }

    private static Transform CreateGroup(
        string name,
        Transform parent)
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

        group.transform.localPosition =
            Vector3.zero;

        group.transform.localRotation =
            Quaternion.identity;

        return group.transform;
    }

    private static void AddBoundsColliderIfMissing(
        GameObject instance)
    {
        if (instance.GetComponentInChildren<Collider>(true) != null)
            return;

        Renderer[] renderers =
            instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer))
                .ToArray();

        if (renderers.Length == 0)
            return;

        Bounds bounds =
            renderers[0].bounds;

        for (int index = 1;
             index < renderers.Length;
             index++)
        {
            bounds.Encapsulate(
                renderers[index].bounds);
        }

        BoxCollider collider =
            Undo.AddComponent<BoxCollider>(
                instance);

        collider.center =
            instance.transform.InverseTransformPoint(
                bounds.center);

        Vector3 scale =
            instance.transform.lossyScale;

        collider.size =
            new Vector3(
                SafeDivide(
                    bounds.size.x,
                    Mathf.Abs(scale.x)),
                SafeDivide(
                    bounds.size.y,
                    Mathf.Abs(scale.y)),
                SafeDivide(
                    bounds.size.z,
                    Mathf.Abs(scale.z)));
    }

    private static void GroundRendererBottomToTerrain(
        GameObject instance)
    {
        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return;
        }

        Renderer[] renderers =
            instance.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer))
                .ToArray();

        if (renderers.Length == 0)
            return;

        Bounds bounds =
            renderers[0].bounds;

        for (int index = 1;
             index < renderers.Length;
             index++)
        {
            bounds.Encapsulate(
                renderers[index].bounds);
        }

        Vector3 position =
            instance.transform.position;

        float terrainY =
            terrain.SampleHeight(position) +
            terrain.transform.position.y;

        instance.transform.position +=
            Vector3.up *
            (terrainY - bounds.min.y);
    }

    private static Vector3 GroundPositionToTerrain(
        Vector3 position)
    {
        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return position;
        }

        Vector3 origin =
            terrain.transform.position;

        Vector3 size =
            terrain.terrainData.size;

        bool inside =
            position.x >= origin.x &&
            position.x <= origin.x + size.x &&
            position.z >= origin.z &&
            position.z <= origin.z + size.z;

        if (!inside)
            return position;

        position.y =
            terrain.SampleHeight(position) +
            origin.y;

        return position;
    }

    private static bool IsUnderNamedParent(
        Transform transform,
        string parentName)
    {
        Transform current = transform;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    parentName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static float SafeDivide(
        float value,
        float divisor)
    {
        return divisor > 0.0001f
            ? value / divisor
            : value;
    }

    private static void DeleteGeneratedRoot(
        bool showDialog)
    {
        GameObject root =
            GameObject.Find(RootName);

        if (root == null)
        {
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Nothing To Delete",
                    RootName + " was not found.",
                    "OK");
            }

            return;
        }

        Undo.DestroyObjectImmediate(root);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 2 Root Deleted",
                RootName + " was removed.",
                "OK");
        }
    }

    private static void RemoveManagedCombatComponents()
    {
        RemoveComponentsOfType<CombatHealth>();
        RemoveComponentsOfType<PlayerDefense>();
        RemoveComponentsOfType<SwordDamageDealer>();
        RemoveComponentsOfType<PlayerRespawn>();
        RemoveComponentsOfType<ContactDamage>();
        RemoveComponentsOfType<DamageFlash>();
        RemoveComponentsOfType<EnemyRespawner>();
        RemoveComponentsOfType<CombatWorldHealthBar>();
    }

    private static void RemoveComponentsOfType<T>()
        where T : Component
    {
        T[] components =
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (T component in components)
        {
            if (component == null ||
                !component.gameObject.scene.IsValid())
            {
                continue;
            }

            Undo.DestroyObjectImmediate(component);
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
