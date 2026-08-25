using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BlacksmithCaptiveAnimationSetup
{
    private const string Folder = "Assets/Characters/BlacksmithMixamo";
    private const string Generated = "Assets/Generated/BlacksmithCaptive";
    private const string ControllerPath = Generated + "/BlacksmithCaptive.controller";

    [MenuItem("Tools/Dungeon Game/Polish/Build Blacksmith Jail and Tied Animations")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode", "Exit Play Mode first.", "OK");
            return;
        }

        EnsureFolder("Assets/Generated");
        EnsureFolder(Generated);

        string[] paths = AssetDatabase.FindAssets("t:Model", new[] { Folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (paths.Length == 0)
        {
            EditorUtility.DisplayDialog("Missing FBX Files",
                "Copy NPC_Blacksmith@*.fbx into:\n" + Folder, "OK");
            return;
        }

        string basePath = paths.FirstOrDefault(p =>
            Path.GetFileNameWithoutExtension(p)
                .IndexOf("kneeling idle", StringComparison.OrdinalIgnoreCase) >= 0)
            ?? paths[0];

        ImportHumanoid(paths, basePath);

        var clips = paths.ToDictionary(
            p => Path.GetFileNameWithoutExtension(p),
            p => AssetDatabase.LoadAllAssetsAtPath(p)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__")),
            StringComparer.OrdinalIgnoreCase);

        AnimatorController controller = BuildController(clips);

        GameObject prisoner = Find("Imprisoned Key Maker");
        if (prisoner == null)
        {
            EditorUtility.DisplayDialog("Missing Object",
                "Imprisoned Key Maker was not found.", "OK");
            return;
        }

        GameObject oldVisual = prisoner.GetComponentsInChildren<Animator>(true)
            .Select(a => a.gameObject)
            .FirstOrDefault(go => !go.name.Contains("Mixamo"));

        Transform existing = prisoner.transform.Find("Blacksmith Captive Mixamo");
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
        GameObject captive = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (captive == null) captive = UnityEngine.Object.Instantiate(source);

        captive.name = "Blacksmith Captive Mixamo";
        Undo.RegisterCreatedObjectUndo(captive, "Create Captive Blacksmith");
        captive.transform.SetParent(prisoner.transform, false);

        Animator animator = captive.GetComponentInChildren<Animator>(true);
        if (animator == null) animator = captive.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        Transform leftHand = FindBone(captive.transform, "LeftHand");
        Transform rightHand = FindBone(captive.transform, "RightHand");

        GameObject leftAnchor = Anchor(prisoner.transform,
            "Left Wrist Chain Anchor", new Vector3(-0.62f, 1.18f, -0.18f));
        GameObject rightAnchor = Anchor(prisoner.transform,
            "Right Wrist Chain Anchor", new Vector3(0.62f, 1.18f, -0.18f));

        GameObject chain = LoadChain();

        BlacksmithCaptivePresentation presentation =
            prisoner.GetComponent<BlacksmithCaptivePresentation>();
        if (presentation == null)
            presentation = Undo.AddComponent<BlacksmithCaptivePresentation>(prisoner);

        presentation.questManager =
            UnityEngine.Object.FindAnyObjectByType<QuestManager>(FindObjectsInactive.Include);
        presentation.originalVisual = oldVisual;
        presentation.captiveVisual = captive;
        presentation.captiveAnimator = animator;
        presentation.leftHand = leftHand;
        presentation.rightHand = rightHand;
        presentation.leftAnchor = leftAnchor.transform;
        presentation.rightAnchor = rightAnchor.transform;
        presentation.chainPrefab = chain;

        captive.SetActive(false);

        EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = prisoner;

        EditorUtility.DisplayDialog("Built",
            "Jail kneeling, tied idle, and wrist chains were added.", "OK");
    }

    private static void ImportHumanoid(string[] paths, string basePath)
    {
        ModelImporter baseImporter = AssetImporter.GetAtPath(basePath) as ModelImporter;
        baseImporter.animationType = ModelImporterAnimationType.Human;
        baseImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        SetLoop(baseImporter, basePath);
        baseImporter.SaveAndReimport();

        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(basePath)
            .OfType<Avatar>().FirstOrDefault();

        foreach (string path in paths.Where(p => p != basePath))
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            SetLoop(importer, path);
            importer.SaveAndReimport();
        }
    }

    private static void SetLoop(ModelImporter importer, string path)
    {
        bool loop = path.ToLowerInvariant().Contains("idle") ||
                    path.ToLowerInvariant().Contains("hold");
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = loop;
            clip.loopPose = loop;
        }
        importer.clipAnimations = clips;
    }

    private static AnimatorController BuildController(
        System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        machine.states = Array.Empty<ChildAnimatorState>();

        AnimationClip kneel = FindClip(clips, "kneeling idle");
        AnimationClip tied = FindClip(clips, "injured idle")
            ?? FindClip(clips, "hold assailant")
            ?? kneel;
        AnimationClip stand = FindClip(clips, "stand up")
            ?? FindClip(clips, "getting up");

        AnimatorState jail = machine.AddState("Jail Kneeling Idle");
        jail.motion = kneel;
        machine.defaultState = jail;

        AnimatorState tiedState = machine.AddState("Tied Injured Idle");
        tiedState.motion = tied;

        AnimatorState standState = machine.AddState("Stand Up");
        standState.motion = stand;

        return controller;
    }

    private static AnimationClip FindClip(
        System.Collections.Generic.Dictionary<string, AnimationClip> clips,
        string keyword)
    {
        return clips.FirstOrDefault(p =>
            p.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).Value;
    }

    private static GameObject LoadChain()
    {
        foreach (string guid in AssetDatabase.FindAssets("obj_chain_hang_A t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
        }
        return null;
    }

    private static GameObject Anchor(Transform parent, string name, Vector3 position)
    {
        Transform old = parent.Find(name);
        if (old != null)
        {
            old.localPosition = position;
            return old.gameObject;
        }

        GameObject anchor = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(anchor, "Create Chain Anchor");
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = position;
        return anchor;
    }

    private static Transform FindBone(Transform root, string suffix)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.EndsWith(suffix,
                StringComparison.OrdinalIgnoreCase));
    }

    private static GameObject Find(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include)
            .Where(t => t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(t.name, name,
                StringComparison.OrdinalIgnoreCase))?.gameObject;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string folder = path.Substring(slash + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
