using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GroundingTools
{
    // Slightly sinks trees into the terrain so roots do not visibly float.
    private const float TreeSinkAmount = 0.08f;

    [MenuItem("Tools/Dungeon Game/Fix Grounding/Rune Sanctuary")]
    public static void FixRuneSanctuary()
    {
        GameObject sanctuary =
            GameObject.Find("RuneSanctuary_Generated");

        if (sanctuary == null)
        {
            EditorUtility.DisplayDialog(
                "Rune Sanctuary Not Found",
                "RuneSanctuary_Generated was not found in the active scene.",
                "OK"
            );

            return;
        }

        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Terrain Found",
                "No active Terrain was found in the scene.",
                "OK"
            );

            return;
        }

        HashSet<GameObject> objects =
            CollectPrefabObjectsUnder(sanctuary.transform);

        int fixedCount = GroundObjects(
            objects,
            terrains,
            0.03f
        );

        FinishSceneChange();

        EditorUtility.DisplayDialog(
            "Sanctuary Grounding Complete",
            $"Adjusted {fixedCount} sanctuary objects.",
            "OK"
        );
    }

    [MenuItem("Tools/Dungeon Game/Fix Grounding/All Scene Trees")]
    public static void FixAllSceneTrees()
    {
        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Terrain Found",
                "No active Terrain was found in the scene.",
                "OK"
            );

            return;
        }

        HashSet<GameObject> trees =
            CollectSceneTreeObjects();

        int fixedCount = GroundObjects(
            trees,
            terrains,
            TreeSinkAmount
        );

        FinishSceneChange();

        EditorUtility.DisplayDialog(
            "Scene Trees Grounded",
            $"Adjusted {fixedCount} tree GameObjects.",
            "OK"
        );
    }

    [MenuItem("Tools/Dungeon Game/Fix Grounding/Terrain-Painted Trees")]
    public static void FixTerrainPaintedTrees()
    {
        int fixedCount = FixPaintedTreeInstances();

        FinishSceneChange();

        EditorUtility.DisplayDialog(
            "Terrain Trees Grounded",
            $"Adjusted {fixedCount} terrain-painted tree instances.",
            "OK"
        );
    }

    [MenuItem("Tools/Dungeon Game/Fix Grounding/All Trees Everywhere")]
    public static void FixAllTreesEverywhere()
    {
        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Terrain Found",
                "No active Terrain was found in the scene.",
                "OK"
            );

            return;
        }

        HashSet<GameObject> sceneTrees =
            CollectSceneTreeObjects();

        int sceneTreeCount = GroundObjects(
            sceneTrees,
            terrains,
            TreeSinkAmount
        );

        int paintedTreeCount =
            FixPaintedTreeInstances();

        FinishSceneChange();

        EditorUtility.DisplayDialog(
            "All Trees Grounded",
            $"Adjusted {sceneTreeCount} tree GameObjects and " +
            $"{paintedTreeCount} terrain-painted trees.",
            "OK"
        );
    }

    [MenuItem("Tools/Dungeon Game/Fix Grounding/Selected Objects")]
    public static void FixSelectedObjects()
    {
        Terrain[] terrains = Terrain.activeTerrains;

        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No Terrain Found",
                "No active Terrain was found in the scene.",
                "OK"
            );

            return;
        }

        HashSet<GameObject> selectedObjects =
            new HashSet<GameObject>();

        foreach (GameObject selected in Selection.gameObjects)
        {
            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    selected
                );

            selectedObjects.Add(
                prefabRoot != null
                    ? prefabRoot
                    : selected
            );
        }

        int fixedCount = GroundObjects(
            selectedObjects,
            terrains,
            0.03f
        );

        FinishSceneChange();

        EditorUtility.DisplayDialog(
            "Selected Objects Grounded",
            $"Adjusted {fixedCount} selected objects.",
            "OK"
        );
    }

    private static HashSet<GameObject>
        CollectPrefabObjectsUnder(Transform parent)
    {
        HashSet<GameObject> objects =
            new HashSet<GameObject>();

        Renderer[] renderers =
            parent.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    renderer.gameObject
                );

            if (prefabRoot != null &&
                prefabRoot.transform.IsChildOf(parent))
            {
                objects.Add(prefabRoot);
            }
            else
            {
                objects.Add(renderer.gameObject);
            }
        }

        return objects;
    }

    private static HashSet<GameObject>
        CollectSceneTreeObjects()
    {
        HashSet<GameObject> trees =
            new HashSet<GameObject>();

        Transform[] sceneTransforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform sceneTransform in sceneTransforms)
        {
            GameObject currentObject =
                sceneTransform.gameObject;

            if (!currentObject.scene.IsValid() ||
                !currentObject.scene.isLoaded)
            {
                continue;
            }

            if (currentObject.hideFlags != HideFlags.None)
                continue;

            GameObject prefabRoot =
                PrefabUtility.GetOutermostPrefabInstanceRoot(
                    currentObject
                );

            GameObject candidate =
                prefabRoot != null
                    ? prefabRoot
                    : currentObject;

            if (!IsTreeObject(candidate))
                continue;

            if (candidate.GetComponentInChildren<Renderer>(true)
                == null)
            {
                continue;
            }

            trees.Add(candidate);
        }

        return trees;
    }

    private static bool IsTreeObject(
        GameObject gameObject
    )
    {
        string prefabPath =
            PrefabUtility
                .GetPrefabAssetPathOfNearestInstanceRoot(
                    gameObject
                );

        if (!string.IsNullOrEmpty(prefabPath) &&
            prefabPath.Contains("/Prefabs/Trees/"))
        {
            return true;
        }

        string lowerName =
            gameObject.name.ToLowerInvariant();

        return lowerName.Contains("pine") ||
               lowerName.Contains("tree");
    }

    private static int GroundObjects(
        IEnumerable<GameObject> objects,
        Terrain[] terrains,
        float sinkAmount
    )
    {
        int fixedCount = 0;

        foreach (GameObject gameObject in objects)
        {
            if (gameObject == null)
                continue;

            if (!TryGetRendererBounds(
                    gameObject,
                    out Bounds bounds))
            {
                continue;
            }

            Terrain terrain =
                FindTerrainUnderPosition(
                    gameObject.transform.position,
                    terrains
                );

            if (terrain == null)
                continue;

            Vector3 samplePosition =
                gameObject.transform.position;

            float terrainHeight =
                terrain.SampleHeight(samplePosition) +
                terrain.transform.position.y;

            float desiredBottomY =
                terrainHeight - sinkAmount;

            float verticalDifference =
                desiredBottomY - bounds.min.y;

            if (Mathf.Abs(verticalDifference) < 0.001f)
                continue;

            Undo.RecordObject(
                gameObject.transform,
                "Ground Object"
            );

            gameObject.transform.position +=
                Vector3.up * verticalDifference;

            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    gameObject.transform
                );

            EditorUtility.SetDirty(
                gameObject.transform
            );

            fixedCount++;
        }

        return fixedCount;
    }

    private static int FixPaintedTreeInstances()
    {
        int fixedCount = 0;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                continue;
            }

            TerrainData terrainData =
                terrain.terrainData;

            TreeInstance[] treeInstances =
                terrainData.treeInstances;

            if (treeInstances == null ||
                treeInstances.Length == 0)
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(
                terrainData,
                "Ground Terrain Trees"
            );

            for (int i = 0;
                 i < treeInstances.Length;
                 i++)
            {
                TreeInstance tree =
                    treeInstances[i];

                float localTerrainHeight =
                    terrainData.GetInterpolatedHeight(
                        tree.position.x,
                        tree.position.z
                    );

                float correctedLocalHeight =
                    Mathf.Max(
                        0f,
                        localTerrainHeight -
                        TreeSinkAmount
                    );

                tree.position = new Vector3(
                    tree.position.x,
                    correctedLocalHeight /
                    terrainData.size.y,
                    tree.position.z
                );

                treeInstances[i] = tree;
                fixedCount++;
            }

            terrainData.treeInstances =
                treeInstances;

            EditorUtility.SetDirty(terrainData);
            terrain.Flush();
        }

        return fixedCount;
    }

    private static Terrain FindTerrainUnderPosition(
        Vector3 worldPosition,
        Terrain[] terrains
    )
    {
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3 terrainSize =
                terrain.terrainData.size;

            bool isInsideTerrain =
                worldPosition.x >= terrainPosition.x &&
                worldPosition.x <=
                    terrainPosition.x + terrainSize.x &&
                worldPosition.z >= terrainPosition.z &&
                worldPosition.z <=
                    terrainPosition.z + terrainSize.z;

            if (isInsideTerrain)
                return terrain;
        }

        return null;
    }

    private static bool TryGetRendererBounds(
        GameObject gameObject,
        out Bounds combinedBounds
    )
    {
        Renderer[] renderers =
            gameObject.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers.Length == 0)
        {
            combinedBounds = default;
            return false;
        }

        combinedBounds =
            renderers[0].bounds;

        for (int i = 1;
             i < renderers.Length;
             i++)
        {
            combinedBounds.Encapsulate(
                renderers[i].bounds
            );
        }

        return true;
    }

    private static void FinishSceneChange()
    {
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement
                .SceneManager.GetActiveScene()
        );

        SceneView.RepaintAll();

        Debug.Log(
            "Grounding completed. Press Command+S to save the scene."
        );
    }
}
