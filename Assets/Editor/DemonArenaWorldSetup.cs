using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DemonArenaWorldSetup
{
    const string ArenaPath = "Assets/Scenes/DemonArena.unity";
    const string Gen = "Assets/Generated/DemonArena";

    [MenuItem("Tools/Dungeon Game/Polish/Build Demon Teleport Arena World")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop Play Mode","Exit Play Mode first.","OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets/Generated");
        EnsureFolder(Gen);

        Scene current = SceneManager.GetActiveScene();
        string currentPath = current.path;

        Material stone = Mat(Gen+"/Stone.mat", new Color(0.055f,0.055f,0.07f), false);
        Material dark = Mat(Gen+"/DarkStone.mat", new Color(0.018f,0.018f,0.028f), false);
        Material rune = Mat(Gen+"/Rune.mat", new Color(0.28f,0.03f,0.55f), true);
        Material moon = Mat(Gen+"/Moon.mat", new Color(0.65f,0.06f,0.12f), true);

        Scene arena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("DEMON_ARENA_WORLD");

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.016f;
        RenderSettings.fogColor = new Color(0.01f,0.002f,0.025f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f,0.025f,0.075f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "Arena Platform";
        floor.transform.SetParent(root.transform,false);
        floor.transform.position = new Vector3(0,-0.7f,0);
        floor.transform.localScale = new Vector3(17,0.8f,17);
        floor.GetComponent<Renderer>().sharedMaterial = stone;

        for (int i=0;i<24;i++)
        {
            float a = i*Mathf.PI*2f/24f;
            Vector3 radial = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Arena Wall "+i.ToString("00");
            wall.transform.SetParent(root.transform,false);
            wall.transform.position = radial*17.2f + Vector3.up*2.2f;
            wall.transform.rotation = Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            wall.transform.localScale = new Vector3(4.6f,4.4f,0.9f);
            wall.GetComponent<Renderer>().sharedMaterial = dark;

            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Rune "+i.ToString("00");
            r.transform.SetParent(root.transform,false);
            r.transform.position = radial*16.7f + Vector3.up*2.3f;
            r.transform.rotation = wall.transform.rotation;
            r.transform.localScale = new Vector3(0.12f,2.4f,0.12f);
            r.GetComponent<Renderer>().sharedMaterial = rune;
            UnityEngine.Object.DestroyImmediate(r.GetComponent<Collider>());
        }

        for (int i=0;i<8;i++)
        {
            float a = i*Mathf.PI*2f/8f;
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            p.name = "Ruined Pillar "+i.ToString("00");
            p.transform.SetParent(root.transform,false);
            p.transform.position = new Vector3(Mathf.Cos(a)*13.5f,3f,Mathf.Sin(a)*13.5f);
            p.transform.localScale = new Vector3(1.1f,3.8f+(i%3),1.1f);
            p.GetComponent<Renderer>().sharedMaterial = dark;
        }

        GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObj.name = "Arena Blood Moon";
        moonObj.transform.SetParent(root.transform,false);
        moonObj.transform.position = new Vector3(-42,38,65);
        moonObj.transform.localScale = Vector3.one*18f;
        moonObj.GetComponent<Renderer>().sharedMaterial = moon;
        UnityEngine.Object.DestroyImmediate(moonObj.GetComponent<Collider>());

        GameObject dl = new GameObject("Arena Moon Light");
        dl.transform.SetParent(root.transform,false);
        dl.transform.rotation = Quaternion.Euler(50,-35,0);
        Light d = dl.AddComponent<Light>();
        d.type = LightType.Directional;
        d.intensity = 0.45f;
        d.color = new Color(0.65f,0.15f,0.22f);
        d.shadows = LightShadows.Soft;

        GameObject pl = new GameObject("Arena Rift Light");
        pl.transform.SetParent(root.transform,false);
        pl.transform.position = new Vector3(0,5,0);
        Light l = pl.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 32;
        l.intensity = 3;
        l.color = new Color(0.32f,0.05f,0.8f);
        l.shadows = LightShadows.None;

        Marker(root.transform,"Arena Player Spawn",new Vector3(0,1.2f,-11.5f),Quaternion.identity);
        Marker(root.transform,"Arena Demon Spawn",new Vector3(0,1.2f,10.5f),Quaternion.Euler(0,180,0));

        EditorSceneManager.SaveScene(arena,ArenaPath);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => string.Equals(s.path,ArenaPath,StringComparison.OrdinalIgnoreCase)))
        {
            scenes.Add(new EditorBuildSettingsScene(ArenaPath,true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        if (!string.IsNullOrEmpty(currentPath))
            EditorSceneManager.OpenScene(currentPath,OpenSceneMode.Single);

        GameObject host = Find("PHASE_03_QUEST_SYSTEM");
        if (host == null) host = new GameObject("PHASE_03_QUEST_SYSTEM");

        if (host.GetComponent<DemonArenaTeleportSequence>() == null)
            Undo.AddComponent<DemonArenaTeleportSequence>(host);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Demon Arena Built",
            "DemonArena.unity was created. During Defeat Demon, approaching the demon teleports both player and demon into the arena. After the demon dies, the player returns to the original encounter point.","OK");
    }

    static Material Mat(string path, Color color, bool emissive)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(s);
            AssetDatabase.CreateAsset(m,path);
        }
        m.color = color;
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor",color*5f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Marker(Transform parent,string name,Vector3 pos,Quaternion rot)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent,false);
        g.transform.position = pos;
        g.transform.rotation = rot;
    }

    static GameObject Find(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(x => x != null && x.gameObject.scene.IsValid())
            .FirstOrDefault(x => string.Equals(x.name,name,StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0,slash);
        string folder = path.Substring(slash+1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,folder);
    }
}
