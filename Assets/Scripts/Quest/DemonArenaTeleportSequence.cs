using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class DemonArenaTeleportSequence : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerObjectName = "player";
    [SerializeField] private string demonObjectName = "Demon Ambush Boss";
    [SerializeField] private string arenaPlayerSpawnName = "Arena Player Spawn";
    [SerializeField] private string arenaDemonSpawnName = "Arena Demon Spawn";

    [Header("Trigger")]
    [SerializeField] private float startDistance = 18f;
    [SerializeField] private float preTeleportDelay = 0.8f;
    [SerializeField] private float fadeDuration = 0.65f;
    [SerializeField] private float returnDelayAfterDemonDeath = 2.2f;

    [Header("Optional")]
    [SerializeField] private QuestManager questManager;
    [SerializeField] private GameObject blacksmithRoot;

    private Transform player;
    private GameObject demon;
    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private bool running;
    private bool inArena;
    private bool teleportedThisEncounter;
    private float fadeAlpha;
    private Texture2D whiteTexture;

    private void Awake()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (blacksmithRoot == null)
            blacksmithRoot = FindSceneObject("Imprisoned Key Maker");

        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void Update()
    {
        if (running || inArena || teleportedThisEncounter)
            return;

        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager == null)
            return;

        QuestStage stage = questManager.CurrentStage;

        // IMPORTANT:
        // In the current story build the demon can still be alive while the
        // objective has already advanced to FreeKeyMakerFromTree.
        bool demonEncounterActive =
            stage == QuestStage.DefeatDemon ||
            stage == QuestStage.FreeKeyMakerFromTree;

        if (!demonEncounterActive)
            return;

        ResolveActors();

        if (player == null || demon == null || !demon.activeInHierarchy)
            return;

        if (Vector3.Distance(player.position, demon.transform.position) <= startDistance)
            StartCoroutine(BeginArenaSequence());
    }

    private void ResolveActors()
    {
        if (player == null)
        {
            GameObject playerObject = FindSceneObject(playerObjectName);
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (demon == null)
            demon = FindSceneObject(demonObjectName);
    }

    private IEnumerator BeginArenaSequence()
    {
        running = true;
        teleportedThisEncounter = true;

        ResolveActors();

        if (player == null || demon == null)
        {
            running = false;
            teleportedThisEncounter = false;
            yield break;
        }

        returnPosition = player.position;
        returnRotation = player.rotation;

        yield return new WaitForSeconds(preTeleportDelay);
        yield return FadeTo(1f);

        AsyncOperation load =
            SceneManager.LoadSceneAsync(arenaSceneName, LoadSceneMode.Additive);

        if (load == null)
        {
            Debug.LogError("[DemonArena] Could not load " + arenaSceneName);
            yield return FadeTo(0f);
            running = false;
            teleportedThisEncounter = false;
            yield break;
        }

        while (!load.isDone)
            yield return null;

        Scene arenaScene = SceneManager.GetSceneByName(arenaSceneName);

        Transform playerSpawn = FindInScene(arenaScene, arenaPlayerSpawnName);
        Transform demonSpawn = FindInScene(arenaScene, arenaDemonSpawnName);

        if (playerSpawn == null || demonSpawn == null)
        {
            Debug.LogError("[DemonArena] Arena spawn points are missing.");
            yield return FadeTo(0f);
            running = false;
            teleportedThisEncounter = false;
            yield break;
        }

        TeleportActor(player, playerSpawn.position, playerSpawn.rotation);
        TeleportActor(demon.transform, demonSpawn.position, demonSpawn.rotation);

        inArena = true;
        running = false;

        yield return FadeTo(0f);

        StartCoroutine(WatchForDemonDeath());
    }

    private IEnumerator WatchForDemonDeath()
    {
        while (inArena)
        {
            if (DemonIsDead())
            {
                yield return new WaitForSeconds(returnDelayAfterDemonDeath);
                yield return ReturnFromArena();
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    private bool DemonIsDead()
    {
        if (demon == null || !demon.activeInHierarchy)
            return true;

        Component health = demon.GetComponent("CombatHealth");

        if (health == null)
        {
            health = demon.GetComponents<Component>()
                .FirstOrDefault(c =>
                    c != null &&
                    c.GetType().Name.IndexOf(
                        "Health",
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        if (health == null)
            return false;

        Type type = health.GetType();

        foreach (string name in new[] { "IsDead", "isDead", "Dead", "dead" })
        {
            PropertyInfo p = type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (p != null && p.PropertyType == typeof(bool))
                return (bool)p.GetValue(health);

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null && f.FieldType == typeof(bool))
                return (bool)f.GetValue(health);
        }

        foreach (string name in new[]
                 {
                     "CurrentHealth",
                     "currentHealth",
                     "health",
                     "Health"
                 })
        {
            PropertyInfo p = type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (p != null && IsNumeric(p.PropertyType))
                return Convert.ToSingle(p.GetValue(health)) <= 0.01f;

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null && IsNumeric(f.FieldType))
                return Convert.ToSingle(f.GetValue(health)) <= 0.01f;
        }

        return false;
    }

    private IEnumerator ReturnFromArena()
    {
        running = true;

        yield return FadeTo(1f);

        ResolveActors();

        if (player != null)
            TeleportActor(player, returnPosition, returnRotation);

        Scene arenaScene = SceneManager.GetSceneByName(arenaSceneName);

        if (arenaScene.IsValid() && arenaScene.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(arenaScene);

            if (unload != null)
            {
                while (!unload.isDone)
                    yield return null;
            }
        }

        inArena = false;
        running = false;

        yield return FadeTo(0f);
    }

    private IEnumerator FadeTo(float target)
    {
        float start = fadeAlpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                fadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / fadeDuration);

            fadeAlpha = Mathf.Lerp(start, target, t);

            yield return null;
        }

        fadeAlpha = target;
    }

    private void OnGUI()
    {
        if (fadeAlpha <= 0.001f || whiteTexture == null)
            return;

        Color old = GUI.color;

        GUI.color =
            new Color(
                0.025f,
                0f,
                0.045f,
                fadeAlpha);

        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height),
            whiteTexture);

        GUI.color = old;
    }

    private static void TeleportActor(
        Transform actor,
        Vector3 position,
        Quaternion rotation)
    {
        CharacterController controller =
            actor.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        UnityEngine.AI.NavMeshAgent agent =
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool agentWasEnabled =
            agent != null && agent.enabled;

        if (agentWasEnabled)
            agent.enabled = false;

        actor.SetPositionAndRotation(
            position,
            rotation);

        if (controller != null)
            controller.enabled = true;

        if (agentWasEnabled)
            agent.enabled = true;
    }

    private static bool IsNumeric(Type type)
    {
        return
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(int) ||
            type == typeof(long);
    }

    private static Transform FindInScene(
        Scene scene,
        string exactName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found =
                root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t =>
                        string.Equals(
                            t.name,
                            exactName,
                            StringComparison.OrdinalIgnoreCase));

            if (found != null)
                return found;
        }

        return null;
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
}
