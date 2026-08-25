using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhaseOneLayoutBuilder
{
    private const string RootName = "PHASE_01_STORY_LAYOUT";

    [MenuItem("Tools/Dungeon Game/Phase 1/Create Story Map Layout")]
    public static void CreateLayout()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog("Terrain required",
                "No active Terrain was found.", "OK");
            return;
        }

        Transform anchor = Selection.activeTransform;

        if (anchor == null)
        {
            GameObject player = GameObject.Find("player") ?? GameObject.Find("Player");
            anchor = player != null ? player.transform : null;
        }

        if (anchor == null)
        {
            EditorUtility.DisplayDialog("Select the player",
                "Select the player in the Hierarchy and run this again.", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);

        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Layout already exists",
                "Replace the existing Phase 1 layout?",
                "Replace", "Cancel");

            if (!replace)
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        Vector3 forward = Vector3.ProjectOnPlane(anchor.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Phase 1 Layout");
        root.transform.position = Ground(anchor.position, terrain);
        root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        Transform spawnGroup = Group("01_Player_Spawn", root.transform);
        Transform gateGroup = Group("02_Main_Gate_Area", root.transform);
        Transform villageGroup = Group("03_Village_Area", root.transform);
        Transform castleGroup = Group("04_Castle_Area", root.transform);
        Transform airGroup = Group("05_Flying_Creature_Area", root.transform);
        Transform endGroup = Group("06_Escape_Ending", root.transform);

        Transform playerSpawn = Marker("PlayerSpawn", spawnGroup, root.transform,
            new Vector3(0,0,0), LevelMarkerType.PlayerSpawn,
            new Color(0.15f,1f,0.25f), 2.2f,
            "The player awakens here.", terrain);

        Transform gateTrigger = Marker("GateApproachTrigger", gateGroup, root.transform,
            new Vector3(0,0,50), LevelMarkerType.Trigger,
            new Color(1f,0.45f,0.1f), 7f,
            "Without the key, this zone wakes the guardians.", terrain);

        Transform mainGate = Marker("MainGate", gateGroup, root.transform,
            new Vector3(0,0,72), LevelMarkerType.MainGate,
            new Color(1f,0.15f,0.15f), 6f,
            "The locked exit from the Realm of Death.", terrain);

        Marker("GuardianSpawn_Left", gateGroup, root.transform,
            new Vector3(-12,0,63), LevelMarkerType.GuardianSpawn,
            new Color(0.9f,0.1f,0.1f), 3f,
            "Left dragon-like guardian.", terrain);

        Marker("GuardianSpawn_Right", gateGroup, root.transform,
            new Vector3(12,0,63), LevelMarkerType.GuardianSpawn,
            new Color(0.9f,0.1f,0.1f), 3f,
            "Right dragon-like guardian.", terrain);

        Transform village = Marker("VillageCenter", villageGroup, root.transform,
            new Vector3(-52,0,24), LevelMarkerType.VillageCenter,
            new Color(1f,0.85f,0.1f), 6f,
            "Center of the ruined village.", terrain);

        Transform shop = Marker("KeyMakerShop", villageGroup, root.transform,
            new Vector3(-63,0,33), LevelMarkerType.KeyMakerShop,
            new Color(1f,0.45f,0.1f), 5f,
            "Broken shop of the old key maker.", terrain);

        Marker("KeyBoxPoint", villageGroup, root.transform,
            new Vector3(-61,0,35), LevelMarkerType.KeyBox,
            new Color(0.95f,0.7f,0.15f), 1f,
            "Empty box that once held the gate key.", terrain);

        Marker("LetterPoint", villageGroup, root.transform,
            new Vector3(-65,0,34), LevelMarkerType.Letter,
            new Color(1f,1f,0.8f), 0.8f,
            "Warning letter revealing the castle clue.", terrain);

        Transform castle = Marker("CastleEntrance", castleGroup, root.transform,
            new Vector3(-96,0,80), LevelMarkerType.CastleEntrance,
            new Color(0.65f,0.18f,1f), 7f,
            "Entrance to the prison castle.", terrain);

        Transform prison = Marker("PrisonCell", castleGroup, root.transform,
            new Vector3(-104,0,91), LevelMarkerType.PrisonCell,
            new Color(0.65f,0.35f,1f), 4f,
            "The old key maker is imprisoned here.", terrain);

        Marker("KeyMakerNPCPoint", castleGroup, root.transform,
            new Vector3(-102,0,91), LevelMarkerType.KeyMakerNPC,
            new Color(0.3f,0.75f,1f), 1.2f,
            "Spawn point for the imprisoned key maker.", terrain);

        Transform boss = Marker("CastleGuardianBossArena", castleGroup, root.transform,
            new Vector3(-86,0,101), LevelMarkerType.BossArena,
            new Color(1f,0.15f,0.75f), 10f,
            "The flying castle guardian lands here.", terrain);

        Transform escape = Marker("EscapePoint", endGroup, root.transform,
            new Vector3(0,0,91), LevelMarkerType.EscapePoint,
            new Color(0.2f,1f,0.65f), 4f,
            "Final escape point behind the opened gate.", terrain);

        AirMarker("AirPatrolPoint_01", airGroup, root.transform, new Vector3(-25,27,12), terrain);
        AirMarker("AirPatrolPoint_02", airGroup, root.transform, new Vector3(20,34,38), terrain);
        AirMarker("AirPatrolPoint_03", airGroup, root.transform, new Vector3(-62,29,46), terrain);
        AirMarker("AirPatrolPoint_04", airGroup, root.transform, new Vector3(-88,39,75), terrain);
        AirMarker("AirPatrolPoint_05", airGroup, root.transform, new Vector3(-45,33,104), terrain);

        StoryRouteGizmos routes = Undo.AddComponent<StoryRouteGizmos>(root);
        routes.discoveryRoute = new[]
        {
            playerSpawn, gateTrigger, village, shop, castle, prison, boss
        };
        routes.rescueRoute = new[] { boss, prison, shop };
        routes.escapeRoute = new[] { shop, mainGate, escape };

        Selection.activeGameObject = root;

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        MarkDirty();

        EditorUtility.DisplayDialog(
            "Phase 1 layout created",
            "Turn on Gizmos in the Scene view. Move the parent or individual markers until the routes feel right.",
            "OK");
    }

    [MenuItem("Tools/Dungeon Game/Phase 1/Delete Story Map Layout")]
    public static void DeleteLayout()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
            return;

        Undo.DestroyObjectImmediate(root);
        MarkDirty();
    }

    private static Transform Group(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, $"Create {name}");
        group.transform.SetParent(parent);
        group.transform.localPosition = Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        return group.transform;
    }

    private static Transform Marker(
        string name,
        Transform parent,
        Transform layoutRoot,
        Vector3 localOffset,
        LevelMarkerType type,
        Color color,
        float size,
        string description,
        Terrain terrain)
    {
        GameObject markerObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(markerObject, $"Create {name}");
        markerObject.transform.SetParent(parent);

        Vector3 world = layoutRoot.TransformPoint(localOffset);
        markerObject.transform.position = Ground(world, terrain);
        markerObject.transform.rotation = layoutRoot.rotation;

        LevelMarker marker = Undo.AddComponent<LevelMarker>(markerObject);
        marker.markerType = type;
        marker.markerColor = color;
        marker.markerSize = size;
        marker.description = description;

        return markerObject.transform;
    }

    private static void AirMarker(
        string name,
        Transform parent,
        Transform layoutRoot,
        Vector3 localOffset,
        Terrain terrain)
    {
        Vector3 groundLocal = new Vector3(localOffset.x, 0f, localOffset.z);
        Vector3 world = Ground(layoutRoot.TransformPoint(groundLocal), terrain);

        GameObject markerObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(markerObject, $"Create {name}");
        markerObject.transform.SetParent(parent);
        markerObject.transform.position = world + Vector3.up * localOffset.y;

        LevelMarker marker = Undo.AddComponent<LevelMarker>(markerObject);
        marker.markerType = LevelMarkerType.AirPatrol;
        marker.markerColor = new Color(0.15f,0.85f,1f);
        marker.markerSize = 2f;
        marker.description = "Flying-creature patrol waypoint.";
    }

    private static Vector3 Ground(Vector3 point, Terrain terrain)
    {
        Vector3 tPos = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        point.x = Mathf.Clamp(point.x, tPos.x + 2f, tPos.x + size.x - 2f);
        point.z = Mathf.Clamp(point.z, tPos.z + 2f, tPos.z + size.z - 2f);
        point.y = terrain.SampleHeight(point) + tPos.y;

        return point;
    }

    private static void MarkDirty()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);

        SceneView.RepaintAll();
    }
}
