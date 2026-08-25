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

public static class AssetGalleryBuilder
{
    private const string ScenePath = "Assets/Scenes/AssetGallery.unity";
    private const string GeneratedFolder = "Assets/Generated/AssetGallery";
    private const string ReportPath = GeneratedFolder + "/AssetGalleryReport.csv";

    private sealed class Entry
    {
        public string category;
        public string label;
        public string path;
        public string fallback;
        public float yaw;

        public Entry(string c, string l, string p, float y = 0f, string f = null)
        {
            category = c;
            label = l;
            path = p;
            yaw = y;
            fallback = f;
        }
    }

    private sealed class Result
    {
        public string category;
        public string label;
        public string path;
        public string status;
        public Vector3 size;
        public int renderers;
        public int colliders;
        public int animators;
        public int missingScripts;
        public int brokenMaterials;
    }

    private static readonly Entry[] Entries =
    {
        // Village
        new Entry("01 VILLAGE", "Village House", "Assets/Aletheia/Prefabs/house.prefab"),
        new Entry("01 VILLAGE", "Village Mill", "Assets/Aletheia/Prefabs/mill.prefab"),
        new Entry("01 VILLAGE", "Village Well", "Assets/Aletheia/Prefabs/well.prefab"),
        new Entry("01 VILLAGE", "Gallows", "Assets/Aletheia/Prefabs/gallows.prefab"),
        new Entry("01 VILLAGE", "House Door", "Assets/Aletheia/Prefabs/door.prefab"),
        new Entry("01 VILLAGE", "Village Fence", "Assets/Aletheia/Prefabs/fence.prefab"),
        new Entry("01 VILLAGE", "Village Lamp", "Assets/Aletheia/Prefabs/lamp.prefab"),
        new Entry("01 VILLAGE", "Barrel", "Assets/Aletheia/Prefabs/barrel.prefab"),
        new Entry("01 VILLAGE", "Wooden Box", "Assets/Aletheia/Prefabs/box.prefab"),
        new Entry("01 VILLAGE", "Wheelbarrow", "Assets/Aletheia/Prefabs/wheelbarrow.prefab"),

        // Key maker workshop
        new Entry("02 KEY MAKER WORKSHOP", "Forge", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Forge.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Anvil", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Anvil.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Bellows", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Bellows.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Work Table", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Work_Table.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Tool Rack", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Tool_Rack.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Grindstone", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Grindstone.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Quenching Trough", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Quenching_Trough.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Blacksmith Sign", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Blacksmith_Sign.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Ore Crate", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Ore_Crate.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Coal Pile", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Coal_Pile.prefab"),
        new Entry("02 KEY MAKER WORKSHOP", "Glowing Ingot", "Assets/UpDraft Art/Stylized Blacksmith Workshop Kit/Prefabs/Glowing_Ingot.prefab"),

        // Castle and dungeon
        new Entry("03 CASTLE AND DUNGEON", "Decorated Main Gate Arch", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_archPointy_decorated_gate.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Gate Door Left", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_gateDoor_left_LOW.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Gate Door Right", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_gateDoor_right_LOW.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Stone Wall", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_bricks.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Broken Stone Wall", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_bricksBroken.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Stone Floor", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/floor_tiles.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Stone Stairs", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/wall_stairs.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Metal Door Left", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_metalDoor_left_LOW.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Metal Door Right", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesLOW_URP/obj_metalDoor_right_LOW.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Hanging Chain", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chain_hang_A.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Skull Column", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_columnSkull_large.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Closed Chest", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chest_close.prefab"),
        new Entry("03 CASTLE AND DUNGEON", "Candle Group", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_candleGroup_A.prefab"),

        // Story props
        new Entry("04 STORY PROPS", "Quest Scroll / Letter", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_scroll.prefab"),
        new Entry("04 STORY PROPS", "Main Gate Key", "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_09_Key_002.prefab"),
        new Entry("04 STORY PROPS", "Player Torch", "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_10_Torch_001.prefab"),
        new Entry("04 STORY PROPS", "Player Sword", "Assets/PurePoly/Free Fantasy RPG Weapons/Prefabs/PP_Theme_11_Sword_One-Handed_003.prefab"),
        new Entry("04 STORY PROPS", "Open Key Box Body", "Assets/ZerinLabs_lowpolyPack_ModularDungeons/prefabs/modularDungeonPiecesHIGH_URP/obj_chestOpen_body.prefab"),

        // Characters
        new Entry("05 CHARACTERS", "Male Peasant / Key Maker", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Male_Peasant_01.prefab"),
        new Entry("05 CHARACTERS", "Female Peasant A", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Female_Peasant_01_a.prefab"),
        new Entry("05 CHARACTERS", "Female Peasant B", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Female_Peasant_01_b.prefab"),
        new Entry("05 CHARACTERS", "Boy Peasant", "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/Sets/PT_Boy_Peasant_01.prefab"),

        // Wyvern
        new Entry(
            "06 KUZAR WYVERN",
            "Kuzar Poly Art",
            "Assets/Malbers Animations/Dragons/8 - Kuzar the Magnificent/Model/Kuzar the Magnificent Poly Art.prefab",
            180f,
            "Assets/Malbers Animations/Dragons/8 - Kuzar the Magnificent/Model/Kuzar the Magnificent Poly Art.fbx"),
        new Entry(
            "06 KUZAR WYVERN",
            "Kuzar Realistic",
            "Assets/Malbers Animations/Dragons/8 - Kuzar the Magnificent/Model/Kuzar the Magnificent.prefab",
            180f,
            "Assets/Malbers Animations/Dragons/8 - Kuzar the Magnificent/Model/Kuzar the Magnificent.fbx")
    };

    [MenuItem("Tools/Dungeon Game/Asset Gallery/Create or Rebuild Gallery")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("ASSET_GALLERY_GENERATED");
        Transform itemsRoot = NewGroup("01_ORIGINAL_SCALE_ASSETS", root.transform);
        Transform helpersRoot = NewGroup("02_LABELS_AND_HELPERS", root.transform);

        CreateLighting(root.transform);
        CreateCamera(root.transform);

        var results = new List<Result>();
        var boundsList = new List<Bounds>();
        float z = 0f;

        foreach (IGrouping<string, Entry> group in Entries.GroupBy(e => e.category))
            z = BuildCategory(group.Key, group.ToArray(), z, itemsRoot, helpersRoot, results, boundsList);

        Bounds galleryBounds = Combine(boundsList);
        CreateFloor(galleryBounds, root.transform);
        CreateText(
            "ASSET GALLERY - ORIGINAL PREFAB SCALE\n" +
            "Scene View: select an item and press F\n" +
            "Play: WASD move | Q/E vertical | hold RMB to look | Shift faster | wheel changes speed",
            new Vector3(galleryBounds.min.x, 4f, galleryBounds.min.z - 10f),
            0.2f,
            TextAnchor.MiddleLeft,
            helpersRoot);

        PositionCamera(galleryBounds);
        WriteReport(results);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Frame(galleryBounds, false);

        int missing = results.Count(r => r.status != "OK");

        EditorUtility.DisplayDialog(
            "Asset Gallery Complete",
            "Scene: " + ScenePath +
            "\nReport: " + ReportPath +
            "\nMissing/failed items: " + missing,
            "OK");
    }

    [MenuItem("Tools/Dungeon Game/Asset Gallery/Open Gallery Scene")]
    public static void OpenGallery()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Gallery Missing", "Create it first.", "OK");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Dungeon Game/Asset Gallery/Select CSV Report")]
    public static void SelectReport()
    {
        UnityEngine.Object report = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ReportPath);
        if (report == null) return;
        Selection.activeObject = report;
        EditorGUIUtility.PingObject(report);
    }

    private static float BuildCategory(
        string category,
        Entry[] entries,
        float startZ,
        Transform itemsRoot,
        Transform helpersRoot,
        List<Result> results,
        List<Bounds> boundsList)
    {
        Transform categoryRoot = NewGroup(category, itemsRoot);
        CreateText(category, new Vector3(-7f, 3f, startZ - 5f), 0.25f, TextAnchor.MiddleLeft, helpersRoot);
        CreateScaleReference(new Vector3(-4f, 0f, startZ), helpersRoot);

        float x = 0f;
        float rowZ = startZ;
        float rowDepth = 3f;
        int rowCount = 0;

        foreach (Entry e in entries)
        {
            string usedPath = e.path;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(usedPath);

            if (prefab == null && !string.IsNullOrEmpty(e.fallback))
            {
                usedPath = e.fallback;
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(usedPath);
            }

            if (prefab == null)
            {
                CreateMissingMarker(e.label, e.path, new Vector3(x + 2f, 0f, rowZ), helpersRoot);
                results.Add(new Result { category = category, label = e.label, path = e.path, status = "MISSING" });
                x += 8f;
                rowCount++;
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;

            if (instance == null)
            {
                results.Add(new Result { category = category, label = e.label, path = usedPath, status = "INSTANTIATION FAILED" });
                continue;
            }

            instance.name = e.label;
            instance.transform.SetParent(categoryRoot);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.Euler(0f, e.yaw, 0f);

            FreezeForGallery(instance);

            Bounds initial;
            if (!TryBounds(instance, out initial))
                initial = new Bounds(Vector3.zero, Vector3.one);

            float width = Mathf.Max(1.5f, initial.size.x);
            float depth = Mathf.Max(1.5f, initial.size.z);

            if (rowCount >= 6 || (rowCount > 0 && x + width > 82f))
            {
                rowZ += rowDepth + 14f;
                x = 0f;
                rowDepth = 3f;
                rowCount = 0;
            }

            Vector3 targetCenter = new Vector3(x + width * 0.5f, initial.center.y, rowZ);
            Vector3 correction = targetCenter - initial.center;
            correction.y = -initial.min.y;
            instance.transform.position += correction;

            Bounds finalBounds;
            if (!TryBounds(instance, out finalBounds))
                finalBounds = new Bounds(instance.transform.position, Vector3.one);

            int rendererCount = instance.GetComponentsInChildren<Renderer>(true).Length;
            int colliderCount = instance.GetComponentsInChildren<Collider>(true).Length;
            int animatorCount = instance.GetComponentsInChildren<Animator>(true).Length;
            int missingScripts = CountMissingScripts(instance);
            int brokenMaterials = CountBrokenMaterials(instance);

            AssetGalleryItemInfo info = instance.AddComponent<AssetGalleryItemInfo>();
            info.sourcePath = usedPath;
            info.category = category;
            info.measuredWorldSize = finalBounds.size;
            info.rendererCount = rendererCount;
            info.colliderCount = colliderCount;
            info.animatorCount = animatorCount;
            info.missingScriptCount = missingScripts;
            info.brokenMaterialSlotCount = brokenMaterials;

            CreatePad(finalBounds, helpersRoot);
            CreateItemLabel(e.label, finalBounds, rendererCount, colliderCount, animatorCount, missingScripts, brokenMaterials, helpersRoot);

            results.Add(new Result
            {
                category = category,
                label = e.label,
                path = usedPath,
                status = "OK",
                size = finalBounds.size,
                renderers = rendererCount,
                colliders = colliderCount,
                animators = animatorCount,
                missingScripts = missingScripts,
                brokenMaterials = brokenMaterials
            });

            boundsList.Add(finalBounds);
            x += width + 5f;
            rowDepth = Mathf.Max(rowDepth, depth);
            rowCount++;
        }

        return rowZ + rowDepth + 22f;
    }

    private static void FreezeForGallery(GameObject go)
    {
        foreach (Animator a in go.GetComponentsInChildren<Animator>(true))
            a.enabled = false;

        foreach (Rigidbody r in go.GetComponentsInChildren<Rigidbody>(true))
        {
            r.isKinematic = true;
            r.useGravity = false;
        }

        foreach (ParticleSystem p in go.GetComponentsInChildren<ParticleSystem>(true))
            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private static void CreateLighting(Transform parent)
    {
        GameObject go = new GameObject("Gallery Directional Light");
        go.transform.SetParent(parent);
        go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;

        Material sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/AllSkyFree/Cold Night/Cold Night.mat");
        if (sky == null)
            sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/AllSkyFree/Deep Dusk/Deep Dusk.mat");
        if (sky != null)
            RenderSettings.skybox = sky;

        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.fog = false;
    }

    private static void CreateCamera(Transform parent)
    {
        GameObject go = new GameObject("Gallery Free Camera");
        go.transform.SetParent(parent);

        Camera camera = go.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 1000f;

        go.AddComponent<AudioListener>();
        go.AddComponent<GalleryFreeCamera>();
    }

    private static void PositionCamera(Bounds b)
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 target = new Vector3(b.center.x, Mathf.Max(3f, b.size.y * 0.25f), b.center.z);
        camera.transform.position = new Vector3(b.center.x, Mathf.Max(22f, b.size.y * 1.4f), b.min.z - 42f);
        camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
    }

    private static void CreateFloor(Bounds b, Transform parent)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Gallery Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(b.center.x, -0.15f, b.center.z);
        floor.transform.localScale = new Vector3(Mathf.Max(120f, b.size.x + 26f), 0.25f, Mathf.Max(120f, b.size.z + 36f));
        floor.GetComponent<Renderer>().sharedMaterial =
            MaterialAt(GeneratedFolder + "/GalleryFloor.mat", new Color(0.075f, 0.085f, 0.11f));
        floor.transform.SetSiblingIndex(0);
    }

    private static void CreatePad(Bounds b, Transform parent)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Display Pad";
        pad.transform.SetParent(parent);
        pad.transform.position = new Vector3(b.center.x, 0.015f, b.center.z);
        pad.transform.localScale = new Vector3(Mathf.Max(1.5f, b.size.x + 0.8f), 0.03f, Mathf.Max(1.5f, b.size.z + 0.8f));
        pad.GetComponent<Renderer>().sharedMaterial =
            MaterialAt(GeneratedFolder + "/GalleryPad.mat", new Color(0.18f, 0.2f, 0.24f));
    }

    private static void CreateScaleReference(Vector3 p, Transform parent)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "1.8m Scale Reference";
        capsule.transform.SetParent(parent);
        capsule.transform.position = p + Vector3.up * 0.9f;
        capsule.transform.localScale = new Vector3(0.45f, 0.9f, 0.45f);
        capsule.GetComponent<Renderer>().sharedMaterial =
            MaterialAt(GeneratedFolder + "/ScaleReference.mat", new Color(0.58f, 0.62f, 0.68f));
        CreateText("1.8 m\nscale", p + new Vector3(0f, 2.25f, -0.35f), 0.13f, TextAnchor.MiddleCenter, parent);
    }

