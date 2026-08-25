using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RuneSanctuaryBuilder
{
    private const string RootName = "RuneSanctuary_Generated";

    private const string MenhirPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab";
    private const string GenericRockPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab";
    private const string OreRockPath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab";
    private const string SplitOrePath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01_split.prefab";
    private const string RockPilePath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab";
    private const string DeadFruitTreePath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_dead.prefab";
    private const string DeadPineTreePath =
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead.prefab";
    private const string BridgePath =
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab";
    private const string GatePath =
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Gate_Wood_01.prefab";
    private const string Fence01Path =
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab";
    private const string Fence02Path =
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab";
    private const string Fence03Path =
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_03.prefab";

    [MenuItem("Tools/Dungeon Game/Generate Rune Sanctuary")]
    public static void Generate()
    {
        Transform anchor = Selection.activeTransform;

        if (anchor == null)
        {
            EditorUtility.DisplayDialog(
                "Select a placement anchor",
                "Create an empty GameObject where you want the sanctuary entrance, rotate it to face into the sanctuary, select it, and run this command again.",
                "OK"
            );
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Rune Sanctuary Already Exists",
                "Replace the existing generated sanctuary?",
                "Replace",
                "Cancel"
            );

            if (!replace)
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        if (!ValidateRequiredAssets())
            return;

        Vector3 flatForward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.forward;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Rune Sanctuary");

        root.transform.position = SnapPointToTerrain(anchor.position, 0f);
        root.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        Transform entrance = CreateGroup("01_Entrance", root.transform);
        Transform avenue = CreateGroup("02_Rune_Avenue", root.transform);
        Transform arena = CreateGroup("03_Ritual_Arena", root.transform);
        Transform border = CreateGroup("04_Cursed_Border", root.transform);
        Transform markers = CreateGroup("05_Gameplay_Markers", root.transform);

        BuildEntrance(entrance);
        BuildRuneAvenue(avenue);
        BuildRitualArena(arena);
        BuildCursedBorder(border);
        BuildMarkers(markers);

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        EditorSceneManager.MarkSceneDirty(root.scene);

        Debug.Log(
            "Rune Sanctuary generated from the project's existing Polytope prefabs. " +
            "Move or rotate RuneSanctuary_Generated to adjust the complete layout."
        );
    }

    [MenuItem("Tools/Dungeon Game/Delete Rune Sanctuary")]
    public static void DeleteGenerated()
    {
        GameObject existing = GameObject.Find(RootName);

        if (existing == null)
        {
            Debug.Log("No generated Rune Sanctuary was found.");
            return;
        }

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
    }

    private static void BuildEntrance(Transform parent)
    {
        // The bridge's longest audited dimension is X, so rotate it 90 degrees
        // to run along the sanctuary's forward Z direction.
        PlacePrefab(
            BridgePath,
            "Entrance_Bridge",
            parent,
            new Vector3(0f, 0.12f, 3.2f),
            new Vector3(0f, 90f, 0f),
            Vector3.one,
            false
        );

        PlacePrefab(
            GatePath,
            "Sanctuary_Gate",
            parent,
            new Vector3(0f, 0f, 8f),
            Vector3.zero,
            new Vector3(1.15f, 1.15f, 1.15f),
            false
        );

        string[] fencePaths = { Fence01Path, Fence02Path, Fence03Path };

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 4; i++)
            {
                PlacePrefab(
                    fencePaths[i % fencePaths.Length],
                    $"Fence_{(side < 0 ? "L" : "R")}_{i + 1}",
                    parent,
                    new Vector3(side * 3.8f, 0f, 10.5f + i * 2.15f),
                    new Vector3(0f, 90f, 0f),
                    Vector3.one,
                    false
                );
            }
        }

        PlacePrefab(
            DeadFruitTreePath,
            "Entrance_DeadTree_Left",
            parent,
            new Vector3(-6.3f, 0f, 7.5f),
            new Vector3(0f, 25f, 0f),
            new Vector3(1.05f, 1.05f, 1.05f),
            true
        );

        PlacePrefab(
            DeadFruitTreePath,
            "Entrance_DeadTree_Right",
            parent,
            new Vector3(6.3f, 0f, 7.5f),
            new Vector3(0f, -35f, 0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            true
        );
    }

    private static void BuildRuneAvenue(Transform parent)
    {
        float[] zPositions = { 15f, 21f, 27f };

        for (int i = 0; i < zPositions.Length; i++)
        {
            float z = zPositions[i];
            float scale = 1.45f + i * 0.08f;

            PlacePrefab(
                MenhirPath,
                $"Avenue_Menhir_L_{i + 1}",
                parent,
                new Vector3(-4.5f, 0f, z),
                new Vector3(0f, 10f + i * 9f, -3f),
                new Vector3(scale, scale, scale),
                true
            );

            PlacePrefab(
                MenhirPath,
                $"Avenue_Menhir_R_{i + 1}",
                parent,
                new Vector3(4.5f, 0f, z),
                new Vector3(0f, -10f - i * 9f, 3f),
                new Vector3(scale, scale, scale),
                true
            );

            PlacePrefab(
                OreRockPath,
                $"Avenue_Ore_L_{i + 1}",
                parent,
                new Vector3(-2.3f, 0f, z + 1.4f),
                new Vector3(0f, i * 35f, 0f),
                new Vector3(1.4f, 1.4f, 1.4f),
                true
            );

            PlacePrefab(
                OreRockPath,
                $"Avenue_Ore_R_{i + 1}",
                parent,
                new Vector3(2.3f, 0f, z + 1.4f),
                new Vector3(0f, 180f - i * 30f, 0f),
                new Vector3(1.4f, 1.4f, 1.4f),
                true
            );
        }

        for (int i = 0; i < 8; i++)
        {
            float z = 13f + i * 2.7f;
            float x = (i % 2 == 0 ? -1f : 1f) * (2.2f + (i % 3) * 0.45f);

            PlacePrefab(
                RockPilePath,
                $"Path_RockPile_{i + 1}",
                parent,
                new Vector3(x, 0f, z),
                new Vector3(0f, i * 41f, 0f),
                new Vector3(1.2f, 1.2f, 1.2f),
                true
            );
        }
    }

    private static void BuildRitualArena(Transform parent)
    {
        const float centerZ = 42f;
        const float radius = 10f;
        const int menhirCount = 12;

        for (int i = 0; i < menhirCount; i++)
        {
            float angle = i * 360f / menhirCount;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 localPosition = new Vector3(
                Mathf.Sin(radians) * radius,
                0f,
                centerZ + Mathf.Cos(radians) * radius
            );

            PlacePrefab(
                MenhirPath,
                $"Arena_Menhir_{i + 1:00}",
                parent,
                localPosition,
                new Vector3(0f, angle + 180f, i % 2 == 0 ? -4f : 4f),
                new Vector3(1.55f, 1.75f, 1.55f),
                true
            );
        }

        PlacePrefab(
            SplitOrePath,
            "Central_Rune_Altar",
            parent,
            new Vector3(0f, 0.08f, centerZ),
            new Vector3(0f, 20f, 0f),
            new Vector3(4.8f, 2.2f, 4.8f),
            true
        );

        for (int i = 0; i < 8; i++)
        {
            float angle = 22.5f + i * 45f;
            float radians = angle * Mathf.Deg2Rad;

            PlacePrefab(
                OreRockPath,
                $"Central_Ore_{i + 1}",
                parent,
                new Vector3(
                    Mathf.Sin(radians) * 5.2f,
                    0f,
                    centerZ + Mathf.Cos(radians) * 5.2f
                ),
                new Vector3(0f, angle * 1.7f, 0f),
                new Vector3(1.8f, 1.8f, 1.8f),
                true
            );
        }

        for (int i = 0; i < 16; i++)
        {
            float angle = i * 360f / 16f;
            float radians = angle * Mathf.Deg2Rad;
            float rockRadius = 12.7f + (i % 2) * 0.8f;

            PlacePrefab(
                i % 3 == 0 ? GenericRockPath : RockPilePath,
                $"Arena_Border_Rock_{i + 1:00}",
                parent,
                new Vector3(
                    Mathf.Sin(radians) * rockRadius,
                    0f,
                    centerZ + Mathf.Cos(radians) * rockRadius
                ),
                new Vector3(0f, angle + i * 11f, 0f),
                i % 3 == 0
                    ? new Vector3(5f, 4f, 5f)
                    : new Vector3(1.5f, 1.5f, 1.5f),
                true
            );
        }

        CreateBlueLight(
            "Sanctuary_Core_Light",
            parent,
            new Vector3(0f, 2.3f, centerZ),
            18f,
            4.5f
        );

        for (int i = 0; i < 4; i++)
        {
            float angle = 45f + i * 90f;
            float radians = angle * Mathf.Deg2Rad;

            CreateBlueLight(
                $"Rune_Light_{i + 1}",
                parent,
                new Vector3(
                    Mathf.Sin(radians) * 6.5f,
                    1.2f,
                    centerZ + Mathf.Cos(radians) * 6.5f
                ),
                8f,
                2f
            );
        }
    }

    private static void BuildCursedBorder(Transform parent)
    {
        const float centerZ = 42f;

        for (int i = 0; i < 14; i++)
        {
            float angle = i * 360f / 14f;
            float radians = angle * Mathf.Deg2Rad;
            float radius = 17f + (i % 3) * 1.8f;

            string treePath = i % 3 == 0 ? DeadPineTreePath : DeadFruitTreePath;
            float scale = i % 3 == 0 ? 0.95f : 0.9f + (i % 2) * 0.15f;

            PlacePrefab(
                treePath,
                $"Cursed_Tree_{i + 1:00}",
                parent,
                new Vector3(
                    Mathf.Sin(radians) * radius,
                    0f,
                    centerZ + Mathf.Cos(radians) * radius
                ),
                new Vector3(0f, angle + 130f + i * 17f, 0f),
                new Vector3(scale, scale, scale),
                true
            );
        }
    }

    private static void BuildMarkers(Transform parent)
    {
        CreateMarker("Player_Approach_Point", parent, new Vector3(0f, 0f, 10f));
        CreateMarker("Combat_Encounter_01", parent, new Vector3(0f, 0f, 24f));
        CreateMarker("Boss_Spawn_Point", parent, new Vector3(0f, 0f, 42f));
        CreateMarker("Boss_Arena_Camera_Point", parent, new Vector3(0f, 1.7f, 29f));
    }

    private static void CreateMarker(string name, Transform parent, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(marker, $"Create {name}");
        Undo.SetTransformParent(marker.transform, parent, $"Parent {name}");

        marker.transform.position = SnapPointToTerrain(
            parent.TransformPoint(localPosition),
            localPosition.y
        );
        marker.transform.rotation = parent.rotation;
    }

    private static void CreateBlueLight(
        string name,
        Transform parent,
        Vector3 localPosition,
        float range,
        float intensity
    )
    {
        GameObject lightObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(lightObject, $"Create {name}");
        Undo.SetTransformParent(lightObject.transform, parent, $"Parent {name}");

        lightObject.transform.position = SnapPointToTerrain(
            parent.TransformPoint(localPosition),
            localPosition.y
        );

        Light light = Undo.AddComponent<Light>(lightObject);
        light.type = LightType.Point;
        light.color = new Color(0.12f, 0.55f, 1f);
        light.range = range;
        light.intensity = intensity;
        light.shadows = LightShadows.Soft;
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, $"Create {name}");
        Undo.SetTransformParent(group.transform, parent, $"Parent {name}");

        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;

        return group.transform;
    }

    private static GameObject PlacePrefab(
        string path,
        string instanceName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localEulerAngles,
        Vector3 localScale,
        bool addColliderIfMissing
    )
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError($"Missing required prefab: {path}");
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError($"Could not instantiate prefab: {path}");
            return null;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Create {instanceName}");
        Undo.SetTransformParent(instance.transform, parent, $"Parent {instanceName}");

        instance.name = instanceName;
        instance.transform.position = SnapPointToTerrain(
            parent.TransformPoint(localPosition),
            localPosition.y
        );
        instance.transform.rotation =
            parent.rotation * Quaternion.Euler(localEulerAngles);
        instance.transform.localScale = localScale;

        if (addColliderIfMissing &&
            instance.GetComponentInChildren<Collider>(true) == null)
        {
            AddBoundsBoxCollider(instance);
        }

        return instance;
    }

    private static void AddBoundsBoxCollider(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds worldBounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);

        BoxCollider box = Undo.AddComponent<BoxCollider>(instance);
        box.center = instance.transform.InverseTransformPoint(worldBounds.center);

        Vector3 scale = instance.transform.lossyScale;
        box.size = new Vector3(
            SafeDivide(worldBounds.size.x, Mathf.Abs(scale.x)),
            SafeDivide(worldBounds.size.y, Mathf.Abs(scale.y)),
            SafeDivide(worldBounds.size.z, Mathf.Abs(scale.z))
        );
    }

    private static float SafeDivide(float value, float divisor)
    {
        return divisor > 0.0001f ? value / divisor : value;
    }

    private static Vector3 SnapPointToTerrain(Vector3 worldPoint, float verticalOffset)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
            return worldPoint + Vector3.up * verticalOffset;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        bool inside =
            worldPoint.x >= terrainPosition.x &&
            worldPoint.x <= terrainPosition.x + terrainSize.x &&
            worldPoint.z >= terrainPosition.z &&
            worldPoint.z <= terrainPosition.z + terrainSize.z;

        if (!inside)
            return worldPoint + Vector3.up * verticalOffset;

        worldPoint.y =
            terrain.SampleHeight(worldPoint) +
            terrainPosition.y +
            verticalOffset;

        return worldPoint;
    }

    private static bool ValidateRequiredAssets()
    {
        string[] requiredPaths =
        {
            MenhirPath,
            GenericRockPath,
            OreRockPath,
            SplitOrePath,
            RockPilePath,
            DeadFruitTreePath,
            DeadPineTreePath,
            BridgePath,
            GatePath,
            Fence01Path,
            Fence02Path,
            Fence03Path
        };

        bool allFound = true;

        foreach (string path in requiredPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                continue;

            Debug.LogError($"Required prefab was not found: {path}");
            allFound = false;
        }

        if (!allFound)
        {
            EditorUtility.DisplayDialog(
                "Missing Assets",
                "One or more required Polytope prefabs are missing. Check the Console.",
                "OK"
            );
        }

        return allFound;
    }
}
