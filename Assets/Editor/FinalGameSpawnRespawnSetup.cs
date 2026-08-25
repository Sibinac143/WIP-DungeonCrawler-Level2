using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class FinalGameSpawnRespawnSetup
{
    private const string RootName =
        "FINAL_GAME_SPAWN_SYSTEM";

    private const string StartPointName =
        "Real Game Start";

    private const string ReportPath =
        "Assets/Generated/FinalSpawnRespawnReport.txt";

    [MenuItem(
        "Tools/Dungeon Game/Finalize/Restore Real Start and Dynamic Respawn",
        priority = 900)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before restoring the real game start.",
                "OK");
            return;
        }

        if (!EditorSceneManager
                .SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        GameObject player =
            FindSceneObject("player") ??
            FindSceneObject("Player");

        GameObject mainGate =
            FindSceneObject("Main Gate Arch");

        if (player == null ||
            mainGate == null)
        {
            EditorUtility.DisplayDialog(
                "Required Objects Missing",
                "Player found: " +
                (player != null) +
                "\nMain Gate Arch found: " +
                (mainGate != null),
                "OK");
            return;
        }

        DeleteExistingRoot();

        GameObject root =
            new GameObject(RootName);

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create Final Spawn System");

        Vector3 approachDirection =
            FindGateApproachDirection(
                player,
                mainGate);

        Vector3 desiredStart =
            mainGate.transform.position +
            approachDirection * 20f;

        Vector3 startPosition =
            FindSafeNavMeshPosition(
                desiredStart,
                mainGate.transform.position,
                approachDirection);

        Quaternion startRotation =
            Quaternion.LookRotation(
                -approachDirection,
                Vector3.up);

        GameObject startPoint =
            new GameObject(StartPointName);

        Undo.RegisterCreatedObjectUndo(
            startPoint,
            "Create Real Game Start");

        Undo.SetTransformParent(
            startPoint.transform,
            root.transform,
            "Parent Real Game Start");

        startPoint.transform.SetPositionAndRotation(
            startPosition,
            startRotation);

        Transform oldRespawn =
            FindTransformByExactName(
                "Player Respawn Point");

        if (oldRespawn != null)
        {
            Undo.RecordObject(
                oldRespawn,
                "Move Old Respawn Point");

            oldRespawn.SetPositionAndRotation(
                startPosition,
                startRotation);
        }

        CombatHealth health =
            player.GetComponent<CombatHealth>();

        PlayerRespawn respawn =
            player.GetComponent<PlayerRespawn>();

        if (respawn == null)
        {
            respawn =
                Undo.AddComponent<PlayerRespawn>(
                    player);
        }

        respawn.Configure(
            health,
            startPoint.transform);

        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        Undo.RecordObject(
            player.transform,
            "Restore Player Start");

        player.transform.SetPositionAndRotation(
            startPosition + Vector3.up * 0.08f,
            startRotation);

        Physics.SyncTransforms();

        if (controller != null)
            controller.enabled = true;

        if (health != null)
            health.Revive();

        PlayerShield shield =
            player.GetComponent<PlayerShield>();

        if (shield != null)
            shield.Configure(100f, 0f);

        MoveNearbySpidersAway(
            startPosition,
            approachDirection);

        QuestManager.ResetSavedProgress();

        EnsureFolder("Assets/Generated");

        List<string> report =
            new List<string>
            {
                "FINAL SPAWN + DYNAMIC RESPAWN REPORT",
                "Scene: " +
                SceneManager.GetActiveScene().path,
                "Built: " +
                DateTime.Now,
                "",
                "Real start: " +
                startPosition.ToString("F2"),
                "Distance from gate: " +
                Vector3.Distance(
                    startPosition,
                    mainGate.transform.position)
                    .ToString("F1") +
                " m",
                "Enemy-relative respawn distance: 20 m",
                "Post-respawn protection: 3 seconds",
                "Player health restored to 100",
                "Player shield reset to 0",
                "Quest progress reset to Inspect Main Gate",
                "Spider test position removed"
            };

        File.WriteAllLines(
            ReportPath,
            report);

        AssetDatabase.ImportAsset(
            ReportPath);

        EditorUtility.SetDirty(respawn);

        if (health != null)
            EditorUtility.SetDirty(health);

        if (shield != null)
            EditorUtility.SetDirty(shield);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = startPoint;
        EditorGUIUtility.PingObject(startPoint);

        EditorUtility.DisplayDialog(
            "Real Game Start Restored",
            "The spider reward-test position was removed.\n\n" +
            "The player now starts about 20 meters from the main gate.\n" +
            "Deaths caused by a demon, monster, or spider now respawn the player about 20 meters away from that attacker.\n" +
            "A safe NavMesh position is selected, controls are restored, health returns to 100, shield resets to 0, and the player receives 3 seconds of protection.",
            "OK");
    }

    private static Vector3 FindGateApproachDirection(
        GameObject player,
        GameObject mainGate)
    {
        GameObject strikePoint =
            FindSceneObject(
                "Gate Throw Strike Point");

        if (strikePoint != null)
        {
            Vector3 direction =
                strikePoint.transform.position -
                mainGate.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude >
                0.001f)
            {
                return direction.normalized;
            }
        }

        GameObject phaseOneSpawn =
            FindSceneObject(
                "01_Player_Spawn");

        if (phaseOneSpawn != null)
        {
            Vector3 direction =
                phaseOneSpawn.transform.position -
                mainGate.transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude >
                0.001f)
            {
                return direction.normalized;
            }
        }

        Vector3 playerDirection =
            player.transform.position -
            mainGate.transform.position;

        playerDirection.y = 0f;

        if (playerDirection.sqrMagnitude >
            0.001f)
        {
            return playerDirection.normalized;
        }

        return -mainGate.transform.forward;
    }

    private static Vector3 FindSafeNavMeshPosition(
        Vector3 desired,
        Vector3 gatePosition,
        Vector3 approachDirection)
    {
        float[] offsets =
        {
            0f,
            3f,
            -3f,
            6f,
            -6f
        };

        foreach (float sideOffset in offsets)
        {
            Vector3 side =
                Vector3.Cross(
                    Vector3.up,
                    approachDirection);

            Vector3 candidate =
                desired +
                side * sideOffset;

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    14f,
                    NavMesh.AllAreas) &&
                Vector3.Distance(
                    hit.position,
                    gatePosition) >= 14f)
            {
                return hit.position;
            }
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

    private static void MoveNearbySpidersAway(
        Vector3 startPosition,
        Vector3 approachDirection)
    {
        GameObject spiderRoot =
            FindSceneObject(
                "PHASE_05_RECOVERY_SPIDERS");

        if (spiderRoot == null)
            return;

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                approachDirection);

        int moved = 0;

        foreach (Transform spider in
                 spiderRoot.transform)
        {
            if (Vector3.Distance(
                    spider.position,
                    startPosition) >= 13f)
            {
                continue;
            }

            float sideSign =
                moved % 2 == 0
                    ? 1f
                    : -1f;

            Vector3 desired =
                startPosition +
                approachDirection *
                (18f + moved * 2f) +
                side *
                sideSign *
                (8f + moved);

            if (NavMesh.SamplePosition(
                    desired,
                    out NavMeshHit hit,
                    12f,
                    NavMesh.AllAreas))
            {
                Undo.RecordObject(
                    spider,
                    "Move Spider Away From Start");

                spider.position = hit.position;
            }

            moved++;
        }
    }

    private static void DeleteExistingRoot()
    {
        GameObject existing =
            FindSceneObject(RootName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    private static Transform FindTransformByExactName(
        string exactName)
    {
        GameObject objectResult =
            FindSceneObject(exactName);

        return objectResult != null
            ? objectResult.transform
            : null;
    }

    private static GameObject FindSceneObject(
        string exactName)
    {
        foreach (Transform transform in
                 UnityEngine.Object
                     .FindObjectsByType<Transform>(
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
