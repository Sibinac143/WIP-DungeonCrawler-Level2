using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SwordAndNPCSetup
{
    private const string SwordPath =
        "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_11_Sword_One-Handed_003.prefab";

    private static readonly string[] NpcPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Male_Peasant_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Female_Peasant_01_a.prefab",
        "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Female_Peasant_01_b.prefab",
        "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Boy_Peasant_01.prefab"
    };

    private const string SwordName = "PlayerSword_Generated";
    private const string NpcRootName = "RandomNPCs_Generated";

    [MenuItem("Tools/Dungeon Game/Setup/Equip First-Person Sword")]
    public static void EquipFirstPersonSword()
    {
        GameObject swordPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(SwordPath);

        if (swordPrefab == null)
        {
            Debug.LogError($"Sword prefab not found: {SwordPath}");
            return;
        }

        Camera playerCamera = FindPlayerCamera();

        if (playerCamera == null)
        {
            EditorUtility.DisplayDialog(
                "Player Camera Not Found",
                "Select the player in the Hierarchy and run this command again, or make sure the player camera is tagged MainCamera.",
                "OK"
            );
            return;
        }

        Transform existing = playerCamera.transform.Find("SwordHolder");

        GameObject holderObject;

        if (existing != null)
        {
            holderObject = existing.gameObject;
        }
        else
        {
            holderObject = new GameObject("SwordHolder");
            Undo.RegisterCreatedObjectUndo(
                holderObject,
                "Create Sword Holder"
            );

            Undo.SetTransformParent(
                holderObject.transform,
                playerCamera.transform,
                "Parent Sword Holder"
            );
        }

        Transform holder = holderObject.transform;
        holder.localPosition = new Vector3(0.42f, -0.36f, 0.72f);
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;

        Transform oldSword = holder.Find(SwordName);

        if (oldSword != null)
            Undo.DestroyObjectImmediate(oldSword.gameObject);

        GameObject swordInstance =
            PrefabUtility.InstantiatePrefab(swordPrefab) as GameObject;

        if (swordInstance == null)
        {
            Debug.LogError("Could not instantiate the sword prefab.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(
            swordInstance,
            "Equip First-Person Sword"
        );

        Undo.SetTransformParent(
            swordInstance.transform,
            holder,
            "Parent First-Person Sword"
        );

        swordInstance.name = SwordName;
        swordInstance.transform.localPosition = Vector3.zero;
        swordInstance.transform.localRotation =
            Quaternion.Euler(70f, 0f, -18f);
        swordInstance.transform.localScale = Vector3.one;

        NormalizeObjectSize(swordInstance, 1.05f);

        foreach (Collider collider in
                 swordInstance.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (Rigidbody rigidbody in
                 swordInstance.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        FirstPersonSword swordController =
            holder.GetComponent<FirstPersonSword>();

        if (swordController == null)
        {
            swordController =
                Undo.AddComponent<FirstPersonSword>(holderObject);
        }

        Selection.activeGameObject = holderObject;
        EditorGUIUtility.PingObject(holderObject);
        MarkSceneDirty();

        Debug.Log(
            "First-person sword equipped. Controls: Left Click attack, Right Click block, E heavy attack."
        );
    }

    [MenuItem("Tools/Dungeon Game/Setup/Spawn 12 Ambient NPCs")]
    public static void SpawnAmbientNPCs()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Not Found",
                "No active Terrain was found in the scene.",
                "OK"
            );
            return;
        }

        foreach (string path in NpcPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"NPC prefab not found: {path}");
                return;
            }
        }

        GameObject existing = GameObject.Find(NpcRootName);

        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "NPC Group Already Exists",
                "Delete the existing generated NPCs and create a new random group?",
                "Replace",
                "Cancel"
            );

            if (!replace)
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(NpcRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Ambient NPC Group");

        Vector3 center = GetSpawnCenter(terrain);
        Transform player = FindPlayerTransform();
        Transform sanctuary = FindSanctuaryTransform();

        const int targetCount = 12;
        const float spawnRadius = 75f;
        const float minimumPlayerDistance = 12f;
        const float minimumSanctuaryDistance = 15f;
        const float maximumSlope = 30f;

        int spawned = 0;
        int attempts = 0;

        while (spawned < targetCount && attempts < 600)
        {
            attempts++;

            Vector2 circle = Random.insideUnitCircle * spawnRadius;

            Vector3 position = new Vector3(
                center.x + circle.x,
                0f,
                center.z + circle.y
            );

            if (!IsInsideTerrain(position, terrain))
                continue;

            position.y =
                terrain.SampleHeight(position) +
                terrain.transform.position.y;

            Vector3 normal = GetTerrainNormal(position, terrain);
            float slope = Vector3.Angle(normal, Vector3.up);

            if (slope > maximumSlope)
                continue;

            if (player != null &&
                Vector3.Distance(position, player.position) <
                minimumPlayerDistance)
            {
                continue;
            }

            if (sanctuary != null &&
                Vector3.Distance(position, sanctuary.position) <
                minimumSanctuaryDistance)
            {
                continue;
            }

            string prefabPath =
                NpcPaths[Random.Range(0, NpcPaths.Length)];

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            GameObject npc =
                PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (npc == null)
                continue;

            Undo.RegisterCreatedObjectUndo(
                npc,
                "Spawn Ambient NPC"
            );

            Undo.SetTransformParent(
                npc.transform,
                root.transform,
                "Parent Ambient NPC"
            );

            npc.name = $"AmbientNPC_{spawned + 1:00}_{prefab.name}";
            npc.transform.position = position;
            npc.transform.rotation =
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float randomScale = prefab.name.Contains("Boy")
                ? Random.Range(0.90f, 0.98f)
                : Random.Range(0.96f, 1.06f);

            npc.transform.localScale =
                Vector3.one * randomScale;

            GroundObjectBottom(npc, position.y);
            AddCapsuleColliderIfMissing(npc);

            if (npc.GetComponent<AmbientNPC>() == null)
                Undo.AddComponent<AmbientNPC>(npc);

            spawned++;
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        MarkSceneDirty();

        EditorUtility.DisplayDialog(
            "Ambient NPCs Created",
            $"Spawned {spawned} NPCs around the terrain.",
            "OK"
        );
    }

    [MenuItem("Tools/Dungeon Game/Setup/Delete Generated NPCs")]
    public static void DeleteGeneratedNPCs()
    {
        GameObject root = GameObject.Find(NpcRootName);

        if (root == null)
        {
            Debug.Log("No generated NPC group was found.");
            return;
        }

        Undo.DestroyObjectImmediate(root);
        MarkSceneDirty();
    }

    private static Camera FindPlayerCamera()
    {
        if (Selection.activeGameObject != null)
        {
            Camera selectedCamera =
                Selection.activeGameObject.GetComponentInChildren<Camera>(true);

            if (selectedCamera != null)
                return selectedCamera;
        }

        if (Camera.main != null)
            return Camera.main;

        Camera[] cameras = Object.FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static Transform FindPlayerTransform()
    {
        GameObject player = GameObject.Find("player");

        if (player == null)
            player = GameObject.Find("Player");

        if (player != null)
            return player.transform;

        Camera camera = FindPlayerCamera();

        return camera != null
            ? camera.transform.root
            : null;
    }

    private static Transform FindSanctuaryTransform()
    {
        GameObject sanctuary =
            GameObject.Find("RuneSanctuary_Generated");

        return sanctuary != null
            ? sanctuary.transform
            : null;
    }

    private static Vector3 GetSpawnCenter(Terrain terrain)
    {
        Transform player = FindPlayerTransform();

        if (player != null &&
            IsInsideTerrain(player.position, terrain))
        {
            return player.position;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        return terrainPosition +
               new Vector3(
                   terrainSize.x * 0.5f,
                   0f,
                   terrainSize.z * 0.5f
               );
    }

    private static bool IsInsideTerrain(
        Vector3 worldPosition,
        Terrain terrain
    )
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        return
            worldPosition.x >= terrainPosition.x &&
            worldPosition.x <= terrainPosition.x + terrainSize.x &&
            worldPosition.z >= terrainPosition.z &&
            worldPosition.z <= terrainPosition.z + terrainSize.z;
    }

    private static Vector3 GetTerrainNormal(
        Vector3 worldPosition,
        Terrain terrain
    )
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float normalizedX =
            Mathf.InverseLerp(
                terrainPosition.x,
                terrainPosition.x + terrainSize.x,
                worldPosition.x
            );

        float normalizedZ =
            Mathf.InverseLerp(
                terrainPosition.z,
                terrainPosition.z + terrainSize.z,
                worldPosition.z
            );

        return terrain.terrainData.GetInterpolatedNormal(
            normalizedX,
            normalizedZ
        );
    }

    private static void GroundObjectBottom(
        GameObject instance,
        float groundY
    )
    {
        if (!TryGetRendererBounds(instance, out Bounds bounds))
            return;

        float adjustment = groundY - bounds.min.y;
        instance.transform.position += Vector3.up * adjustment;
    }

    private static void AddCapsuleColliderIfMissing(
        GameObject instance
    )
    {
        if (instance.GetComponentInChildren<Collider>(true) != null)
            return;

        if (!TryGetRendererBounds(instance, out Bounds worldBounds))
            return;

        CapsuleCollider capsule =
            Undo.AddComponent<CapsuleCollider>(instance);

        capsule.direction = 1;
        capsule.center =
            instance.transform.InverseTransformPoint(worldBounds.center);

        Vector3 lossyScale = instance.transform.lossyScale;

        float localHeight =
            SafeDivide(
                worldBounds.size.y,
                Mathf.Abs(lossyScale.y)
            );

        float localRadius =
            0.45f *
            Mathf.Max(
                SafeDivide(
                    worldBounds.size.x,
                    Mathf.Abs(lossyScale.x)
                ),
                SafeDivide(
                    worldBounds.size.z,
                    Mathf.Abs(lossyScale.z)
                )
            );

        capsule.height = Mathf.Max(
            localHeight,
            localRadius * 2f
        );

        capsule.radius = localRadius;
    }

    private static void NormalizeObjectSize(
        GameObject instance,
        float targetLongestDimension
    )
    {
        if (!TryGetRendererBounds(instance, out Bounds bounds))
            return;

        float longestDimension = Mathf.Max(
            bounds.size.x,
            bounds.size.y,
            bounds.size.z
        );

        if (longestDimension <= 0.0001f)
            return;

        float scaleMultiplier =
            targetLongestDimension / longestDimension;

        instance.transform.localScale *= scaleMultiplier;
    }

    private static bool TryGetRendererBounds(
        GameObject instance,
        out Bounds combinedBounds
    )
    {
        Renderer[] renderers =
            instance.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            combinedBounds = default;
            return false;
        }

        combinedBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private static float SafeDivide(
        float value,
        float divisor
    )
    {
        return divisor > 0.0001f
            ? value / divisor
            : value;
    }

    private static void MarkSceneDirty()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        SceneView.RepaintAll();
    }
}
