using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class DemonArenaTeleportRepair : MonoBehaviour
{
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerName = "player";
    [SerializeField] private string demonName = "Demon Ambush Boss";
    [SerializeField] private string arenaPlayerSpawnName = "Arena Player Spawn";
    [SerializeField] private string arenaDemonSpawnName = "Arena Demon Spawn";
    [SerializeField] private float triggerDistance = 22f;
    [SerializeField] private float fadeSeconds = 0.6f;

    private Transform player;
    private GameObject demon;
    private bool teleporting;
    private bool teleported;
    private float fade;
    private Texture2D texture;

    private void Awake()
    {
        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
    }

    private void Update()
    {
        if (teleporting || teleported)
            return;

        Resolve();

        if (player == null || demon == null || !demon.activeInHierarchy)
            return;

        float distance = Vector3.Distance(player.position, demon.transform.position);

        // Deliberately independent of quest stage.
        // The demon encounter has changed stages several times during development,
        // so proximity to the active Demon Ambush Boss is now the authoritative trigger.
        if (distance <= triggerDistance)
            StartCoroutine(TeleportToArena());
    }

    private void Resolve()
    {
        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null) player = p.transform;
        }

        if (demon == null)
            demon = FindSceneObject(demonName);
    }

    private IEnumerator TeleportToArena()
    {
        teleporting = true;
        Debug.Log("[DemonArenaRepair] Triggered arena teleport.");

        yield return FadeTo(1f);

        if (!Application.CanStreamedLevelBeLoaded(arenaSceneName))
        {
            Debug.LogError(
                "[DemonArenaRepair] DemonArena is not in Build Settings or cannot be loaded.");
            teleporting = false;
            yield return FadeTo(0f);
            yield break;
        }

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (!arena.IsValid() || !arena.isLoaded)
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(arenaSceneName, LoadSceneMode.Additive);

            if (load == null)
            {
                Debug.LogError("[DemonArenaRepair] LoadSceneAsync returned null.");
                teleporting = false;
                yield return FadeTo(0f);
                yield break;
            }

            while (!load.isDone)
                yield return null;

            arena = SceneManager.GetSceneByName(arenaSceneName);
        }

        Transform playerSpawn = FindInScene(arena, arenaPlayerSpawnName);
        Transform demonSpawn = FindInScene(arena, arenaDemonSpawnName);

        if (playerSpawn == null || demonSpawn == null)
        {
            Debug.LogError(
                "[DemonArenaRepair] Missing Arena Player Spawn or Arena Demon Spawn.");
            teleporting = false;
            yield return FadeTo(0f);
            yield break;
        }

        MoveActor(player, playerSpawn.position, playerSpawn.rotation);
        MoveActor(demon.transform, demonSpawn.position, demonSpawn.rotation);

        teleported = true;
        teleporting = false;

        Debug.Log("[DemonArenaRepair] Player and demon teleported to DemonArena.");

        yield return FadeTo(0f);
    }

    private IEnumerator FadeTo(float target)
    {
        float start = fade;
        float time = 0f;

        while (time < fadeSeconds)
        {
            time += Time.unscaledDeltaTime;
            fade = Mathf.Lerp(start, target, Mathf.Clamp01(time / fadeSeconds));
            yield return null;
        }

        fade = target;
    }

    private void OnGUI()
    {
        if (fade <= 0.001f || texture == null)
            return;

        Color old = GUI.color;
        GUI.color = new Color(0.02f, 0f, 0.05f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);
        GUI.color = old;
    }

    private static void MoveActor(
        Transform actor,
        Vector3 position,
        Quaternion rotation)
    {
        CharacterController cc = actor.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent =
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool agentEnabled = agent != null && agent.enabled;
        if (agentEnabled) agent.enabled = false;

        actor.SetPositionAndRotation(position, rotation);

        if (cc != null) cc.enabled = true;
        if (agentEnabled) agent.enabled = true;
    }

    private static Transform FindInScene(Scene scene, string exactName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t =>
                    string.Equals(t.name, exactName, StringComparison.OrdinalIgnoreCase));

            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindSceneObject(string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t =>
                string.Equals(t.name, exactName, StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
