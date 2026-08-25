using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LevelTwoDarkStormSkySetup
{
    private const string RootName = "DARK_STORM_ENVIRONMENT";
    private const string GeneratedFolder = "Assets/Generated/Environment";
    private const string SkyboxMaterialPath = GeneratedFolder + "/StormNightSkybox.mat";
    private const string MoonMaterialPath = GeneratedFolder + "/MoonEmissive.mat";
    private const string ReportPath = GeneratedFolder + "/DarkStormSkyReport.txt";

    [MenuItem(
        "Tools/Dungeon Game/Finalize/Build Dark Storm Sky",
        priority = 920)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Stop Play Mode",
                "Exit Play Mode before building the dark storm sky.",
                "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        DeleteExistingRoot();
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedFolder);

        Scene scene = SceneManager.GetActiveScene();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Dark Storm Environment");

        Transform followTarget = FindFollowTarget();
        Vector3 center = GetSceneCenter();

        Light moonLight = CreateMoonLight(root.transform, center);
        Light flashLight = CreateFlashLight(root.transform, center);
        Transform moon = CreateMoonObject(root.transform, center);

        Material skybox = GetOrCreateStormSkybox();
        ApplyRenderSettings(skybox, moonLight);

        NightStormController controller =
            Undo.AddComponent<NightStormController>(root);

        controller.Configure(
            followTarget,
            moonLight,
            flashLight,
            moon);

        string[] report =
        {
            "LEVEL 2 DARK STORM SKY REPORT",
            "Scene: " + scene.path,
            "Built: " + DateTime.Now,
            "",
            "No external sky asset was required.",
            "Configured a dark night skybox, moon light, emissive moon, ambient fog, and runtime lightning.",
            "Lightning now appears in the sky and sometimes strikes the ground.",
            "Menu: Tools > Dungeon Game > Finalize > Build Dark Storm Sky"
        };

        File.WriteAllLines(ReportPath, report);
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        EditorUtility.DisplayDialog(
            "Dark Storm Sky Built",
            "Level 2 now has a dark stormy night sky, an emissive moon, and random lightning in the sky with occasional land strikes.",
            "OK");
    }

    private static Material GetOrCreateStormSkybox()
    {
        Shader shader = Shader.Find("Skybox/Procedural");

        if (shader == null)
            throw new InvalidOperationException("Skybox/Procedural shader was not found.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        }

        material.name = "StormNightSkybox";

        SetColorIfPropertyExists(material, "_SkyTint", new Color(0.08f, 0.11f, 0.18f, 1f));
        SetColorIfPropertyExists(material, "_GroundColor", new Color(0.012f, 0.014f, 0.02f, 1f));
        SetFloatIfPropertyExists(material, "_Exposure", 0.34f);
        SetFloatIfPropertyExists(material, "_AtmosphereThickness", 0.62f);
        SetFloatIfPropertyExists(material, "_SunSize", 0.02f);
        SetFloatIfPropertyExists(material, "_SunSizeConvergence", 8f);

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyRenderSettings(Material skybox, Light moonLight)
    {
        RenderSettings.skybox = skybox;
        RenderSettings.sun = moonLight;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.045f, 0.05f, 0.065f, 1f);
        RenderSettings.ambientIntensity = 0.95f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f, 0.04f, 0.055f, 1f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0075f;
        RenderSettings.reflectionIntensity = 0.28f;
        DynamicGI.UpdateEnvironment();
    }

    private static Light CreateMoonLight(Transform parent, Vector3 center)
    {
        GameObject lightObject = new GameObject("Moon Light");
        Undo.RegisterCreatedObjectUndo(lightObject, "Create Moon Light");
        Undo.SetTransformParent(lightObject.transform, parent, "Parent Moon Light");
        lightObject.transform.position = center + new Vector3(-60f, 80f, -35f);
        lightObject.transform.rotation = Quaternion.Euler(38f, 320f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.48f, 0.58f, 0.75f, 1f);
        light.intensity = 0.42f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.72f;
        light.shadowBias = 0.08f;
        light.shadowNormalBias = 0.5f;
        return light;
    }

    private static Light CreateFlashLight(Transform parent, Vector3 center)
    {
        GameObject lightObject = new GameObject("Storm Flash Light");
        Undo.RegisterCreatedObjectUndo(lightObject, "Create Storm Flash Light");
        Undo.SetTransformParent(lightObject.transform, parent, "Parent Storm Flash Light");
        lightObject.transform.position = center + Vector3.up * 20f;
        lightObject.transform.rotation = Quaternion.Euler(50f, 45f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.8f, 0.88f, 1f, 1f);
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        return light;
    }

    private static Transform CreateMoonObject(Transform parent, Vector3 center)
    {
        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Storm Moon";
        Undo.RegisterCreatedObjectUndo(moon, "Create Storm Moon");
        Undo.SetTransformParent(moon.transform, parent, "Parent Storm Moon");

        moon.transform.position = center + new Vector3(-180f, 165f, 230f);
        moon.transform.localScale = Vector3.one * 22f;

        Collider collider = moon.GetComponent<Collider>();
        if (collider != null)
            Undo.DestroyObjectImmediate(collider);

        Material moonMaterial = AssetDatabase.LoadAssetAtPath<Material>(MoonMaterialPath);

        if (moonMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            moonMaterial = new Material(shader);
            AssetDatabase.CreateAsset(moonMaterial, MoonMaterialPath);
        }

        moonMaterial.name = "MoonEmissive";

        SetColorIfPropertyExists(moonMaterial, "_BaseColor", new Color(0.82f, 0.86f, 0.96f, 1f));
        SetColorIfPropertyExists(moonMaterial, "_Color", new Color(0.82f, 0.86f, 0.96f, 1f));
        SetColorIfPropertyExists(moonMaterial, "_EmissionColor", new Color(1.1f, 1.2f, 1.45f, 1f));
        EnableEmissionIfPossible(moonMaterial);
        EditorUtility.SetDirty(moonMaterial);

        Renderer renderer = moon.GetComponent<Renderer>();
        renderer.sharedMaterial = moonMaterial;
        EditorUtility.SetDirty(renderer);

        GameObject glow = new GameObject("Moon Glow");
        Undo.RegisterCreatedObjectUndo(glow, "Create Moon Glow");
        Undo.SetTransformParent(glow.transform, moon.transform, "Parent Moon Glow");
        glow.transform.localPosition = Vector3.zero;
        Light point = glow.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = new Color(0.55f, 0.65f, 0.9f, 1f);
        point.intensity = 0.35f;
        point.range = 42f;
        point.shadows = LightShadows.None;

        return moon.transform;
    }

    private static Transform FindFollowTarget()
    {
        GameObject player = FindSceneObject("player") ?? FindSceneObject("Player");

        if (player != null)
            return player.transform;

        Camera camera = Camera.main;
        if (camera != null)
            return camera.transform;

        return null;
    }

    private static Vector3 GetSceneCenter()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 size = terrain.terrainData.size;
            return terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        }

        Transform follow = FindFollowTarget();
        return follow != null ? follow.position : Vector3.zero;
    }

    private static GameObject FindSceneObject(string exactName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

        foreach (Transform transform in transforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
                continue;

            if (string.Equals(transform.name, exactName, StringComparison.OrdinalIgnoreCase))
                return transform.gameObject;
        }

        return null;
    }

    private static void DeleteExistingRoot()
    {
        GameObject existing = FindSceneObject(RootName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
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

    private static void SetColorIfPropertyExists(Material material, string property, Color color)
    {
        if (material.HasProperty(property))
            material.SetColor(property, color);
    }

    private static void SetFloatIfPropertyExists(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    private static void EnableEmissionIfPossible(Material material)
    {
        if (material == null)
            return;

        material.EnableKeyword("_EMISSION");

        if (material.HasProperty("_EmissionColor") &&
            material.GetColor("_EmissionColor").maxColorComponent > 0.001f)
        {
            material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
    }
}
