using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PhaseOneActualWorldBuilder
{
    private const string RootName = "PHASE_01_ACTUAL_ASSETS";
    private const string ReportPath = "Assets/Generated/PhaseOneActualWorldReport.txt";

    private static readonly string VillageHouse =
        "Assets/Aletheia/Prefabs/house.prefab";
    private static readonly string VillageMill =
        "Assets/Aletheia/Prefabs/mill.prefab";
    private static readonly string VillageWell =
        "Assets/Aletheia/Prefabs/well.prefab";
    private static readonly string VillageGallows =
        "Assets/Aletheia/Prefabs/gallows.prefab";
    private static readonly string VillageFence =
        "Assets/Aletheia/Prefabs/fence.prefab";
    private static readonly string VillageLamp =
        "Assets/Aletheia/Prefabs/lamp.prefab";
    private static readonly string VillageBarrel =
        "Assets/Aletheia/Prefabs/barrel.prefab";
    private static readonly string VillageBox =
        "Assets/Aletheia/Prefabs/box.prefab";
    private static readonly string VillageWheelbarrow =
        "Assets/Aletheia/Prefabs/wheelbarrow.prefab";

    private static readonly string Forge =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Forge.prefab";
    private static readonly string Anvil =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Anvil.prefab";
    private static readonly string Bellows =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Bellows.prefab";
    private static readonly string WorkTable =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Work_Table.prefab";
    private static readonly string ToolRack =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Tool_Rack.prefab";
    private static readonly string Grindstone =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Grindstone.prefab";
    private static readonly string QuenchingTrough =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Quenching_Trough.prefab";
    private static readonly string BlacksmithSign =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Blacksmith_Sign.prefab";
    private static readonly string OreCrate =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Ore_Crate.prefab";
    private static readonly string CoalPile =
        "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Coal_Pile.prefab";

    private static readonly string GateArch =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_archPointy_decorated_gate.prefab";
    private static readonly string GateDoorLeft =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_gateDoor_left_LOW.prefab";
    private static readonly string GateDoorRight =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_gateDoor_right_LOW.prefab";
    private static readonly string StoneWall =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_bricks.prefab";
    private static readonly string BrokenWall =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_bricksBroken.prefab";
    private static readonly string StoneFloor =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/floor_tiles.prefab";
    private static readonly string StoneStairs =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_stairs.prefab";
    private static readonly string MetalDoorLeft =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_metalDoor_left_LOW.prefab";
    private static readonly string MetalDoorRight =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_metalDoor_right_LOW.prefab";
    private static readonly string Chain =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chain_hang_A.prefab";
    private static readonly string SkullColumn =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_columnSkull_large.prefab";
    private static readonly string ClosedChest =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chest_close.prefab";
    private static readonly string OpenChest =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chestOpen_body.prefab";
    private static readonly string Scroll =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_scroll.prefab";
    private static readonly string CandleGroup =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_candleGroup_A.prefab";

    private static readonly string BrokenBench =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_bench_broken.prefab";
    private static readonly string Skull =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_skull.prefab";
    private static readonly string BrokenFloor =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/floor_tiles_broken.prefab";
    private static readonly string RoundArch =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_archRound.prefab";
    private static readonly string Coffin =
        "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_coffin_A.prefab";

    private static readonly string Key =
        "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_09_Key_002.prefab";

    private static readonly string MalePeasant =
        "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Male_Peasant_01.prefab";

    private static readonly string KuzarRealistic =
        "Assets/Malbers Animations/Dragons/8 - Kuzar the Magnificent/Model/Kuzar the Magnificent.prefab";

    private static readonly List<string> Report = new List<string>();

    [MenuItem("Tools/Dungeon Game/Phase 1/Build Actual Story World")]
    public static void BuildActualWorld()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building the world.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        DeleteExistingRoot(false);

        Report.Clear();
        Report.Add("PHASE 1 ACTUAL WORLD BUILD REPORT");
        Report.Add("Scene: " + SceneManager.GetActiveScene().path);
        Report.Add("Built: " + DateTime.Now);
        Report.Add("");

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Phase 1 Actual World");

        Transform player = FindPlayer();
        Vector3 anchor = player != null ? player.position : Vector3.zero;
        Vector3 forward = player != null
            ? Flatten(player.forward).normalized
            : Vector3.forward;

        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.forward;

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // Terrain-aware Version 5 layout.
        // The script searches broad terrain regions instead of using one
        // exact coordinate:
        // - Gate: northern pass/ridge
        // - Village: high but reasonably flat eastern mountain plateau
        // - Castle: large, low, flat western field
        Vector3 gatePosition = FindBestTerrainLocation(
            "MAIN GATE",
            0.35f,
            0.65f,
            0.80f,
            0.96f,
            28f,
            1f,
            0.55f,
            0.28f,
            0.17f);

        Vector3 villagePosition = FindBestTerrainLocation(
            "MOUNTAIN VILLAGE",
            0.58f,
            0.92f,
            0.14f,
            0.62f,
            30f,
            1f,
            0.50f,
            0.22f,
            0.28f);

        Vector3 castlePosition = FindBestTerrainLocation(
            "FLATLAND CASTLE",
            0.06f,
            0.42f,
            0.08f,
            0.44f,
            52f,
            -1f,
            0.68f,
            0.24f,
            0.08f);

        Vector3 gateForward = Vector3.back;
        Vector3 gateRight =
            Vector3.Cross(Vector3.up, gateForward).normalized;

        Vector3 villageForward = Vector3.forward;
        Vector3 villageRight =
            Vector3.Cross(Vector3.up, villageForward).normalized;

        Vector3 castleForward = Vector3.forward;
        Vector3 castleRight =
            Vector3.Cross(Vector3.up, castleForward).normalized;

        Vector3 shopPosition = RoutePosition(
            villagePosition -
            villageRight * 22f +
            villageForward * 15f);

        Vector3 prisonPosition = RoutePosition(
            castlePosition -
            castleRight * 12f +
            castleForward * 36f);

        Vector3 bossPosition = RoutePosition(
            castlePosition +
            castleRight * 24f +
            castleForward * 60f);

        Vector3 keyMakerNpcPosition = RoutePosition(
            prisonPosition +
            castleRight * 2f +
            castleForward * 2f);

        Vector3 guardianOne = RoutePosition(
            gatePosition -
            gateRight * 18f -
            gateForward * 13f);

        Vector3 guardianTwo = RoutePosition(
            gatePosition +
            gateRight * 18f -
            gateForward * 13f);

        Report.Add("ROUTE DISTANCES:");
        Report.Add("Spawn -> Gate: " + Vector3.Distance(anchor, gatePosition).ToString("0.0") + "m");
        Report.Add("Gate -> Village: " + Vector3.Distance(gatePosition, villagePosition).ToString("0.0") + "m");
        Report.Add("Village -> Castle: " + Vector3.Distance(villagePosition, castlePosition).ToString("0.0") + "m");
        Report.Add("");

        BuildMainGate(
            root.transform,
            gatePosition,
            gateForward,
            gateRight,
            guardianOne,
            guardianTwo);

        BuildVillage(
            root.transform,
            villagePosition,
            shopPosition,
            villageForward,
            villageRight);

        BuildCastle(
            root.transform,
            castlePosition,
            prisonPosition,
            bossPosition,
            keyMakerNpcPosition,
            castleForward,
            castleRight);

        EnsureFolder("Assets/Generated");
        File.WriteAllLines(ReportPath, Report);
        AssetDatabase.ImportAsset(ReportPath);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Bounds bounds;
        if (TryGetBounds(root, out bounds) && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(bounds, false);

        EditorUtility.DisplayDialog(
            "Phase 1 World Built",
            "Created " + RootName +
            "\n\nThe scene was saved." +
            "\nReport: " + ReportPath +
            "\n\nThe village is selected from a high eastern mountain plateau, while the castle is selected from a large low western flatland patch. The dungeon includes a corridor, arches, multiple cells, props, and a dedicated story cell.",
            "OK");
    }

    [MenuItem("Tools/Dungeon Game/Phase 1/Delete Actual Story World")]
    public static void DeleteActualWorld()
    {
        DeleteExistingRoot(true);
    }

    private static void BuildMainGate(
        Transform parent,
        Vector3 gatePosition,
        Vector3 forward,
        Vector3 right,
        Vector3 guardianOne,
        Vector3 guardianTwo)
    {
        Transform area = Group("01_MAIN_ESCAPE_GATE", parent);
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

        Spawn(
            "Main Gate Arch",
            GateArch,
            gatePosition,
            rotation,
            Vector3.one * 1.5f,
            area,
            true,
            false);

        Spawn(
            "Main Gate Door Left",
            GateDoorLeft,
            gatePosition - right * 1.25f,
            rotation,
            Vector3.one * 1.5f,
            area,
            true,
            false);

        Spawn(
            "Main Gate Door Right",
            GateDoorRight,
            gatePosition + right * 1.25f,
            rotation,
            Vector3.one * 1.5f,
            area,
            true,
            false);

        for (int i = 1; i <= 3; i++)
        {
            Spawn(
                "Gate Wall Left " + i,
                i == 3 ? BrokenWall : StoneWall,
                gatePosition - right * (6.2f + 11.7f * (i - 1)),
                rotation,
                Vector3.one * 1.45f,
                area,
                true,
                false);

            Spawn(
                "Gate Wall Right " + i,
                i == 3 ? BrokenWall : StoneWall,
                gatePosition + right * (6.2f + 11.7f * (i - 1)),
                rotation,
                Vector3.one * 1.45f,
                area,
                true,
                false);
        }

        Spawn(
            "Left Gate Skull Column",
            SkullColumn,
            gatePosition - right * 6.1f - forward * 0.7f,
            rotation,
            Vector3.one * 1.25f,
            area,
            true,
            false);

        Spawn(
            "Right Gate Skull Column",
            SkullColumn,
            gatePosition + right * 6.1f - forward * 0.7f,
            rotation,
            Vector3.one * 1.25f,
            area,
            true,
            false);

        Spawn(
            "Left Gate Chain",
            Chain,
            gatePosition - right * 3.4f - forward * 0.5f,
            rotation,
            Vector3.one * 1.7f,
            area,
            false,
            false);

        Spawn(
            "Right Gate Chain",
            Chain,
            gatePosition + right * 3.4f - forward * 0.5f,
            rotation,
            Vector3.one * 1.7f,
            area,
            false,
            false);

        Spawn(
            "Gate Guardian Left",
            KuzarRealistic,
            guardianOne,
            Quaternion.LookRotation(forward, Vector3.up),
            Vector3.one * 0.68f,
            area,
            false,
            true);

        Spawn(
            "Gate Guardian Right",
            KuzarRealistic,
            guardianTwo,
            Quaternion.LookRotation(forward, Vector3.up),
            Vector3.one * 0.68f,
            area,
            false,
            true);
    }

    private static void BuildVillage(
        Transform parent,
        Vector3 village,
        Vector3 shop,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("02_ABANDONED_VILLAGE", parent);

        Spawn(
            "Village House A",
            VillageHouse,
            village - right * 18f + forward * 5f,
            Quaternion.LookRotation((village - (village - right * 18f + forward * 5f)).normalized, Vector3.up),
            Vector3.one * 0.72f,
            area,
            true,
            false);

        Spawn(
            "Village House B",
            VillageHouse,
            village + right * 18f + forward * 4f,
            Quaternion.Euler(0f, 205f, 0f),
            Vector3.one * 0.68f,
            area,
            true,
            false);

        Spawn(
            "Village House C",
            VillageHouse,
            village - right * 14f - forward * 17f,
            Quaternion.Euler(0f, 35f, 0f),
            Vector3.one * 0.62f,
            area,
            true,
            false);

        Spawn(
            "Village House D",
            VillageHouse,
            village + right * 15f - forward * 16f,
            Quaternion.Euler(0f, 145f, 0f),
            Vector3.one * 0.64f,
            area,
            true,
            false);

        Spawn(
            "Village Mill",
            VillageMill,
            village - right * 30f - forward * 8f,
            Quaternion.Euler(0f, 25f, 0f),
            Vector3.one * 0.78f,
            area,
            true,
            false);

        Spawn(
            "Village Well",
            VillageWell,
            village,
            Quaternion.identity,
            Vector3.one * 0.58f,
            area,
            true,
            false);

        Spawn(
            "Village Gallows",
            VillageGallows,
            village + right * 26f + forward * 18f,
            Quaternion.Euler(0f, -20f, 0f),
            Vector3.one * 0.62f,
            area,
            true,
            false);

        Spawn(
            "Village Lamp A",
            VillageLamp,
            village - right * 4f + forward * 8f,
            Quaternion.identity,
            Vector3.one,
            area,
            true,
            false);

        Spawn(
            "Village Lamp B",
            VillageLamp,
            village + right * 5f - forward * 8f,
            Quaternion.Euler(0f, 180f, 0f),
            Vector3.one,
            area,
            true,
            false);

        for (int i = 0; i < 4; i++)
        {
            Spawn(
                "Village Fence West " + i,
                VillageFence,
                village - right * 34f + forward * (-20f + i * 12f),
                Quaternion.LookRotation(right, Vector3.up),
                new Vector3(0.48f, 0.82f, 1f),
                area,
                true,
                false);
        }

        Spawn(
            "Village Barrel Cluster A",
            VillageBarrel,
            village + right * 8f + forward * 2f,
            Quaternion.Euler(0f, 10f, 0f),
            Vector3.one * 0.7f,
            area,
            true,
            false);

        Spawn(
            "Village Box Cluster A",
            VillageBox,
            village + right * 10f + forward * 3f,
            Quaternion.Euler(0f, 25f, 0f),
            Vector3.one * 0.8f,
            area,
            true,
            false);

        Spawn(
            "Village Wheelbarrow",
            VillageWheelbarrow,
            village - right * 5f - forward * 9f,
            Quaternion.Euler(0f, 40f, 0f),
            Vector3.one * 0.55f,
            area,
            true,
            false);

        BuildKeyMakerShop(area, shop, forward, right);
    }

    private static void BuildKeyMakerShop(
        Transform parent,
        Vector3 shop,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("KEY_MAKER_BROKEN_SHOP", parent);

        Spawn(
            "Key Maker House",
            VillageHouse,
            shop,
            Quaternion.LookRotation(-forward, Vector3.up),
            Vector3.one * 0.7f,
            area,
            true,
            false);

        Vector3 yard = shop - forward * 10f;

        Spawn("Forge", Forge, yard - right * 3f, Quaternion.LookRotation(forward, Vector3.up), Vector3.one * 1.15f, area, true, false);
        Spawn("Anvil", Anvil, yard + right * 0.5f, Quaternion.Euler(0f, 85f, 0f), Vector3.one * 1.4f, area, true, false);
        Spawn("Bellows", Bellows, yard - right * 2.2f - forward * 1.4f, Quaternion.Euler(0f, 20f, 0f), Vector3.one * 1.4f, area, true, false);
        Spawn("Work Table", WorkTable, yard + right * 3.3f, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.5f, area, true, false);
        Spawn("Tool Rack", ToolRack, yard + right * 3.7f + forward * 0.6f, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.5f, area, true, false);
        Spawn("Grindstone", Grindstone, yard - right * 0.8f - forward * 2.8f, Quaternion.Euler(0f, 120f, 0f), Vector3.one, area, true, false);
        Spawn("Quenching Trough", QuenchingTrough, yard + right * 1.7f - forward * 1.8f, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.2f, area, true, false);
        Spawn("Blacksmith Sign", BlacksmithSign, shop - forward * 7f - right * 5f, Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.5f, area, true, false);
        Spawn("Ore Crate", OreCrate, yard + right * 5f - forward * 1f, Quaternion.Euler(0f, 15f, 0f), Vector3.one * 1.25f, area, true, false);
        Spawn("Coal Pile", CoalPile, yard - right * 4.2f - forward * 0.2f, Quaternion.identity, Vector3.one * 1.4f, area, true, false);

        Spawn(
            "Empty Key Box",
            OpenChest,
            yard + right * 4.8f + forward * 1.8f,
            Quaternion.Euler(0f, 210f, 0f),
            Vector3.one * 0.55f,
            area,
            true,
            false);

        Spawn(
            "Warning Letter",
            Scroll,
            yard + right * 4.55f + forward * 1.85f + Vector3.up * 1.45f,
            Quaternion.Euler(90f, 0f, 25f),
            Vector3.one * 1.4f,
            area,
            false,
            false);

        Spawn(
            "Final Gate Key Preview",
            Key,
            yard + right * 2.8f + forward * 0.8f + Vector3.up * 1f,
            Quaternion.Euler(15f, 0f, 90f),
            Vector3.one * 6f,
            area,
            false,
            false);
    }

    private static void BuildCastle(
        Transform parent,
        Vector3 castle,
        Vector3 prison,
        Vector3 boss,
        Vector3 keyMakerNpc,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("03_RUINED_CASTLE_AND_PRISON", parent);
        Quaternion frontRotation = Quaternion.LookRotation(forward, Vector3.up);

        Spawn(
            "Castle Front Gate",
            GateArch,
            castle,
            frontRotation,
            Vector3.one * 1.35f,
            area,
            true,
            false);

        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;

            Spawn(
                "Castle Front Wall " + i,
                Mathf.Abs(i) == 2 ? BrokenWall : StoneWall,
                castle + right * (i * 8.1f),
                frontRotation,
                Vector3.one,
                area,
                true,
                false);
        }

        Vector3 courtyard = castle + forward * 20f;

        for (int x = -4; x <= 4; x++)
        {
            for (int z = -4; z <= 4; z++)
            {
                Spawn(
                    "Castle Floor " + x + "_" + z,
                    StoneFloor,
                    courtyard + right * (x * 4f) + forward * (z * 4f),
                    frontRotation,
                    Vector3.one,
                    area,
                    true,
                    false);
            }
        }

        for (int i = -4; i <= 4; i++)
        {
            Spawn(
                "Castle Left Wall " + i,
                i == 2 ? BrokenWall : StoneWall,
                courtyard - right * 36f + forward * (i * 8f),
                Quaternion.LookRotation(right, Vector3.up),
                Vector3.one,
                area,
                true,
                false);

            Spawn(
                "Castle Right Wall " + i,
                i == -1 ? BrokenWall : StoneWall,
                courtyard + right * 36f + forward * (i * 8f),
                Quaternion.LookRotation(right, Vector3.up),
                Vector3.one,
                area,
                true,
                false);
        }

        for (int i = -4; i <= 4; i++)
        {
            Spawn(
                "Castle Rear Wall " + i,
                i == 1 ? BrokenWall : StoneWall,
                courtyard + right * (i * 8f) + forward * 36f,
                frontRotation,
                Vector3.one,
                area,
                true,
                false);
        }

        Spawn(
            "Castle Stairs",
            StoneStairs,
            courtyard - right * 24f + forward * 25f,
            Quaternion.LookRotation(right, Vector3.up),
            Vector3.one * 0.82f,
            area,
            true,
            false);

        BuildDungeonCorridor(area, courtyard, prison, forward, right);
        BuildPrison(area, prison, keyMakerNpc, forward, right);
        BuildBossArena(area, boss, forward, right);
    }

    private static void BuildDungeonCorridor(
        Transform parent,
        Vector3 courtyard,
        Vector3 prison,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("CASTLE_DUNGEON_CORRIDOR", parent);

        Vector3 corridorStart = courtyard + forward * 21f - right * 9f;
        Vector3 corridorEnd = prison - forward * 7f;
        Vector3 direction = Flatten(corridorEnd - corridorStart).normalized;

        if (direction.sqrMagnitude < 0.01f)
            direction = forward;

        Vector3 corridorRight = Vector3.Cross(Vector3.up, direction).normalized;
        float length = Vector3.Distance(corridorStart, corridorEnd);
        int segments = Mathf.Max(4, Mathf.CeilToInt(length / 4f));

        for (int i = 0; i <= segments; i++)
        {
            float t = segments == 0 ? 0f : i / (float)segments;
            Vector3 center = Vector3.Lerp(corridorStart, corridorEnd, t);

            Spawn(
                "Dungeon Floor " + i,
                i % 4 == 2 ? BrokenFloor : StoneFloor,
                center,
                Quaternion.LookRotation(direction, Vector3.up),
                Vector3.one,
                area,
                true,
                false);

            Spawn(
                "Dungeon Left Wall " + i,
                i % 5 == 3 ? BrokenWall : StoneWall,
                center - corridorRight * 4.2f,
                Quaternion.LookRotation(corridorRight, Vector3.up),
                new Vector3(0.72f, 0.78f, 0.72f),
                area,
                true,
                false);

            Spawn(
                "Dungeon Right Wall " + i,
                i % 6 == 2 ? BrokenWall : StoneWall,
                center + corridorRight * 4.2f,
                Quaternion.LookRotation(corridorRight, Vector3.up),
                new Vector3(0.72f, 0.78f, 0.72f),
                area,
                true,
                false);

            if (i > 0 && i < segments && i % 3 == 0)
            {
                Spawn(
                    "Dungeon Arch " + i,
                    RoundArch,
                    center,
                    Quaternion.LookRotation(direction, Vector3.up),
                    new Vector3(0.92f, 0.92f, 0.92f),
                    area,
                    true,
                    false);
            }

            if (i % 2 == 0)
            {
                Spawn(
                    "Dungeon Candle " + i,
                    CandleGroup,
                    center - corridorRight * 2.9f + direction * 0.5f,
                    Quaternion.identity,
                    Vector3.one,
                    area,
                    false,
                    false);
            }

            if (i == 2 || i == segments - 2)
            {
                Spawn(
                    "Dungeon Broken Bench " + i,
                    BrokenBench,
                    center + corridorRight * 2.5f,
                    Quaternion.LookRotation(-corridorRight, Vector3.up),
                    Vector3.one * 0.9f,
                    area,
                    true,
                    false);

                Spawn(
                    "Dungeon Skull " + i,
                    Skull,
                    center + corridorRight * 2.2f + direction * 0.8f,
                    Quaternion.Euler(0f, i * 37f, 0f),
                    Vector3.one,
                    area,
                    false,
                    false);
            }
        }

        Spawn(
            "Dungeon Entrance Arch",
            RoundArch,
            corridorStart,
            Quaternion.LookRotation(direction, Vector3.up),
            Vector3.one,
            area,
            true,
            false);
    }

    private static void BuildPrison(
        Transform parent,
        Vector3 prison,
        Vector3 keyMakerNpc,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("CASTLE_PRISON_BLOCK", parent);

        // Central prison hallway.
        for (int z = -2; z <= 2; z++)
        {
            Spawn(
                "Prison Hall Floor " + z,
                z == 1 ? BrokenFloor : StoneFloor,
                prison + forward * (z * 4f),
                Quaternion.LookRotation(forward, Vector3.up),
                Vector3.one,
                area,
                true,
                false);
        }

        Spawn(
            "Prison Rear Wall",
            StoneWall,
            prison + forward * 12f,
            Quaternion.LookRotation(forward, Vector3.up),
            Vector3.one,
            area,
            true,
            false);

        // Three cells on each side of a narrow corridor.
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 sideVector = right * side;

            for (int cell = -1; cell <= 1; cell++)
            {
                Vector3 cellCenter =
                    prison + sideVector * 7f + forward * (cell * 8f + 2f);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Back Wall " + cell,
                    cell == 1 ? BrokenWall : StoneWall,
                    cellCenter + sideVector * 4f,
                    Quaternion.LookRotation(right, Vector3.up),
                    new Vector3(0.85f, 0.85f, 0.85f),
                    area,
                    true,
                    false);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Side Wall A " + cell,
                    StoneWall,
                    cellCenter - forward * 4f,
                    Quaternion.LookRotation(forward, Vector3.up),
                    new Vector3(0.85f, 0.85f, 0.85f),
                    area,
                    true,
                    false);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Side Wall B " + cell,
                    StoneWall,
                    cellCenter + forward * 4f,
                    Quaternion.LookRotation(forward, Vector3.up),
                    new Vector3(0.85f, 0.85f, 0.85f),
                    area,
                    true,
                    false);

                Quaternion doorRotation =
                    Quaternion.LookRotation(sideVector, Vector3.up);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Door Left " + cell,
                    MetalDoorLeft,
                    cellCenter - forward * 0.88f - sideVector * 3.25f,
                    doorRotation,
                    Vector3.one,
                    area,
                    true,
                    false);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Door Right " + cell,
                    MetalDoorRight,
                    cellCenter + forward * 0.88f - sideVector * 3.25f,
                    doorRotation,
                    Vector3.one,
                    area,
                    true,
                    false);

                Spawn(
                    (side < 0 ? "Left" : "Right") + " Cell Chain " + cell,
                    Chain,
                    cellCenter + sideVector * 1.8f,
                    Quaternion.identity,
                    Vector3.one * 1.15f,
                    area,
                    false,
                    false);

                if (cell != 0 || side > 0)
                {
                    Spawn(
                        (side < 0 ? "Left" : "Right") + " Cell Coffin " + cell,
                        Coffin,
                        cellCenter + sideVector * 1.4f + forward * 1.4f,
                        Quaternion.LookRotation(forward, Vector3.up),
                        Vector3.one * 0.7f,
                        area,
                        true,
                        false);
                }
            }
        }

        for (int z = -2; z <= 2; z++)
        {
            Spawn(
                "Prison Hall Candle " + z,
                CandleGroup,
                prison - right * 2.3f + forward * (z * 4f),
                Quaternion.identity,
                Vector3.one,
                area,
                false,
                false);
        }

        // The story cell is the middle-left cell.
        Vector3 storyCellCenter = prison - right * 7f + forward * 2f;

        Spawn(
            "Key Maker Chain A",
            Chain,
            storyCellCenter - right * 1.5f - forward * 0.9f,
            Quaternion.identity,
            Vector3.one * 1.3f,
            area,
            false,
            false);

        Spawn(
            "Key Maker Chain B",
            Chain,
            storyCellCenter - right * 1.5f + forward * 0.9f,
            Quaternion.identity,
            Vector3.one * 1.3f,
            area,
            false,
            false);

        Spawn(
            "Imprisoned Key Maker",
            MalePeasant,
            storyCellCenter - right * 1.4f,
            Quaternion.LookRotation(right, Vector3.up),
            Vector3.one,
            area,
            false,
            true);

        Spawn(
            "Prison Warning Skull",
            Skull,
            prison + right * 1.5f - forward * 5f,
            Quaternion.Euler(0f, 35f, 0f),
            Vector3.one * 1.2f,
            area,
            false,
            false);
    }

    private static void BuildBossArena(
        Transform parent,
        Vector3 boss,
        Vector3 forward,
        Vector3 right)
    {
        Transform area = Group("CASTLE_BOSS_ARENA", parent);

        Vector3[] corners =
        {
            boss - right * 11f - forward * 11f,
            boss + right * 11f - forward * 11f,
            boss - right * 11f + forward * 11f,
            boss + right * 11f + forward * 11f
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Spawn(
                "Boss Arena Skull Column " + (i + 1),
                SkullColumn,
                corners[i],
                Quaternion.LookRotation(forward, Vector3.up),
                Vector3.one * 1.15f,
                area,
                true,
                false);
        }

        Spawn(
            "Castle Guardian Boss",
            KuzarRealistic,
            boss,
            Quaternion.LookRotation(-forward, Vector3.up),
            Vector3.one,
            area,
            false,
            true);

        Spawn(
            "Boss Reward Chest",
            ClosedChest,
            boss + forward * 12f,
            Quaternion.LookRotation(-forward, Vector3.up),
            Vector3.one * 0.75f,
            area,
            true,
            false);
    }

    private static GameObject Spawn(
        string name,
        string assetPath,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Transform parent,
        bool addColliders,
        bool disableAnimator)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (prefab == null)
        {
            Report.Add("MISSING: " + assetPath);
            Debug.LogWarning("Phase 1 builder could not find " + assetPath);
            return null;
        }

        GameObject instance =
            PrefabUtility.InstantiatePrefab(
                prefab,
                SceneManager.GetActiveScene()) as GameObject;

        if (instance == null)
        {
            Report.Add("FAILED TO INSTANTIATE: " + assetPath);
            return null;
        }

        instance.name = name;
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale);

        GroundToTerrain(instance);

        if (disableAnimator)
        {
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;
        }

        if (addColliders)
        {
            AddStaticMeshColliders(instance);
            SetStaticRecursively(instance);
        }

        Report.Add(
            "OK: " + name +
            " | " + assetPath +
            " | pos " + instance.transform.position);

        return instance;
    }

    private static void GroundToTerrain(GameObject instance)
    {
        Bounds bounds;
        if (!TryGetBounds(instance, out bounds))
        {
            instance.transform.position =
                TerrainPosition(instance.transform.position);
            return;
        }

        Vector3 position = instance.transform.position;
        float groundY = TerrainPosition(position).y;
        float difference = groundY - bounds.min.y;
        instance.transform.position += Vector3.up * difference;
    }

    private static Vector3 TerrainPosition(Vector3 world)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
            return world;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        bool inside =
            world.x >= terrainPosition.x &&
            world.z >= terrainPosition.z &&
            world.x <= terrainPosition.x + size.x &&
            world.z <= terrainPosition.z + size.z;

        if (!inside)
            return world;

        world.y = terrain.SampleHeight(world) + terrainPosition.y;
        return world;
    }

    private static void AddStaticMeshColliders(GameObject root)
    {
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null)
                continue;

            if (filter.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
        }
    }

    private static void SetStaticRecursively(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(
                child.gameObject,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.NavigationStatic);
    }

    private static Transform FindPlayer()
    {
        GameObject player =
            GameObject.Find("player") ??
            GameObject.Find("Player");

        if (player != null)
            return player.transform;

        foreach (FirstPersonPlayer controller in
                 UnityEngine.Object.FindObjectsByType<FirstPersonPlayer>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            return controller.transform;
        }

        return Selection.activeTransform;
    }

    private static Vector3 FindBestTerrainLocation(
        string locationName,
        float minNormalizedX,
        float maxNormalizedX,
        float minNormalizedZ,
        float maxNormalizedZ,
        float sampleRadius,
        float elevationPreference,
        float flatnessWeight,
        float slopeWeight,
        float elevationWeight)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning(
                "No active Terrain was found while placing " +
                locationName + ".");

            return TerrainPoint(
                (minNormalizedX + maxNormalizedX) * 0.5f,
                (minNormalizedZ + maxNormalizedZ) * 0.5f);
        }

        const int gridResolution = 19;

        float bestScore = float.NegativeInfinity;
        Vector3 bestPoint = Vector3.zero;
        float bestHeightRange = 0f;
        float bestAverageSlope = 0f;
        float bestNormalizedHeight = 0f;

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        for (int x = 0; x < gridResolution; x++)
        {
            float tx = (x + 0.5f) / gridResolution;
            float normalizedX = Mathf.Lerp(
                minNormalizedX,
                maxNormalizedX,
                tx);

            for (int z = 0; z < gridResolution; z++)
            {
                float tz = (z + 0.5f) / gridResolution;
                float normalizedZ = Mathf.Lerp(
                    minNormalizedZ,
                    maxNormalizedZ,
                    tz);

                Vector3 candidate = TerrainPoint(
                    normalizedX,
                    normalizedZ);

                float averageHeight;
                float heightRange;
                float averageSlope;

                EvaluateTerrainPatch(
                    terrain,
                    candidate,
                    sampleRadius,
                    out averageHeight,
                    out heightRange,
                    out averageSlope);

                float normalizedHeight = Mathf.Clamp01(
                    (averageHeight - terrainOrigin.y) /
                    Mathf.Max(1f, terrainSize.y));

                float flatnessScore = Mathf.Clamp01(
                    1f -
                    heightRange /
                    Mathf.Max(5f, sampleRadius * 0.24f));

                float slopeScore = Mathf.Clamp01(
                    1f - averageSlope / 30f);

                float elevationScore;

                if (elevationPreference > 0f)
                    elevationScore = normalizedHeight;
                else if (elevationPreference < 0f)
                    elevationScore = 1f - normalizedHeight;
                else
                    elevationScore = 0.5f;

                float score =
                    flatnessScore * flatnessWeight +
                    slopeScore * slopeWeight +
                    elevationScore * elevationWeight;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPoint = candidate;
                bestHeightRange = heightRange;
                bestAverageSlope = averageSlope;
                bestNormalizedHeight = normalizedHeight;
            }
        }

        bestPoint = RoutePosition(bestPoint);

        Report.Add(
            locationName +
            " TERRAIN SEARCH: position=" +
            bestPoint +
            " | patch radius=" +
            sampleRadius.ToString("0.0") +
            "m | height range=" +
            bestHeightRange.ToString("0.0") +
            "m | average slope=" +
            bestAverageSlope.ToString("0.0") +
            " degrees | normalized elevation=" +
            bestNormalizedHeight.ToString("0.00"));

        return bestPoint;
    }

    private static void EvaluateTerrainPatch(
        Terrain terrain,
        Vector3 center,
        float radius,
        out float averageHeight,
        out float heightRange,
        out float averageSlope)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        float minimumHeight = float.PositiveInfinity;
        float maximumHeight = float.NegativeInfinity;
        float heightTotal = 0f;
        float slopeTotal = 0f;
        int sampleCount = 0;

        const int halfGrid = 2;

        for (int x = -halfGrid; x <= halfGrid; x++)
        {
            for (int z = -halfGrid; z <= halfGrid; z++)
            {
                float offsetX =
                    x / (float)halfGrid * radius;
                float offsetZ =
                    z / (float)halfGrid * radius;

                Vector3 samplePoint = new Vector3(
                    Mathf.Clamp(
                        center.x + offsetX,
                        origin.x,
                        origin.x + size.x),
                    origin.y,
                    Mathf.Clamp(
                        center.z + offsetZ,
                        origin.z,
                        origin.z + size.z));

                float height =
                    terrain.SampleHeight(samplePoint) + origin.y;

                float normalizedX = Mathf.Clamp01(
                    (samplePoint.x - origin.x) /
                    Mathf.Max(1f, size.x));

                float normalizedZ = Mathf.Clamp01(
                    (samplePoint.z - origin.z) /
                    Mathf.Max(1f, size.z));

                float slope =
                    terrain.terrainData.GetSteepness(
                        normalizedX,
                        normalizedZ);

                minimumHeight = Mathf.Min(
                    minimumHeight,
                    height);

                maximumHeight = Mathf.Max(
                    maximumHeight,
                    height);

                heightTotal += height;
                slopeTotal += slope;
                sampleCount++;
            }
        }

        averageHeight =
            sampleCount > 0
                ? heightTotal / sampleCount
                : center.y;

        averageSlope =
            sampleCount > 0
                ? slopeTotal / sampleCount
                : 0f;

        heightRange =
            sampleCount > 0
                ? maximumHeight - minimumHeight
                : 0f;
    }

    private static Vector3 TerrainPoint(
        float normalizedX,
        float normalizedZ)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning(
                "No active Terrain was found. " +
                "Using approximate fallback positions.");

            return new Vector3(
                normalizedX * 500f,
                0f,
                normalizedZ * 500f);
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        Vector3 point = new Vector3(
            origin.x + size.x * Mathf.Clamp01(normalizedX),
            origin.y,
            origin.z + size.z * Mathf.Clamp01(normalizedZ));

        point.y =
            terrain.SampleHeight(point) + origin.y;

        return point;
    }

    private static Vector3 PointNearTerrainEdge(
        Vector3 start,
        Vector3 direction,
        float inset)
    {
        Terrain terrain = Terrain.activeTerrain;

        direction = Flatten(direction).normalized;

        if (terrain == null ||
            terrain.terrainData == null ||
            direction.sqrMagnitude < 0.01f)
        {
            return RoutePosition(start + direction * 250f);
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        float minX = origin.x + inset;
        float maxX = origin.x + size.x - inset;
        float minZ = origin.z + inset;
        float maxZ = origin.z + size.z - inset;

        float maxDistance = float.PositiveInfinity;

        if (direction.x > 0.0001f)
            maxDistance = Mathf.Min(
                maxDistance,
                (maxX - start.x) / direction.x);
        else if (direction.x < -0.0001f)
            maxDistance = Mathf.Min(
                maxDistance,
                (minX - start.x) / direction.x);

        if (direction.z > 0.0001f)
            maxDistance = Mathf.Min(
                maxDistance,
                (maxZ - start.z) / direction.z);
        else if (direction.z < -0.0001f)
            maxDistance = Mathf.Min(
                maxDistance,
                (minZ - start.z) / direction.z);

        if (float.IsInfinity(maxDistance) ||
            float.IsNaN(maxDistance) ||
            maxDistance < 0f)
        {
            maxDistance =
                Mathf.Min(size.x, size.z) * 0.40f;
        }

        // Stop just inside the edge while still using most of the terrain.
        float distance = Mathf.Max(80f, maxDistance * 0.88f);

        return RoutePosition(start + direction * distance);
    }

    private static Vector3 RoutePosition(Vector3 requested)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
            return requested;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        const float margin = 18f;

        requested.x = Mathf.Clamp(
            requested.x,
            origin.x + margin,
            origin.x + size.x - margin);

        requested.z = Mathf.Clamp(
            requested.z,
            origin.z + margin,
            origin.z + size.z - margin);

        requested.y =
            terrain.SampleHeight(requested) + origin.y;

        return requested;
    }

    private static Vector3 MarkerOrFallback(
        string[] tokens,
        Vector3 fallback)
    {
        Transform marker = FindTransformByTokens(tokens);

        if (marker != null)
        {
            Report.Add(
                "MARKER: " +
                string.Join("+", tokens) +
                " -> " +
                marker.name);
            return marker.position;
        }

        Report.Add(
            "FALLBACK: " +
            string.Join("+", tokens) +
            " -> " +
            fallback);

        return TerrainPosition(fallback);
    }

    private static Transform FindTransformByTokens(string[] tokens)
    {
        Transform[] all =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Transform candidate in all)
        {
            string normalized =
                candidate.name
                    .ToLowerInvariant()
                    .Replace("_", " ")
                    .Replace("-", " ");

            bool matches = true;

            foreach (string token in tokens)
            {
                if (!normalized.Contains(token.ToLowerInvariant()))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return candidate;
        }

        return null;
    }

    private static Vector3 Flatten(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private static Transform Group(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group.transform;
    }

    private static bool TryGetBounds(
        GameObject root,
        out Bounds bounds)
    {
        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private static void DeleteExistingRoot(bool showDialog)
    {
        GameObject existing = GameObject.Find(RootName);

        if (existing == null)
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

        Undo.DestroyObjectImmediate(existing);
        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 1 World Deleted",
                RootName + " was removed.",
                "OK");
        }
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
