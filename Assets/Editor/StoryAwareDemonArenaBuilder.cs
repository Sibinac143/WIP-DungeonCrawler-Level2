using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoryAwareDemonArenaBuilder
{
    private const string ArenaPath =
        "Assets/Scenes/DemonArena.unity";

    private const string MaterialFolder =
        "Assets/Generated/StoryDemonArena";

    [MenuItem(
        "Tools/Dungeon Game/Polish/BUILD Final Story Demon Arena",
        priority = 1060)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode first.",
                "OK");
            return;
        }

        Scene current = SceneManager.GetActiveScene();

        if (current.name != "SampleScene")
        {
            EditorUtility.DisplayDialog(
                "Open SampleScene",
                "Open Assets/Scenes/SampleScene.unity first.",
                "OK");
            return;
        }

        GameObject player =
            FindSceneObject("player");

        GameObject demon =
            FindSceneObject("Demon Ambush Boss");

        GameObject spiderSource =
            FindFirstSpider();

        if (player == null || demon == null)
        {
            EditorUtility.DisplayDialog(
                "Required Actors Missing",
                "Player found: " + (player != null) +
                "\nDemon found: " + (demon != null),
                "OK");
            return;
        }

        EditorSceneManager.SaveOpenScenes();

        EnsureFolder("Assets/Generated");
        EnsureFolder(MaterialFolder);

        Material stone =
            MakeMaterial(
                MaterialFolder + "/ArenaStone.mat",
                new Color(0.055f, 0.06f, 0.075f, 1f));

        Material wall =
            MakeMaterial(
                MaterialFolder + "/ArenaWall.mat",
                new Color(0.025f, 0.028f, 0.038f, 1f));

        Material rune =
            MakeEmissive(
                MaterialFolder + "/ArenaRune.mat",
                new Color(0.25f, 0.03f, 0.55f, 1f),
                5.5f);

        string samplePath = current.path;

        Scene arena =
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

        GameObject root =
            new GameObject("FINAL_STORY_DEMON_ARENA");

        RenderSettings.fog = true;
        RenderSettings.fogMode =
            FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.016f;
        RenderSettings.fogColor =
            new Color(0.012f, 0.006f, 0.026f);

        RenderSettings.ambientMode =
            UnityEngine.Rendering.AmbientMode.Flat;

        RenderSettings.ambientLight =
            new Color(0.06f, 0.045f, 0.085f);

        CreateFloor(
            root.transform,
            stone);

        CreateClosedWall(
            root.transform,
            wall,
            rune);

        CreateSafetyBoundary(
            root.transform);

        CreatePillars(
            root.transform,
            wall,
            rune);

        CreateLights(
            root.transform);

        CreateMarker(
            root.transform,
            "Arena Player Spawn",
            new Vector3(0f, 1.1f, -13f),
            Quaternion.identity);

        CreateMarker(
            root.transform,
            "Arena Demon Spawn",
            new Vector3(0f, 1.1f, 10f),
            Quaternion.Euler(0f, 180f, 0f));

        GameObject respawnRoot =
            new GameObject("Arena Respawn Points");

        respawnRoot.transform.SetParent(
            root.transform,
            false);

        Vector3[] respawns =
        {
            new Vector3(-13f, 1.1f, -7f),
            new Vector3(13f, 1.1f, -7f),
            new Vector3(-13f, 1.1f, 5f),
            new Vector3(13f, 1.1f, 5f),
            new Vector3(0f, 1.1f, -14f)
        };

        for (int i = 0; i < respawns.Length; i++)
        {
            CreateMarker(
                respawnRoot.transform,
                "Arena Respawn " + (i + 1),
                respawns[i],
                Quaternion.LookRotation(
                    Vector3.zero - respawns[i]));
        }

        if (spiderSource != null)
            CreateArenaSpiders(
                spiderSource,
                arena,
                root.transform);

        EditorSceneManager.SaveScene(
            arena,
            ArenaPath);

        AddArenaToBuildSettings();

        EditorSceneManager.OpenScene(
            samplePath,
            OpenSceneMode.Single);

        demon =
            FindSceneObject("Demon Ambush Boss");

        foreach (Component c in
                 demon.GetComponents<Component>())
        {
            if (c == null)
                continue;

            string n = c.GetType().Name;

            if (n == "DemonArenaDirectBossTrigger" ||
                n == "DemonArenaTeleportSequence" ||
                n == "DemonArenaTeleportRepair")
            {
                Undo.DestroyObjectImmediate(c);
            }
        }

        if (demon.GetComponent<StoryAwareDemonArenaController>() == null)
            Undo.AddComponent<StoryAwareDemonArenaController>(demon);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject =
            demon;

        EditorUtility.DisplayDialog(
            "Final Story Arena Built",
            "The arena is now fully enclosed with a solid floor and collision walls.\n\n" +
            "The demon controls the teleport after the player approaches.\n" +
            "Arena deaths respawn the player inside the arena, far from the demon.\n" +
            "Spider recovery enemies were copied into the arena when an existing spider source was found.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Polish/VERIFY Final Story Demon Arena")]
    public static void Verify()
    {
        GameObject player =
            FindSceneObject("player");

        GameObject demon =
            FindSceneObject("Demon Ambush Boss");

        bool controller =
            demon != null &&
            demon.GetComponent<StoryAwareDemonArenaController>() != null;

        bool sceneExists =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(
                ArenaPath) != null;

        bool enabled =
            EditorBuildSettings.scenes.Any(s =>
                s.enabled &&
                string.Equals(
                    s.path,
                    ArenaPath,
                    StringComparison.OrdinalIgnoreCase));

        EditorUtility.DisplayDialog(
            "Final Story Arena Verification",
            "Scene: " +
            SceneManager.GetActiveScene().name +
            "\nPlayer found: " +
            (player != null) +
            "\nDemon found: " +
            (demon != null) +
            "\nStory arena controller: " +
            controller +
            "\nDemonArena exists: " +
            sceneExists +
            "\nDemonArena enabled: " +
            enabled,
            "OK");
    }

    private static void CreateFloor(
        Transform parent,
        Material material)
    {
        GameObject floor =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);

        floor.name =
            "Solid Arena Floor";

        floor.transform.SetParent(
            parent,
            false);

        floor.transform.position =
            new Vector3(0f, -0.75f, 0f);

        floor.transform.localScale =
            new Vector3(19f, 0.75f, 19f);

        floor.GetComponent<Renderer>()
            .sharedMaterial = material;
    }

    private static void CreateClosedWall(
        Transform parent,
        Material material,
        Material runeMaterial)
    {
        const int segments = 40;
        const float radius = 18.5f;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i * Mathf.PI * 2f / segments;

            Vector3 radial =
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle));

            GameObject wall =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);

            wall.name =
                "Sealed Arena Wall " +
                i.ToString("00");

            wall.transform.SetParent(
                parent,
                false);

            wall.transform.position =
                radial * radius +
                Vector3.up * 4.4f;

            wall.transform.rotation =
                Quaternion.Euler(
                    0f,
                    -angle * Mathf.Rad2Deg,
                    0f);

            wall.transform.localScale =
                new Vector3(
                    3.25f,
                    9f,
                    1.3f);

            wall.GetComponent<Renderer>()
                .sharedMaterial = material;

            if (i % 5 == 0)
            {
                GameObject rune =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                rune.name =
                    "Arena Wall Rune " +
                    i.ToString("00");

                rune.transform.SetParent(
                    parent,
                    false);

                rune.transform.position =
                    radial * 17.78f +
                    Vector3.up * 4.5f;

                rune.transform.rotation =
                    wall.transform.rotation;

                rune.transform.localScale =
                    new Vector3(
                        0.18f,
                        4.8f,
                        0.12f);

                rune.GetComponent<Renderer>()
                    .sharedMaterial = runeMaterial;

                UnityEngine.Object
                    .DestroyImmediate(
                        rune.GetComponent<Collider>());
            }
        }
    }

    private static void CreateSafetyBoundary(
        Transform parent)
    {
        GameObject safety =
            new GameObject(
                "Invisible Arena Safety Boundary");

        safety.transform.SetParent(
            parent,
            false);

        for (int i = 0; i < 24; i++)
        {
            float angle =
                i * Mathf.PI * 2f / 24f;

            GameObject wall =
                new GameObject(
                    "Safety Collider " +
                    i.ToString("00"));

            wall.transform.SetParent(
                safety.transform,
                false);

            wall.transform.position =
                new Vector3(
                    Mathf.Cos(angle) * 17.2f,
                    2.5f,
                    Mathf.Sin(angle) * 17.2f);

            wall.transform.rotation =
                Quaternion.Euler(
                    0f,
                    -angle * Mathf.Rad2Deg,
                    0f);

            BoxCollider collider =
                wall.AddComponent<BoxCollider>();

            collider.size =
                new Vector3(
                    4.8f,
                    7f,
                    1.5f);
        }
    }

    private static void CreatePillars(
        Transform parent,
        Material material,
        Material rune)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle =
                i * Mathf.PI * 2f / 8f;

            Vector3 p =
                new Vector3(
                    Mathf.Cos(angle) * 14f,
                    2.6f,
                    Mathf.Sin(angle) * 14f);

            GameObject pillar =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);

            pillar.name =
                "Arena Pillar " +
                i.ToString("00");

            pillar.transform.SetParent(
                parent,
                false);

            pillar.transform.position = p;

            pillar.transform.localScale =
                new Vector3(
                    1.2f,
                    3.2f,
                    1.2f);

            pillar.GetComponent<Renderer>()
                .sharedMaterial = material;

            GameObject orb =
                GameObject.CreatePrimitive(
                    PrimitiveType.Sphere);

            orb.name =
                "Arena Rune Orb " +
                i.ToString("00");

            orb.transform.SetParent(
                parent,
                false);

            orb.transform.position =
                p + Vector3.up * 3.8f;

            orb.transform.localScale =
                Vector3.one * 0.45f;

            orb.GetComponent<Renderer>()
                .sharedMaterial = rune;

            UnityEngine.Object
                .DestroyImmediate(
                    orb.GetComponent<Collider>());
        }
    }

    private static void CreateLights(
        Transform parent)
    {
        GameObject directionalObject =
            new GameObject(
                "Arena Directional Light");

        directionalObject.transform.SetParent(
            parent,
            false);

        directionalObject.transform.rotation =
            Quaternion.Euler(
                48f,
                -30f,
                0f);

        Light directional =
            directionalObject.AddComponent<Light>();

        directional.type =
            LightType.Directional;

        directional.intensity = 0.48f;

        directional.color =
            new Color(
                0.58f,
                0.17f,
                0.28f);

        directional.shadows =
            LightShadows.Soft;

        GameObject pointObject =
            new GameObject(
                "Arena Purple Light");

        pointObject.transform.SetParent(
            parent,
            false);

        pointObject.transform.position =
            new Vector3(
                0f,
                7f,
                0f);

        Light point =
            pointObject.AddComponent<Light>();

        point.type =
            LightType.Point;

        point.range = 38f;
        point.intensity = 3.2f;

        point.color =
            new Color(
                0.30f,
                0.06f,
                0.75f);

        point.shadows =
            LightShadows.None;
    }

    private static void CreateArenaSpiders(
        GameObject source,
        Scene arena,
        Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(-12.5f, 0.4f, -1f),
            new Vector3(12.5f, 0.4f, -1f),
            new Vector3(-9f, 0.4f, 10.5f),
            new Vector3(9f, 0.4f, 10.5f),
            new Vector3(-8f, 0.4f, -11f),
            new Vector3(8f, 0.4f, -11f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject spider =
                UnityEngine.Object.Instantiate(
                    source);

            spider.name =
                "Arena Recovery Spider " +
                (i + 1);

            spider.SetActive(true);

            SceneManager.MoveGameObjectToScene(
                spider,
                arena);

            spider.transform.SetParent(
                parent,
                true);

            spider.transform.position =
                positions[i];

            spider.transform.rotation =
                Quaternion.Euler(
                    0f,
                    i * 60f,
                    0f);

            UnityEngine.AI.NavMeshAgent agent =
                spider.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (agent != null)
                agent.enabled = false;
        }
    }

    private static GameObject FindFirstSpider()
    {
        GameObject root =
            FindSceneObject(
                "PHASE_05_RECOVERY_SPIDERS");

        if (root == null)
            return null;

        return root
            .GetComponentsInChildren<Transform>(true)
            .Select(t => t.gameObject)
            .FirstOrDefault(go =>
                go != root &&
                go.name.IndexOf(
                    "Spider",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                go.GetComponents<Component>()
                    .Any(c =>
                        c != null &&
                        c.GetType().Name.IndexOf(
                            "Health",
                            StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static GameObject FindSceneObject(
        string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Where(t =>
                t != null &&
                t.gameObject.scene.IsValid())
            .FirstOrDefault(t =>
                string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }

    private static Material MakeMaterial(
        string path,
        Color color)
    {
        Material mat =
            AssetDatabase.LoadAssetAtPath<Material>(
                path);

        if (mat == null)
        {
            mat =
                new Material(
                    Shader.Find(
                        "Universal Render Pipeline/Lit"));

            AssetDatabase.CreateAsset(
                mat,
                path);
        }

        mat.color = color;

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.22f);

        EditorUtility.SetDirty(mat);

        return mat;
    }

    private static Material MakeEmissive(
        string path,
        Color color,
        float strength)
    {
        Material mat =
            MakeMaterial(
                path,
                color);

        mat.EnableKeyword("_EMISSION");

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor(
                "_EmissionColor",
                color * strength);
        }

        return mat;
    }

    private static void CreateMarker(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject marker =
            new GameObject(name);

        marker.transform.SetParent(
            parent,
            false);

        marker.transform.position =
            position;

        marker.transform.rotation =
            rotation;
    }

    private static void AddArenaToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            EditorBuildSettings.scenes.ToList();

        bool found = false;

        for (int i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(
                    scenes[i].path,
                    ArenaPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                scenes[i] =
                    new EditorBuildSettingsScene(
                        ArenaPath,
                        true);

                found = true;
            }
        }

        if (!found)
        {
            scenes.Add(
                new EditorBuildSettingsScene(
                    ArenaPath,
                    true));
        }

        EditorBuildSettings.scenes =
            scenes.ToArray();
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
