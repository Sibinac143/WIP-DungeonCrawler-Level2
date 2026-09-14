using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalDemonArenaStoryBuilder
{
    private const string ArenaPath = "Assets/Scenes/DemonArena.unity";
    private const string MatFolder = "Assets/Generated/FinalDemonArena";
    private static readonly Vector3 ArenaOrigin = new Vector3(5000f, 0f, 5000f);

    [MenuItem("Tools/Dungeon Game/Polish/REBUILD Final Terrifying Demon Arena", priority = 1090)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first.", "OK");
            return;
        }

        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            EditorUtility.DisplayDialog("Open SampleScene",
                "Open Assets/Scenes/SampleScene.unity first.", "OK");
            return;
        }

        GameObject player = FindSceneObject("player");
        GameObject demon = FindSceneObject("Demon Ambush Boss");
        GameObject spiderSource = FindSpiderSource();

        if (player == null || demon == null)
        {
            EditorUtility.DisplayDialog("Actors Missing",
                "Player found: " + (player != null) +
                "\nDemon found: " + (demon != null), "OK");
            return;
        }

        string samplePath = SceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();

        EnsureFolder("Assets/Generated");
        EnsureFolder(MatFolder);

        Material floorMat = MakeLit(
            MatFolder + "/BloodStone.mat",
            new Color(0.055f, 0.025f, 0.03f, 1f),
            0.15f);

        Material wallMat = MakeLit(
            MatFolder + "/BlackStone.mat",
            new Color(0.012f, 0.012f, 0.018f, 1f),
            0.08f);

        Material metalMat = MakeLit(
            MatFolder + "/DarkMetal.mat",
            new Color(0.035f, 0.035f, 0.045f, 1f),
            0.42f);

        Material runeMat = MakeEmission(
            MatFolder + "/BloodRune.mat",
            new Color(0.75f, 0.015f, 0.02f, 1f),
            7f);

        Scene arena = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);

        GameObject root = new GameObject("FINAL_TERRIFYING_DEMON_ARENA");
        root.transform.position = ArenaOrigin;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.021f;
        RenderSettings.fogColor = new Color(0.012f, 0.003f, 0.005f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.015f, 0.02f);
        RenderSettings.reflectionIntensity = 0.08f;

        BuildClearFloor(root.transform, floorMat);
        BuildWalls(root.transform, wallMat, runeMat);
        BuildDangerDecor(root.transform, metalMat, runeMat);
        BuildLighting(root.transform);
        BuildSpawns(root.transform);

        if (spiderSource != null)
            AddSpiders(spiderSource, arena, root.transform);

        EditorSceneManager.SaveScene(arena, ArenaPath);
        AddArenaToBuildSettings();

        EditorSceneManager.OpenScene(samplePath, OpenSceneMode.Single);
        demon = FindSceneObject("Demon Ambush Boss");

        // Remove every old experimental arena trigger so only one trigger remains.
        foreach (Component c in demon.GetComponents<Component>().ToArray())
        {
            if (c == null)
                continue;

            string n = c.GetType().Name;
            if (n == "DemonArenaDirectBossTrigger" ||
                n == "DemonArenaTeleportSequence" ||
                n == "DemonArenaTeleportRepair" ||
                n == "StoryAwareDemonArenaController")
            {
                Undo.DestroyObjectImmediate(c);
            }
        }

        if (demon.GetComponent<DemonArenaAttackTeleport>() == null)
            Undo.AddComponent<DemonArenaAttackTeleport>(demon);

        // Keep fall recovery / boss anchor if already present.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = demon;

        EditorUtility.DisplayDialog(
            "Final Arena Rebuilt",
            "Story trigger fixed:\n" +
            "The demon still knocks the player down and ties the blacksmith in the main world.\n" +
            "Nothing teleports merely because the player approaches.\n\n" +
            "The FIRST HIT on the guarding demon now triggers the arena transition.\n\n" +
            "Arena movement space was rebuilt as one flat open floor with only perimeter colliders.",
            "OK");
    }

    private static void BuildClearFloor(Transform root, Material mat)
    {
        // One large, reliable box collider. No terrain, grass, steps or overlapping floor pieces.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena Main Collision Floor";
        floor.transform.SetParent(root, false);
        floor.transform.localPosition = new Vector3(0f, -1f, 0f);
        floor.transform.localScale = new Vector3(42f, 2f, 42f);
        floor.GetComponent<Renderer>().sharedMaterial = mat;

        // Lower emergency catch floor.
        GameObject catchFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        catchFloor.name = "Arena Emergency Floor";
        catchFloor.transform.SetParent(root, false);
        catchFloor.transform.localPosition = new Vector3(0f, -5f, 0f);
        catchFloor.transform.localScale = new Vector3(48f, 2f, 48f);
        catchFloor.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void BuildWalls(Transform root, Material wallMat, Material runeMat)
    {
        const int segments = 32;
        const float radius = 20f;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Arena Outer Wall " + i.ToString("00");
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = radial * radius + Vector3.up * 5f;
            wall.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            wall.transform.localScale = new Vector3(4.25f, 10f, 1.5f);
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;

            if (i % 4 == 0)
            {
                GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rune.name = "Glowing Blood Rune " + i.ToString("00");
                rune.transform.SetParent(root, false);
                rune.transform.localPosition = radial * 19.15f + Vector3.up * 5f;
                rune.transform.localRotation = wall.transform.localRotation;
                rune.transform.localScale = new Vector3(0.14f, 5.5f, 0.10f);
                rune.GetComponent<Renderer>().sharedMaterial = runeMat;
                UnityEngine.Object.DestroyImmediate(rune.GetComponent<Collider>());
            }
        }
    }

    private static void BuildDangerDecor(Transform root, Material metalMat, Material runeMat)
    {
        // Decorative spikes remain close to the perimeter, leaving the combat center clear.
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spike.name = "Perimeter Spike " + i.ToString("00");
            spike.transform.SetParent(root, false);
            spike.transform.localPosition = radial * 16.8f + Vector3.up * 1.4f;
            spike.transform.localScale = new Vector3(0.45f, 2.8f, 0.45f);
            spike.GetComponent<Renderer>().sharedMaterial = metalMat;

            // Decorations do NOT block player movement.
            UnityEngine.Object.DestroyImmediate(spike.GetComponent<Collider>());
        }

        // Ritual rune disc in the center - visual only.
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Blood Ritual Circle";
        disc.transform.SetParent(root, false);
        disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        disc.transform.localScale = new Vector3(5.5f, 0.03f, 5.5f);
        disc.GetComponent<Renderer>().sharedMaterial = runeMat;
        UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());

        // Hanging-feeling chain columns using existing dungeon chain prefab if available.
        GameObject chain = FindChainPrefab();
        if (chain != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                GameObject instance = PrefabUtility.InstantiatePrefab(chain) as GameObject;
                if (instance == null)
                    continue;

                instance.name = "Arena Hanging Chain " + i.ToString("00");
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
                instance.transform.SetParent(root, false);
                instance.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 18.2f,
                    7.5f,
                    Mathf.Sin(angle) * 18.2f);

                foreach (Collider c in instance.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;
            }
        }
    }

    private static void BuildLighting(Transform root)
    {
        GameObject moon = new GameObject("Arena Blood Directional");
        moon.transform.SetParent(root, false);
        moon.transform.localRotation = Quaternion.Euler(55f, -35f, 0f);

        Light directional = moon.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 0.34f;
        directional.color = new Color(0.55f, 0.07f, 0.09f);
        directional.shadows = LightShadows.Soft;
        directional.shadowStrength = 0.8f;

        GameObject red = new GameObject("Arena Blood Glow");
        red.transform.SetParent(root, false);
        red.transform.localPosition = new Vector3(0f, 6f, 0f);

        Light point = red.AddComponent<Light>();
        point.type = LightType.Point;
        point.range = 42f;
        point.intensity = 4.2f;
        point.color = new Color(0.85f, 0.025f, 0.035f);
        point.shadows = LightShadows.None;

        GameObject fill = new GameObject("Arena Purple Fill");
        fill.transform.SetParent(root, false);
        fill.transform.localPosition = new Vector3(0f, 4f, -12f);

        Light purple = fill.AddComponent<Light>();
        purple.type = LightType.Point;
        purple.range = 27f;
        purple.intensity = 2.4f;
        purple.color = new Color(0.25f, 0.03f, 0.5f);
        purple.shadows = LightShadows.None;
    }

    private static void BuildSpawns(Transform root)
    {
        CreateMarker(root, "Arena Player Spawn",
            new Vector3(0f, 1.5f, -14f),
            Quaternion.identity);

        CreateMarker(root, "Arena Demon Spawn",
            new Vector3(0f, 1.3f, 11f),
            Quaternion.Euler(0f, 180f, 0f));

        GameObject respawnRoot = new GameObject("Arena Respawn Points");
        respawnRoot.transform.SetParent(root, false);

        Vector3[] points =
        {
            new Vector3(-13f, 1.5f, -8f),
            new Vector3(13f, 1.5f, -8f),
            new Vector3(-13f, 1.5f, 7f),
            new Vector3(13f, 1.5f, 7f),
            new Vector3(0f, 1.5f, -15f)
        };

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 direction = -points[i];
            direction.y = 0f;

            CreateMarker(
                respawnRoot.transform,
                "Arena Respawn " + (i + 1),
                points[i],
                direction.sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(direction.normalized)
                    : Quaternion.identity);
        }
    }

    private static void AddSpiders(GameObject source, Scene scene, Transform root)
    {
        Vector3[] positions =
        {
            new Vector3(-15f, 0.5f, -1f),
            new Vector3(15f, 0.5f, -1f),
            new Vector3(-11f, 0.5f, 11f),
            new Vector3(11f, 0.5f, 11f),
            new Vector3(-10f, 0.5f, -12f),
            new Vector3(10f, 0.5f, -12f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject spider = UnityEngine.Object.Instantiate(source);
            spider.name = "Arena Recovery Spider " + (i + 1);
            SceneManager.MoveGameObjectToScene(spider, scene);
            spider.transform.SetParent(root, false);
            spider.transform.localPosition = positions[i];
            spider.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
            spider.SetActive(true);

            UnityEngine.AI.NavMeshAgent agent =
                spider.GetComponent<UnityEngine.AI.NavMeshAgent>();

            if (agent != null)
                agent.enabled = false;
        }
    }

    private static GameObject FindSpiderSource()
    {
        GameObject root = FindSceneObject("PHASE_05_RECOVERY_SPIDERS");
        if (root == null)
            return null;

        return root.GetComponentsInChildren<Transform>(true)
            .Select(t => t.gameObject)
            .FirstOrDefault(go =>
                go != root &&
                go.name.IndexOf("Spider", StringComparison.OrdinalIgnoreCase) >= 0 &&
                go.GetComponents<Component>().Any(c =>
                    c != null &&
                    c.GetType().Name.IndexOf("Health", StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static GameObject FindChainPrefab()
    {
        foreach (string guid in AssetDatabase.FindAssets("obj_chain_hang_A t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private static GameObject FindSceneObject(string exactName)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(
                t.name,
                exactName,
                StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }

    private static void CreateMarker(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = pos;
        marker.transform.localRotation = rot;
    }

    private static Material MakeLit(string path, Color color, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material MakeEmission(string path, Color color, float strength)
    {
        Material mat = MakeLit(path, color, 0.25f);
        mat.EnableKeyword("_EMISSION");

        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", color * strength);

        return mat;
    }

    private static void AddArenaToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        bool found = false;

        for (int i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(scenes[i].path, ArenaPath, StringComparison.OrdinalIgnoreCase))
            {
                scenes[i] = new EditorBuildSettingsScene(ArenaPath, true);
                found = true;
            }
        }

        if (!found)
            scenes.Add(new EditorBuildSettingsScene(ArenaPath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
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
