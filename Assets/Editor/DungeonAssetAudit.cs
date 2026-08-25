using UnityEditor;
using UnityEngine;

public static class DungeonAssetAudit
{
    private static readonly string[] PrefabPaths =
    {
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Menhir_Rock_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Generic_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_Ore_Rock_01_split.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks/PT_River_Rock_Pile_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_dead.prefab",
        "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_dead.prefab",
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Gate_Wood_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_01.prefab",
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_02.prefab",
        "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/PT_Modular_Fence_Wood_03.prefab"
    };

    [MenuItem("Tools/Dungeon Game/Audit Available Assets")]
    public static void AuditAssets()
    {
        Debug.Log("========== DUNGEON ASSET AUDIT ==========");

        foreach (string path in PrefabPaths)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"Missing prefab: {path}");
                continue;
            }

            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (instance == null)
            {
                Debug.LogError($"Could not inspect: {path}");
                continue;
            }

            instance.hideFlags = HideFlags.HideAndDontSave;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>(true);

            Collider[] colliders =
                instance.GetComponentsInChildren<Collider>(true);

            Bounds combinedBounds = new Bounds();
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            string materialNames = "";

            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    if (!materialNames.Contains(material.name))
                    {
                        if (materialNames.Length > 0)
                            materialNames += ", ";

                        materialNames += material.name;
                    }
                }
            }

            Debug.Log(
                $"\nPREFAB: {prefab.name}" +
                $"\nPath: {path}" +
                $"\nBounds size: {(hasBounds ? combinedBounds.size.ToString("F2") : "No renderer")}" +
                $"\nRenderers: {renderers.Length}" +
                $"\nColliders: {colliders.Length}" +
                $"\nMaterials: {(materialNames.Length > 0 ? materialNames : "None")}" +
                "\n----------------------------------------"
            );

            Object.DestroyImmediate(instance);
        }

        Debug.Log("========== ASSET AUDIT FINISHED ==========");
    }
}
