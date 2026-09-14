
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DemonArenaDirectBossTrigger : MonoBehaviour
{
    [SerializeField] string arenaSceneName = "DemonArena";
    [SerializeField] string playerObjectName = "player";
    [SerializeField] string arenaPlayerSpawnName = "Arena Player Spawn";
    [SerializeField] string arenaDemonSpawnName = "Arena Demon Spawn";
    [SerializeField] float triggerDistance = 26f;
    [SerializeField] float fadeDuration = 0.6f;

    Transform player;
    bool started;
    float fade;
    Texture2D tex;

    void Awake()
    {
        tex = new Texture2D(1,1);
        tex.SetPixel(0,0,Color.white);
        tex.Apply();
        Debug.Log("[DemonArenaDirect] Awake on " + gameObject.name);
    }

    void Update()
    {
        if (started) return;

        if (player == null)
        {
            var p = FindSceneObject(playerObjectName);
            if (p != null) player = p.transform;
        }

        if (player == null) return;

        float d = Vector3.Distance(player.position, transform.position);
        if (d <= triggerDistance)
        {
            started = true;
            Debug.Log("[DemonArenaDirect] Triggered at " + d.ToString("0.0") + "m");
            StartCoroutine(Teleport());
        }
    }

    IEnumerator Teleport()
    {
        yield return FadeTo(1f);

        if (!Application.CanStreamedLevelBeLoaded(arenaSceneName))
        {
            Debug.LogError("[DemonArenaDirect] DemonArena cannot be loaded.");
            started = false;
            yield return FadeTo(0f);
            yield break;
        }

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);
        if (!arena.IsValid() || !arena.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(arenaSceneName, LoadSceneMode.Additive);
            while (op != null && !op.isDone) yield return null;
            arena = SceneManager.GetSceneByName(arenaSceneName);
        }

        var ps = FindInScene(arena, arenaPlayerSpawnName);
        var ds = FindInScene(arena, arenaDemonSpawnName);

        if (ps == null || ds == null)
        {
            Debug.LogError("[DemonArenaDirect] Arena spawn points missing.");
            started = false;
            yield return FadeTo(0f);
            yield break;
        }

        MoveActor(player, ps.position, ps.rotation);
        MoveActor(transform, ds.position, ds.rotation);

        Debug.Log("[DemonArenaDirect] TELEPORT COMPLETE");
        yield return FadeTo(0f);
    }

    IEnumerator FadeTo(float target)
    {
        float start = fade, t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            fade = Mathf.Lerp(start, target, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        fade = target;
    }

    void OnGUI()
    {
        if (fade <= .001f || tex == null) return;
        var old = GUI.color;
        GUI.color = new Color(.02f,0f,.05f,fade);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), tex);
        GUI.color = old;
    }

    static void MoveActor(Transform a, Vector3 p, Quaternion r)
    {
        var cc = a.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        var agent = a.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool ae = agent != null && agent.enabled;
        if (ae) agent.enabled = false;

        a.SetPositionAndRotation(p,r);

        if (cc != null) cc.enabled = true;
        if (ae) agent.enabled = true;
    }

    static Transform FindInScene(Scene s, string name)
    {
        if (!s.IsValid() || !s.isLoaded) return null;
        foreach (var root in s.GetRootGameObjects())
        {
            var f = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase));
            if (f != null) return f;
        }
        return null;
    }

    static GameObject FindSceneObject(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(t.name,name,StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