    private static void CreateItemLabel(
        string name,
        Bounds b,
        int r,
        int c,
        int a,
        int missing,
        int broken,
        Transform parent)
    {
        string text =
            name +
            "\nSize: " + b.size.x.ToString("0.00") + " x " +
            b.size.y.ToString("0.00") + " x " +
            b.size.z.ToString("0.00") + " m" +
            "\nR:" + r + " C:" + c + " A:" + a +
            " Missing scripts:" + missing +
            " Broken mats:" + broken;

        CreateText(
            text,
            new Vector3(b.center.x, Mathf.Max(0.35f, b.min.y + 0.35f), b.min.z - 1.35f),
            0.095f,
            TextAnchor.MiddleCenter,
            parent);
    }

    private static void CreateMissingMarker(string name, string path, Vector3 p, Transform parent)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "MISSING_" + name;
        cube.transform.SetParent(parent);
        cube.transform.position = p + Vector3.up;
        cube.transform.localScale = Vector3.one * 2f;
        cube.GetComponent<Renderer>().sharedMaterial =
            MaterialAt(GeneratedFolder + "/MissingAsset.mat", new Color(0.65f, 0.08f, 0.08f));
        CreateText("MISSING\n" + name + "\n" + path, p + Vector3.up * 2.8f, 0.1f, TextAnchor.MiddleCenter, parent);
    }

    private static void CreateText(string text, Vector3 p, float size, TextAnchor anchor, Transform parent)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent);
        go.transform.position = p;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = anchor;
        tm.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left : TextAlignment.Center;
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.color = Color.white;
    }

    private static Transform NewGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    private static bool TryBounds(GameObject go, out Bounds b)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            b = default;
            return false;
        }

        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static Bounds Combine(List<Bounds> list)
    {
        if (list.Count == 0)
            return new Bounds(Vector3.zero, new Vector3(100f, 20f, 100f));

        Bounds b = list[0];
        for (int i = 1; i < list.Count; i++)
            b.Encapsulate(list[i]);
        return b;
    }

    private static int CountMissingScripts(GameObject root)
    {
        int count = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
        return count;
    }

    private static int CountBrokenMaterials(GameObject root)
    {
        int count = 0;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null ||
                    material.shader == null ||
                    material.shader.name.Contains("Hidden/InternalErrorShader"))
                    count++;
            }
        }
        return count;
    }

    private static Material MaterialAt(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static void WriteReport(List<Result> results)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Category,Name,Status,Source Path,Size X,Size Y,Size Z,Renderers,Colliders,Animators,Missing Scripts,Broken Material Slots");

        foreach (Result r in results)
        {
            csv.Append(Q(r.category)).Append(',');
            csv.Append(Q(r.label)).Append(',');
            csv.Append(Q(r.status)).Append(',');
            csv.Append(Q(r.path)).Append(',');
            csv.Append(r.size.x.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(r.size.y.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(r.size.z.ToString("0.000", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(r.renderers).Append(',');
            csv.Append(r.colliders).Append(',');
            csv.Append(r.animators).Append(',');
            csv.Append(r.missingScripts).Append(',');
            csv.Append(r.brokenMaterials).AppendLine();
        }

        File.WriteAllText(ReportPath, csv.ToString());
        AssetDatabase.ImportAsset(ReportPath);
    }

    private static string Q(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);

        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, name);
    }
}
