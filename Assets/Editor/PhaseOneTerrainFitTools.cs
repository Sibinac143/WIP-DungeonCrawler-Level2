using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhaseOneTerrainFitTools
{
    private const string PhaseRootName = "PHASE_01_ACTUAL_ASSETS";
    private const string VillageRootName = "02_ABANDONED_VILLAGE";
    private const string CastleRootName = "03_RUINED_CASTLE_AND_PRISON";
    private const string BackupFolder = "Assets/Generated/TerrainBackups";
    private const string ReportPath =
        "Assets/Generated/PhaseOneTerrainFitReport.txt";

    private sealed class TerracePatch
    {
        public string name;
        public Bounds bounds;
        public float targetWorldY;
        public float innerMargin;
        public float blendDistance;
        public bool clearVegetation;
    }

    private sealed class FitStats
    {
        public int heightPatches;
        public int detailsRemoved;
        public int treesRemoved;
        public int villageObjectsGrounded;
        public int castleObjectsGrounded;
        public int npcsGrounded;
        public string backupPath;
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 1.5/Run Full Terrain Fit",
        priority = 100)]
    public static void RunFullTerrainFit()
    {
        if (!ValidateScene(out Terrain terrain, out GameObject phaseRoot))
            return;

        if (!EditorUtility.DisplayDialog(
                "Run Phase 1.5 Terrain Fit?",
                "This will modify the active TerrainData, create small " +
                "terraces under village buildings, fit castle foundations, " +
                "clear vegetation beneath structures, and re-ground NPCs.\n\n" +
                "A TerrainData backup will be created first.",
                "Run Terrain Fit",
                "Cancel"))
        {
            return;
        }

        FitStats stats = new FitStats();

        try
        {
            stats.backupPath = CreateTerrainBackupInternal(terrain);

            Undo.RegisterCompleteObjectUndo(
                terrain.terrainData,
                "Phase 1.5 Terrain Fit");

            Transform villageRoot =
                FindChildRecursive(
                    phaseRoot.transform,
                    VillageRootName);

            Transform castleRoot =
                FindChildRecursive(
                    phaseRoot.transform,
                    CastleRootName);

            List<TerracePatch> patches =
                new List<TerracePatch>();

            if (villageRoot != null)
                patches.AddRange(BuildVillageTerracePatches(villageRoot));

            if (castleRoot != null)
                patches.AddRange(BuildCastleFoundationPatches(castleRoot));

            ApplyHeightPatches(
                terrain,
                patches,
                stats);

            ClearVegetationUnderPatches(
                terrain,
                patches,
                stats);

            if (villageRoot != null)
            {
                stats.villageObjectsGrounded =
                    GroundVillageStructures(villageRoot, terrain);
            }

            if (castleRoot != null)
            {
                stats.castleObjectsGrounded =
                    GroundCastleArchitecture(castleRoot, terrain);
            }

            stats.npcsGrounded =
                GroundAllNPCs(terrain);

            terrain.Flush();

            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(terrain);

            EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());

            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WriteReport(terrain, patches, stats);

            EditorUtility.DisplayDialog(
                "Terrain Fit Complete",
                "Village terraces: " +
                patches.Count(p => p.name.StartsWith("Village")) +
                "\nCastle foundation patches: " +
                patches.Count(p => p.name.StartsWith("Castle")) +
                "\nNPCs grounded: " +
                stats.npcsGrounded +
                "\nTrees removed: " +
                stats.treesRemoved +
                "\nDetail cells cleared: " +
                stats.detailsRemoved +
                "\n\nBackup:\n" +
                stats.backupPath +
                "\n\nReport:\n" +
                ReportPath,
                "OK");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Terrain Fit Failed",
                "The terrain fitting operation stopped because of an error.\n\n" +
                exception.Message +
                "\n\nThe backup created before the operation can be restored " +
                "from the Phase 1.5 menu.",
                "OK");
        }
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 1.5/Create Terrain Backup",
        priority = 110)]
    public static void CreateTerrainBackup()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Not Found",
                "No active Terrain with TerrainData was found.",
                "OK");
            return;
        }

        string backupPath =
            CreateTerrainBackupInternal(terrain);

        EditorUtility.DisplayDialog(
            "Terrain Backup Created",
            backupPath,
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 1.5/Restore Latest Terrain Backup",
        priority = 111)]
    public static void RestoreLatestTerrainBackup()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Not Found",
                "No active Terrain with TerrainData was found.",
                "OK");
            return;
        }

        EnsureFolder(BackupFolder);

        string[] backupGuids =
            AssetDatabase.FindAssets(
                "t:TerrainData",
                new[] { BackupFolder });

        string latestPath =
            backupGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderByDescending(path =>
                    File.GetLastWriteTimeUtc(path))
                .FirstOrDefault();

        if (string.IsNullOrEmpty(latestPath))
        {
            EditorUtility.DisplayDialog(
                "No Backup Found",
                "No TerrainData backups were found in:\n" +
                BackupFolder,
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Restore Latest Terrain Backup?",
                "Restore:\n" +
                latestPath +
                "\n\nThis will replace the current TerrainData contents.",
                "Restore",
                "Cancel"))
        {
            return;
        }

        TerrainData backup =
            AssetDatabase.LoadAssetAtPath<TerrainData>(
                latestPath);

        if (backup == null)
        {
            EditorUtility.DisplayDialog(
                "Backup Could Not Be Read",
                latestPath,
                "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(
            terrain.terrainData,
            "Restore Terrain Backup");

        EditorUtility.CopySerialized(
            backup,
            terrain.terrainData);

        terrain.Flush();

        EditorUtility.SetDirty(terrain.terrainData);
        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Terrain Restored",
            "Restored from:\n" + latestPath,
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 1.5/Re-ground NPCs Only",
        priority = 120)]
    public static void RegroundNPCsOnly()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Not Found",
                "No active Terrain with TerrainData was found.",
                "OK");
            return;
        }

        int count = GroundAllNPCs(terrain);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog(
            "NPC Grounding Complete",
            "Grounded NPCs: " + count,
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Phase 1.5/Re-ground Village Buildings Only",
        priority = 121)]
    public static void RegroundVillageOnly()
    {
        GameObject phaseRoot =
            GameObject.Find(PhaseRootName);

        Terrain terrain = Terrain.activeTerrain;

        if (phaseRoot == null ||
            terrain == null ||
            terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Required Objects Missing",
                "The Phase 1 root or active Terrain was not found.",
                "OK");
            return;
        }

        Transform villageRoot =
            FindChildRecursive(
                phaseRoot.transform,
                VillageRootName);

        if (villageRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Village Not Found",
                VillageRootName + " was not found.",
                "OK");
            return;
        }

        int count =
            GroundVillageStructures(
                villageRoot,
                terrain);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog(
            "Village Grounding Complete",
            "Grounded village structures: " + count,
            "OK");
    }

    private static bool ValidateScene(
        out Terrain terrain,
        out GameObject phaseRoot)
    {
        terrain = Terrain.activeTerrain;
        phaseRoot = GameObject.Find(PhaseRootName);

        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before fitting the terrain.",
                "OK");
            return false;
        }

        if (terrain == null ||
            terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Terrain Not Found",
                "No active Terrain with TerrainData was found.",
                "OK");
            return false;
        }

        if (phaseRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Phase 1 World Not Found",
                PhaseRootName +
                " was not found. Build the Phase 1 world first.",
                "OK");
            return false;
        }

        return true;
    }

    private static List<TerracePatch>
        BuildVillageTerracePatches(
            Transform villageRoot)
    {
        List<TerracePatch> patches =
            new List<TerracePatch>();

        foreach (Transform child in villageRoot)
        {
            string lower =
                child.name.ToLowerInvariant();

            bool isMajorBuilding =
                lower.Contains("house") ||
                lower.Contains("mill") ||
                lower.Contains("gallows");

            if (!isMajorBuilding)
                continue;

            if (!TryGetRendererBounds(
                    child.gameObject,
                    out Bounds bounds))
            {
                continue;
            }

            float margin =
                lower.Contains("mill")
                    ? 5.5f
                    : 4.5f;

            patches.Add(new TerracePatch
            {
                name =
                    "Village Terrace - " +
                    child.name,
                bounds = bounds,
                targetWorldY = bounds.min.y,
                innerMargin = margin,
                blendDistance = 10f,
                clearVegetation = true
            });
        }

        Transform shopRoot =
            FindChildRecursive(
                villageRoot,
                "KEY_MAKER_BROKEN_SHOP");

        if (shopRoot != null)
        {
            foreach (Transform child in shopRoot)
            {
                string lower =
                    child.name.ToLowerInvariant();

                if (!lower.Contains("house"))
                    continue;

                if (!TryGetRendererBounds(
                        child.gameObject,
                        out Bounds bounds))
                {
                    continue;
                }

                patches.Add(new TerracePatch
                {
                    name =
                        "Village Terrace - " +
                        child.name,
                    bounds = bounds,
                    targetWorldY = bounds.min.y,
                    innerMargin = 5.5f,
                    blendDistance = 11f,
                    clearVegetation = true
                });
            }
        }

        return patches;
    }

    private static List<TerracePatch>
        BuildCastleFoundationPatches(
            Transform castleRoot)
    {
        List<TerracePatch> patches =
            new List<TerracePatch>();

        List<Transform> courtyardFloors =
            castleRoot
                .Cast<Transform>()
                .Where(child =>
                    child.name.StartsWith(
                        "Castle Floor",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (TryGetCombinedBounds(
                courtyardFloors,
                out Bounds courtyardBounds))
        {
            float floorY =
                courtyardFloors
                    .Select(GetLowestRendererY)
                    .Where(value =>
                        !float.IsNaN(value))
                    .DefaultIfEmpty(
                        courtyardBounds.min.y)
                    .Average();

            patches.Add(new TerracePatch
            {
                name =
                    "Castle Foundation - Main Courtyard",
                bounds = courtyardBounds,
                targetWorldY = floorY,
                innerMargin = 5f,
                blendDistance = 15f,
                clearVegetation = true
            });
        }

        AddCastleGroupPatch(
            castleRoot,
            "CASTLE_DUNGEON_CORRIDOR",
            "Castle Foundation - Dungeon Corridor",
            3.5f,
            10f,
            patches);

        AddCastleGroupPatch(
            castleRoot,
            "CASTLE_PRISON_BLOCK",
            "Castle Foundation - Prison Block",
            4.5f,
            12f,
            patches);

        AddCastleGroupPatch(
            castleRoot,
            "CASTLE_BOSS_ARENA",
            "Castle Foundation - Boss Arena",
            5f,
            14f,
            patches);

        return patches;
    }

    private static void AddCastleGroupPatch(
        Transform castleRoot,
        string groupName,
        string patchName,
        float innerMargin,
        float blendDistance,
        List<TerracePatch> patches)
    {
        Transform group =
            FindChildRecursive(
                castleRoot,
                groupName);

        if (group == null)
            return;

        List<Transform> floorObjects =
            group
                .GetComponentsInChildren<Transform>(true)
                .Where(transform =>
                    transform.name
                        .ToLowerInvariant()
                        .Contains("floor"))
                .ToList();

        Bounds bounds;
        float targetY;

        if (floorObjects.Count > 0 &&
            TryGetCombinedBounds(
                floorObjects,
                out bounds))
        {
            targetY =
                floorObjects
                    .Select(GetLowestRendererY)
                    .Where(value =>
                        !float.IsNaN(value))
                    .DefaultIfEmpty(bounds.min.y)
                    .Average();
        }
        else if (TryGetArchitectureBounds(
                     group.gameObject,
                     out bounds))
        {
            targetY = bounds.min.y;
        }
        else
        {
            return;
        }

        patches.Add(new TerracePatch
        {
            name = patchName,
            bounds = bounds,
            targetWorldY = targetY,
            innerMargin = innerMargin,
            blendDistance = blendDistance,
            clearVegetation = true
        });
    }

    private static void ApplyHeightPatches(
        Terrain terrain,
        List<TerracePatch> patches,
        FitStats stats)
    {
        TerrainData data = terrain.terrainData;
        int resolution = data.heightmapResolution;

        float[,] heights =
            data.GetHeights(
                0,
                0,
                resolution,
                resolution);

        Vector3 terrainOrigin =
            terrain.transform.position;

        Vector3 terrainSize =
            data.size;

        foreach (TerracePatch patch in patches)
        {
            ApplySingleHeightPatch(
                heights,
                resolution,
                terrainOrigin,
                terrainSize,
                patch);

            stats.heightPatches++;
        }

        data.SetHeightsDelayLOD(
            0,
            0,
            heights);

        data.SyncHeightmap();
    }

    private static void ApplySingleHeightPatch(
        float[,] heights,
        int resolution,
        Vector3 terrainOrigin,
        Vector3 terrainSize,
        TerracePatch patch)
    {
        float innerMinX =
            patch.bounds.min.x -
            patch.innerMargin;

        float innerMaxX =
            patch.bounds.max.x +
            patch.innerMargin;

        float innerMinZ =
            patch.bounds.min.z -
            patch.innerMargin;

        float innerMaxZ =
            patch.bounds.max.z +
            patch.innerMargin;

        float outerMinX =
            innerMinX -
            patch.blendDistance;

        float outerMaxX =
            innerMaxX +
            patch.blendDistance;

        float outerMinZ =
            innerMinZ -
            patch.blendDistance;

        float outerMaxZ =
            innerMaxZ +
            patch.blendDistance;

        int minHeightX =
            WorldToHeightIndex(
                outerMinX,
                terrainOrigin.x,
                terrainSize.x,
                resolution);

        int maxHeightX =
            WorldToHeightIndex(
                outerMaxX,
                terrainOrigin.x,
                terrainSize.x,
                resolution);

        int minHeightZ =
            WorldToHeightIndex(
                outerMinZ,
                terrainOrigin.z,
                terrainSize.z,
                resolution);

        int maxHeightZ =
            WorldToHeightIndex(
                outerMaxZ,
                terrainOrigin.z,
                terrainSize.z,
                resolution);

        float targetNormalizedHeight =
            Mathf.Clamp01(
                (patch.targetWorldY -
                 terrainOrigin.y) /
                Mathf.Max(0.001f, terrainSize.y));

        for (int z = minHeightZ;
             z <= maxHeightZ;
             z++)
        {
            float worldZ =
                HeightIndexToWorld(
                    z,
                    terrainOrigin.z,
                    terrainSize.z,
                    resolution);

            for (int x = minHeightX;
                 x <= maxHeightX;
                 x++)
            {
                float worldX =
                    HeightIndexToWorld(
                        x,
                        terrainOrigin.x,
                        terrainSize.x,
                        resolution);

                float distance =
                    DistanceOutsideRectangle(
                        worldX,
                        worldZ,
                        innerMinX,
                        innerMaxX,
                        innerMinZ,
                        innerMaxZ);

                if (distance >= patch.blendDistance)
                    continue;

                float blend =
                    distance <= 0f
                        ? 1f
                        : 1f -
                          SmoothStep01(
                              distance /
                              patch.blendDistance);

                heights[z, x] =
                    Mathf.Lerp(
                        heights[z, x],
                        targetNormalizedHeight,
                        blend);
            }
        }
    }

    private static void ClearVegetationUnderPatches(
        Terrain terrain,
        List<TerracePatch> patches,
        FitStats stats)
    {
        TerrainData data = terrain.terrainData;

        foreach (TerracePatch patch in patches)
        {
            if (!patch.clearVegetation)
                continue;

            ClearDetailsInPatch(
                terrain,
                patch,
                stats);
        }

        ClearTreesInPatches(
            terrain,
            patches,
            stats);
    }

    private static void ClearDetailsInPatch(
        Terrain terrain,
        TerracePatch patch,
        FitStats stats)
    {
        TerrainData data = terrain.terrainData;

        if (data.detailPrototypes == null ||
            data.detailPrototypes.Length == 0)
        {
            return;
        }

        Vector3 origin =
            terrain.transform.position;

        Vector3 size =
            data.size;

        float minX =
            patch.bounds.min.x -
            patch.innerMargin -
            1f;

        float maxX =
            patch.bounds.max.x +
            patch.innerMargin +
            1f;

        float minZ =
            patch.bounds.min.z -
            patch.innerMargin -
            1f;

        float maxZ =
            patch.bounds.max.z +
            patch.innerMargin +
            1f;

        int detailMinX =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    (minX - origin.x) /
                    size.x *
                    data.detailWidth),
                0,
                data.detailWidth - 1);

        int detailMaxX =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    (maxX - origin.x) /
                    size.x *
                    data.detailWidth),
                0,
                data.detailWidth - 1);

        int detailMinZ =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    (minZ - origin.z) /
                    size.z *
                    data.detailHeight),
                0,
                data.detailHeight - 1);

        int detailMaxZ =
            Mathf.Clamp(
                Mathf.CeilToInt(
                    (maxZ - origin.z) /
                    size.z *
                    data.detailHeight),
                0,
                data.detailHeight - 1);

        int width =
            detailMaxX -
            detailMinX +
            1;

        int height =
            detailMaxZ -
            detailMinZ +
            1;

        if (width <= 0 || height <= 0)
            return;

        int[,] empty =
            new int[height, width];

        for (int layer = 0;
             layer < data.detailPrototypes.Length;
             layer++)
        {
            data.SetDetailLayer(
                detailMinX,
                detailMinZ,
                layer,
                empty);

            stats.detailsRemoved +=
                width * height;
        }
    }

    private static void ClearTreesInPatches(
        Terrain terrain,
        List<TerracePatch> patches,
        FitStats stats)
    {
        TerrainData data = terrain.terrainData;

        if (data.treeInstances == null ||
            data.treeInstances.Length == 0)
        {
            return;
        }

        Vector3 origin =
            terrain.transform.position;

        Vector3 size =
            data.size;

        List<TreeInstance> kept =
            new List<TreeInstance>();

        foreach (TreeInstance tree in data.treeInstances)
        {
            Vector3 worldPosition =
                new Vector3(
                    origin.x +
                    tree.position.x *
                    size.x,
                    origin.y +
                    tree.position.y *
                    size.y,
                    origin.z +
                    tree.position.z *
                    size.z);

            bool insideAnyPatch =
                patches.Any(patch =>
                    IsPointInsideExpandedBoundsXZ(
                        worldPosition,
                        patch.bounds,
                        patch.innerMargin + 2f));

            if (insideAnyPatch)
                stats.treesRemoved++;
            else
                kept.Add(tree);
        }

        data.treeInstances =
            kept.ToArray();
    }

    private static int GroundVillageStructures(
        Transform villageRoot,
        Terrain terrain)
    {
        int count = 0;

        foreach (Transform child in villageRoot)
        {
            string lower =
                child.name.ToLowerInvariant();

            bool shouldGround =
                lower.Contains("house") ||
                lower.Contains("mill") ||
                lower.Contains("well") ||
                lower.Contains("gallows") ||
                lower.Contains("lamp") ||
                lower.Contains("barrel") ||
                lower.Contains("box") ||
                lower.Contains("wheelbarrow");

            if (!shouldGround)
                continue;

            if (GroundObjectByRendererBottom(
                    child.gameObject,
                    terrain))
            {
                count++;
            }
        }

        Transform shop =
            FindChildRecursive(
                villageRoot,
                "KEY_MAKER_BROKEN_SHOP");

        if (shop != null)
        {
            foreach (Transform child in shop)
            {
                if (GroundObjectByRendererBottom(
                        child.gameObject,
                        terrain))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int GroundCastleArchitecture(
        Transform castleRoot,
        Terrain terrain)
    {
        int count = 0;

        foreach (Transform child in
                 castleRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == castleRoot)
                continue;

            if (child.parent != castleRoot &&
                child.parent !=
                FindChildRecursive(
                    castleRoot,
                    "CASTLE_DUNGEON_CORRIDOR") &&
                child.parent !=
                FindChildRecursive(
                    castleRoot,
                    "CASTLE_PRISON_BLOCK") &&
                child.parent !=
                FindChildRecursive(
                    castleRoot,
                    "CASTLE_BOSS_ARENA"))
            {
                continue;
            }

            string lower =
                child.name.ToLowerInvariant();

            if (lower.Contains("kuzar") ||
                lower.Contains("guardian") ||
                lower.Contains("key maker") ||
                child.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
            {
                continue;
            }

            bool shouldGround =
                lower.Contains("floor") ||
                lower.Contains("wall") ||
                lower.Contains("gate") ||
                lower.Contains("door") ||
                lower.Contains("column") ||
                lower.Contains("stairs") ||
                lower.Contains("bench") ||
                lower.Contains("coffin") ||
                lower.Contains("chest") ||
                lower.Contains("candle") ||
                lower.Contains("skull");

            if (!shouldGround)
                continue;

            if (GroundObjectByRendererBottom(
                    child.gameObject,
                    terrain))
            {
                count++;
            }
        }

        return count;
    }

    private static int GroundAllNPCs(
        Terrain terrain)
    {
        int count = 0;

        Animator[] animators =
            UnityEngine.Object.FindObjectsByType<Animator>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Animator animator in animators)
        {
            if (animator == null)
                continue;

            GameObject root =
                FindPrefabInstanceRoot(
                    animator.gameObject);

            if (root == null)
                root = animator.gameObject;

            string combinedName =
                (root.name + " " +
                 animator.gameObject.name)
                    .ToLowerInvariant();

            bool isDragon =
                combinedName.Contains("kuzar") ||
                combinedName.Contains("dragon") ||
                combinedName.Contains("wyvern");

            if (isDragon)
                continue;

            bool looksLikeNpc =
                combinedName.Contains("peasant") ||
                combinedName.Contains("citizen") ||
                combinedName.Contains("npc") ||
                combinedName.Contains("key maker") ||
                IsUnderNamedRoot(
                    root.transform,
                    "RandomNPCs_Generated");

            if (!looksLikeNpc)
                continue;

            if (GroundObjectByRendererBottom(
                    root,
                    terrain))
            {
                count++;
            }
        }

        return count;
    }

    private static bool GroundObjectByRendererBottom(
        GameObject gameObject,
        Terrain terrain)
    {
        if (!TryGetRendererBounds(
                gameObject,
                out Bounds bounds))
        {
            return false;
        }

        Vector3 samplePoint =
            gameObject.transform.position;

        Vector3 terrainOrigin =
            terrain.transform.position;

        Vector3 terrainSize =
            terrain.terrainData.size;

        bool insideTerrain =
            samplePoint.x >= terrainOrigin.x &&
            samplePoint.x <=
                terrainOrigin.x + terrainSize.x &&
            samplePoint.z >= terrainOrigin.z &&
            samplePoint.z <=
                terrainOrigin.z + terrainSize.z;

        if (!insideTerrain)
            return false;

        float terrainY =
            terrain.SampleHeight(
                samplePoint) +
            terrainOrigin.y;

        float delta =
            terrainY -
            bounds.min.y;

        if (Mathf.Abs(delta) < 0.001f)
            return true;

        Undo.RecordObject(
            gameObject.transform,
            "Ground " + gameObject.name);

        gameObject.transform.position +=
            Vector3.up * delta;

        EditorUtility.SetDirty(
            gameObject.transform);

        return true;
    }

    private static string CreateTerrainBackupInternal(
        Terrain terrain)
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(BackupFolder);

        string sourcePath =
            AssetDatabase.GetAssetPath(
                terrain.terrainData);

        string timestamp =
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss");

        string safeName =
            SanitizeFileName(
                terrain.terrainData.name);

        string backupPath =
            AssetDatabase.GenerateUniqueAssetPath(
                BackupFolder +
                "/" +
                safeName +
                "_Backup_" +
                timestamp +
                ".asset");

        if (!string.IsNullOrEmpty(sourcePath) &&
            sourcePath.StartsWith("Assets/"))
        {
            bool copied =
                AssetDatabase.CopyAsset(
                    sourcePath,
                    backupPath);

            if (!copied)
            {
                TerrainData clone =
                    UnityEngine.Object.Instantiate(
                        terrain.terrainData);

                clone.name =
                    Path.GetFileNameWithoutExtension(
                        backupPath);

                AssetDatabase.CreateAsset(
                    clone,
                    backupPath);
            }
        }
        else
        {
            TerrainData clone =
                UnityEngine.Object.Instantiate(
                    terrain.terrainData);

            clone.name =
                Path.GetFileNameWithoutExtension(
                    backupPath);

            AssetDatabase.CreateAsset(
                clone,
                backupPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return backupPath;
    }

    private static void WriteReport(
        Terrain terrain,
        List<TerracePatch> patches,
        FitStats stats)
    {
        EnsureFolder("Assets/Generated");

        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            "PHASE 1.5 TERRAIN FIT REPORT");

        builder.AppendLine(
            "Scene: " +
            SceneManager.GetActiveScene().path);

        builder.AppendLine(
            "Terrain: " +
            terrain.name);

        builder.AppendLine(
            "Terrain size: " +
            terrain.terrainData.size);

        builder.AppendLine(
            "Backup: " +
            stats.backupPath);

        builder.AppendLine();

        builder.AppendLine(
            "PATCHES:");

        foreach (TerracePatch patch in patches)
        {
            builder.AppendLine(
                "- " +
                patch.name +
                " | bounds=" +
                patch.bounds +
                " | targetY=" +
                patch.targetWorldY.ToString(
                    "0.000",
                    CultureInfo.InvariantCulture) +
                " | innerMargin=" +
                patch.innerMargin.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture) +
                " | blend=" +
                patch.blendDistance.ToString(
                    "0.0",
                    CultureInfo.InvariantCulture));
        }

        builder.AppendLine();
        builder.AppendLine(
            "Height patches: " +
            stats.heightPatches);

        builder.AppendLine(
            "Village structures grounded: " +
            stats.villageObjectsGrounded);

        builder.AppendLine(
            "Castle objects grounded: " +
            stats.castleObjectsGrounded);

        builder.AppendLine(
            "NPCs grounded: " +
            stats.npcsGrounded);

        builder.AppendLine(
            "Trees removed: " +
            stats.treesRemoved);

        builder.AppendLine(
            "Detail cells cleared: " +
            stats.detailsRemoved);

        File.WriteAllText(
            ReportPath,
            builder.ToString());

        AssetDatabase.ImportAsset(
            ReportPath);
    }

    private static bool TryGetRendererBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer) &&
                    renderer.enabled)
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

    private static bool TryGetArchitectureBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    !(renderer is ParticleSystemRenderer) &&
                    !(renderer is SkinnedMeshRenderer) &&
                    renderer.enabled)
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

    private static bool TryGetCombinedBounds(
        IEnumerable<Transform> transforms,
        out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;

        foreach (Transform transform in transforms)
        {
            if (transform == null)
                continue;

            if (!TryGetRendererBounds(
                    transform.gameObject,
                    out Bounds childBounds))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = childBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(
                    childBounds);
            }
        }

        return hasBounds;
    }

    private static float GetLowestRendererY(
        Transform transform)
    {
        if (transform == null ||
            !TryGetRendererBounds(
                transform.gameObject,
                out Bounds bounds))
        {
            return float.NaN;
        }

        return bounds.min.y;
    }

    private static Transform FindChildRecursive(
        Transform root,
        string targetName)
    {
        if (root == null)
            return null;

        foreach (Transform child in
                 root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(
                    child.name,
                    targetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private static GameObject FindPrefabInstanceRoot(
        GameObject gameObject)
    {
        GameObject outermost =
            PrefabUtility.GetOutermostPrefabInstanceRoot(
                gameObject);

        return outermost != null
            ? outermost
            : gameObject;
    }

    private static bool IsUnderNamedRoot(
        Transform transform,
        string rootName)
    {
        Transform current = transform;

        while (current != null)
        {
            if (string.Equals(
                    current.name,
                    rootName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsPointInsideExpandedBoundsXZ(
        Vector3 point,
        Bounds bounds,
        float expansion)
    {
        return
            point.x >= bounds.min.x - expansion &&
            point.x <= bounds.max.x + expansion &&
            point.z >= bounds.min.z - expansion &&
            point.z <= bounds.max.z + expansion;
    }

    private static int WorldToHeightIndex(
        float worldCoordinate,
        float terrainOrigin,
        float terrainLength,
        int resolution)
    {
        float normalized =
            Mathf.Clamp01(
                (worldCoordinate -
                 terrainOrigin) /
                Mathf.Max(
                    0.001f,
                    terrainLength));

        return Mathf.Clamp(
            Mathf.RoundToInt(
                normalized *
                (resolution - 1)),
            0,
            resolution - 1);
    }

    private static float HeightIndexToWorld(
        int index,
        float terrainOrigin,
        float terrainLength,
        int resolution)
    {
        float normalized =
            index /
            Mathf.Max(
                1f,
                resolution - 1f);

        return
            terrainOrigin +
            normalized *
            terrainLength;
    }

    private static float DistanceOutsideRectangle(
        float x,
        float z,
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        float dx =
            Mathf.Max(
                Mathf.Max(
                    minX - x,
                    0f),
                x - maxX);

        float dz =
            Mathf.Max(
                Mathf.Max(
                    minZ - z,
                    0f),
                z - maxZ);

        return Mathf.Sqrt(
            dx * dx +
            dz * dz);
    }

    private static float SmoothStep01(
        float value)
    {
        value =
            Mathf.Clamp01(value);

        return
            value *
            value *
            (3f - 2f * value);
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Terrain";

        foreach (char invalid in
                 Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    invalid,
                    '_');
        }

        return value;
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
