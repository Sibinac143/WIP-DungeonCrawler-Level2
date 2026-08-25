using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ThirdPersonKnightMaterialFixer
{
    private const string CharacterRootName =
        "ThirdPersonCharacterRoot";

    private const string MaterialFolder =
        "Assets/Generated/ThirdPerson/Materials";

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Upgrade Existing Setup - Manual Switch + Fix Pink",
        priority = 309)]
    public static void UpgradeExistingSetup()
    {
        GameObject characterRoot =
            FindSceneObjectIncludingInactive(CharacterRootName);

        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Third-Person Character Not Found",
                "ThirdPersonCharacterRoot was not found in the open scene.\n\n" +
                "Run Build Knight Combat Starter first.",
                "OK");
            return;
        }

        int replaced =
            ConvertCharacterMaterialsToUrp(characterRoot);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = characterRoot;
        EditorGUIUtility.PingObject(characterRoot);

        EditorUtility.DisplayDialog(
            "Third-Person Setup Upgraded",
            "The inactive ThirdPersonCharacterRoot was found successfully.\n\n" +
            "T = switch FPV / TPV\n" +
            "Converted material slots to URP: " +
            replaced +
            "\n\nThe imported source materials were not modified.",
            "OK");
    }

    [MenuItem(
        "Tools/Dungeon Game/Third Person/Fix Pink Knight Materials",
        priority = 313)]
    public static void FixPinkKnightMaterials()
    {
        GameObject characterRoot =
            FindSceneObjectIncludingInactive(CharacterRootName);

        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Third-Person Character Not Found",
                "ThirdPersonCharacterRoot was not found in the open scene.",
                "OK");
            return;
        }

        int replaced =
            ConvertCharacterMaterialsToUrp(characterRoot);

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = characterRoot;
        EditorGUIUtility.PingObject(characterRoot);

        EditorUtility.DisplayDialog(
            "Knight Materials Fixed",
            "Converted material slots to URP: " +
            replaced +
            "\n\nPress Play and press T to reveal the knight in TPV.",
            "OK");
    }

    public static int ConvertCharacterMaterialsToUrp(
        GameObject characterRoot)
    {
        if (characterRoot == null)
            return 0;

        Shader urpShader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (urpShader == null)
        {
            urpShader =
                Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (urpShader == null)
        {
            EditorUtility.DisplayDialog(
                "URP Shader Not Found",
                "Universal Render Pipeline/Lit could not be found.",
                "OK");
            return 0;
        }

        EnsureFolder("Assets/Generated");
        EnsureFolder("Assets/Generated/ThirdPerson");
        EnsureFolder(MaterialFolder);

        int replacementCount = 0;

        Renderer[] renderers =
            characterRoot.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] sourceMaterials =
                renderer.sharedMaterials;

            Material[] converted =
                new Material[sourceMaterials.Length];

            bool changed = false;

            for (int index = 0;
                 index < sourceMaterials.Length;
                 index++)
            {
                Material source = sourceMaterials[index];

                Material destination =
                    GetOrCreateUrpMaterial(
                        source,
                        renderer.name,
                        index,
                        urpShader);

                converted[index] = destination;

                if (destination != source)
                {
                    changed = true;
                    replacementCount++;
                }
            }

            if (!changed)
                continue;

            Undo.RecordObject(
                renderer,
                "Fix Knight URP Materials");

            renderer.sharedMaterials = converted;
            EditorUtility.SetDirty(renderer);
        }

        return replacementCount;
    }

    private static GameObject FindSceneObjectIncludingInactive(
        string exactName)
    {
        Transform[] transforms =
            UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (Transform transform in transforms)
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

    private static Material GetOrCreateUrpMaterial(
        Material source,
        string rendererName,
        int materialIndex,
        Shader urpShader)
    {
        string sourceName =
            source != null
                ? source.name
                : rendererName +
                  "_Missing_" +
                  materialIndex;

        string materialPath =
            MaterialFolder +
            "/" +
            SanitizeFileName(sourceName) +
            "_URP.mat";

        Material destination =
            AssetDatabase.LoadAssetAtPath<Material>(
                materialPath);

        if (destination == null)
        {
            destination =
                new Material(urpShader)
                {
                    name =
                        SanitizeFileName(sourceName) +
                        "_URP"
                };

            AssetDatabase.CreateAsset(
                destination,
                materialPath);
        }
        else
        {
            destination.shader = urpShader;
        }

        CopyMaterialProperties(source, destination);
        EditorUtility.SetDirty(destination);

        return destination;
    }

    private static void CopyMaterialProperties(
        Material source,
        Material destination)
    {
        Color baseColor = Color.white;
        Texture baseMap = null;
        Vector2 scale = Vector2.one;
        Vector2 offset = Vector2.zero;
        float metallic = 0f;
        float smoothness = 0.35f;

        if (source != null)
        {
            if (source.HasProperty("_BaseColor"))
                baseColor = source.GetColor("_BaseColor");
            else if (source.HasProperty("_Color"))
                baseColor = source.GetColor("_Color");

            if (source.HasProperty("_BaseMap"))
            {
                baseMap = source.GetTexture("_BaseMap");
                scale = source.GetTextureScale("_BaseMap");
                offset = source.GetTextureOffset("_BaseMap");
            }
            else if (source.HasProperty("_MainTex"))
            {
                baseMap = source.GetTexture("_MainTex");
                scale = source.GetTextureScale("_MainTex");
                offset = source.GetTextureOffset("_MainTex");
            }
            else
            {
                baseMap = source.mainTexture;
                scale = source.mainTextureScale;
                offset = source.mainTextureOffset;
            }

            if (source.HasProperty("_Metallic"))
                metallic = source.GetFloat("_Metallic");

            if (source.HasProperty("_Smoothness"))
                smoothness = source.GetFloat("_Smoothness");
            else if (source.HasProperty("_Glossiness"))
                smoothness = source.GetFloat("_Glossiness");
        }

        destination.SetColor("_BaseColor", baseColor);
        destination.SetTexture("_BaseMap", baseMap);
        destination.SetTextureScale("_BaseMap", scale);
        destination.SetTextureOffset("_BaseMap", offset);
        destination.SetFloat("_Metallic", metallic);
        destination.SetFloat("_Smoothness", smoothness);

        if (source != null &&
            source.HasProperty("_BumpMap"))
        {
            Texture normal =
                source.GetTexture("_BumpMap");

            if (normal != null)
            {
                destination.SetTexture("_BumpMap", normal);
                destination.EnableKeyword("_NORMALMAP");
            }
        }

        if (source != null &&
            source.HasProperty("_EmissionColor"))
        {
            Color emission =
                source.GetColor("_EmissionColor");

            destination.SetColor("_EmissionColor", emission);

            if (emission.maxColorComponent > 0.001f)
                destination.EnableKeyword("_EMISSION");
        }

        destination.enableInstancing =
            source != null &&
            source.enableInstancing;

        destination.doubleSidedGI =
            source != null &&
            source.doubleSidedGI;
    }

    private static string SanitizeFileName(
        string value)
    {
        foreach (char invalid in
                 Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace('/', '_');
    }

    private static void EnsureFolder(
        string path)
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
