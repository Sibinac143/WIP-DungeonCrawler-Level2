using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixStormMoon
{
    private const string MoonMaterialPath =
        "Assets/Generated/Environment/MoonEmissive.mat";

    private const string SkyboxMaterialPath =
        "Assets/Generated/Environment/StormNightSkybox.mat";

    [MenuItem("Tools/Dungeon Game/Finalize/Fix Storm Moon Permanently")]
    public static void Apply()
    {
        FixMoonMaterial();
        RemoveSmallSkyboxMoon();
        FixMoonRenderer();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();

        Debug.Log(
            "Storm Moon fixed and saved permanently.");

        EditorUtility.DisplayDialog(
            "Storm Moon Fixed",
            "The large moon now uses a permanent blue-white Unlit material. " +
            "The small skybox moon was removed.",
            "OK");
    }

    private static void FixMoonMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MoonMaterialPath);

        if (material == null)
        {
            Debug.LogError(
                "Moon material not found: " +
                MoonMaterialPath);

            return;
        }

        Shader unlitShader =
            Shader.Find(
                "Universal Render Pipeline/Unlit");

        if (unlitShader == null)
        {
            Debug.LogError(
                "URP Unlit shader was not found.");

            return;
        }

        material.shader = unlitShader;

        Color moonColor =
            new Color(
                0.74f,
                0.84f,
                1f,
                1f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", moonColor);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", moonColor);

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 0f);

        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", 2f);

        material.renderQueue = -1;

        EditorUtility.SetDirty(material);
    }

    private static void RemoveSmallSkyboxMoon()
    {
        Material skybox =
            AssetDatabase.LoadAssetAtPath<Material>(
                SkyboxMaterialPath);

        if (skybox == null)
            return;

        if (skybox.HasProperty("_SunSize"))
            skybox.SetFloat("_SunSize", 0f);

        if (skybox.HasProperty(
                "_SunSizeConvergence"))
        {
            skybox.SetFloat(
                "_SunSizeConvergence",
                0f);
        }

        EditorUtility.SetDirty(skybox);
    }

    private static void FixMoonRenderer()
    {
        GameObject moon =
            GameObject.Find("Storm Moon");

        if (moon == null)
        {
            Debug.LogError(
                "Storm Moon was not found in the scene.");

            return;
        }

        MeshRenderer renderer =
            moon.GetComponent<MeshRenderer>();

        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(
                MoonMaterialPath);

        if (renderer != null)
        {
            renderer.sharedMaterial = material;

            renderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            renderer.receiveShadows = false;

            EditorUtility.SetDirty(renderer);
        }

        Transform glow =
            moon.transform.Find("Moon Glow");

        if (glow != null)
        {
            Light glowLight =
                glow.GetComponent<Light>();

            if (glowLight != null)
            {
                glowLight.color =
                    new Color(
                        0.5f,
                        0.68f,
                        1f,
                        1f);

                glowLight.intensity = 1.2f;
                glowLight.range = 55f;
                glowLight.shadows =
                    LightShadows.None;

                EditorUtility.SetDirty(glowLight);
            }
        }
    }
}
