using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class StoryAwareDemonArenaController : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerName = "player";
    [SerializeField] private string playerSpawnName = "Arena Player Spawn";
    [SerializeField] private string demonSpawnName = "Arena Demon Spawn";
    [SerializeField] private string respawnRootName = "Arena Respawn Points";
    [SerializeField] private float triggerDistance = 20f;
    [SerializeField] private float fadeSeconds = 0.65f;

    [Header("Respawn")]
    [SerializeField] private float deathDelay = 2.0f;
    [SerializeField] private float invulnerabilitySeconds = 3.0f;
    [SerializeField] private float minimumRespawnDistanceFromDemon = 15f;

    private Transform player;
    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private bool arenaLoaded;
    private bool sequenceStarted;
    private bool respawning;
    private float fadeAlpha;
    private Texture2D fadeTexture;
    private Component disabledNormalRespawn;

    private void Awake()
    {
        fadeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fadeTexture.SetPixel(0, 0, Color.white);
        fadeTexture.Apply();

        Debug.Log("[StoryArena] Controller active on " + gameObject.name);
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        if (player == null)
            return;

        if (!arenaLoaded)
        {
            if (!sequenceStarted &&
                gameObject.activeInHierarchy &&
                Vector3.Distance(player.position, transform.position) <= triggerDistance)
            {
                sequenceStarted = true;
                StartCoroutine(EnterArena());
            }

            return;
        }

        if (!respawning && ActorIsDead(player.gameObject))
            StartCoroutine(RespawnPlayerInsideArena());

        if (ActorIsDead(gameObject))
            StartCoroutine(ReturnAfterBossDeath());
    }

    private IEnumerator EnterArena()
    {
        Debug.Log("[StoryArena] Demon confrontation reached. Teleporting to arena.");

        returnPosition = player.position;
        returnRotation = player.rotation;

        yield return FadeTo(1f);

        if (!Application.CanStreamedLevelBeLoaded(arenaSceneName))
        {
            Debug.LogError("[StoryArena] DemonArena is not available in Build Settings.");
            sequenceStarted = false;
            yield return FadeTo(0f);
            yield break;
        }

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (!arena.IsValid() || !arena.isLoaded)
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(arenaSceneName, LoadSceneMode.Additive);

            while (load != null && !load.isDone)
                yield return null;

            arena = SceneManager.GetSceneByName(arenaSceneName);
        }

        Transform playerSpawn = FindInScene(arena, playerSpawnName);
        Transform demonSpawn = FindInScene(arena, demonSpawnName);

        if (playerSpawn == null || demonSpawn == null)
        {
            Debug.LogError("[StoryArena] Arena spawn points are missing.");
            sequenceStarted = false;
            yield return FadeTo(0f);
            yield break;
        }

        disabledNormalRespawn =
            player.GetComponents<Component>()
                .FirstOrDefault(c =>
                    c != null &&
                    c.GetType().Name == "PlayerRespawn");

        if (disabledNormalRespawn is Behaviour normalRespawnBehaviour)
            normalRespawnBehaviour.enabled = false;

        MoveActor(player, playerSpawn.position, playerSpawn.rotation);
        MoveActor(transform, demonSpawn.position, demonSpawn.rotation);

        arenaLoaded = true;

        yield return FadeTo(0f);

        Debug.Log("[StoryArena] Arena fight started.");
    }

    private IEnumerator RespawnPlayerInsideArena()
    {
        respawning = true;

        yield return new WaitForSeconds(deathDelay);

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        List<Transform> points = new List<Transform>();

        Transform root = FindInScene(arena, respawnRootName);

        if (root != null)
        {
            foreach (Transform child in root)
                points.Add(child);
        }

        if (points.Count == 0)
        {
            Transform fallback = FindInScene(arena, playerSpawnName);
            if (fallback != null)
                points.Add(fallback);
        }

        Transform chosen = points
            .OrderByDescending(p => Vector3.Distance(p.position, transform.position))
            .FirstOrDefault(p =>
                Vector3.Distance(p.position, transform.position) >=
                minimumRespawnDistanceFromDemon);

        if (chosen == null)
        {
            chosen = points
                .OrderByDescending(p =>
                    Vector3.Distance(p.position, transform.position))
                .FirstOrDefault();
        }

        RestoreHealth(player.gameObject);
        ResetShield(player.gameObject);

        if (chosen != null)
            MoveActor(player, chosen.position, chosen.rotation);

        yield return StartCoroutine(TemporaryInvulnerability());

        respawning = false;

        Debug.Log("[StoryArena] Player respawned inside arena.");
    }

    private IEnumerator TemporaryInvulnerability()
    {
        float timer = invulnerabilitySeconds;

        while (timer > 0f)
        {
            RestoreHealth(player.gameObject);
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ReturnAfterBossDeath()
    {
        if (!arenaLoaded)
            yield break;

        arenaLoaded = false;

        yield return new WaitForSeconds(2f);
        yield return FadeTo(1f);

        RestoreHealth(player.gameObject);
        MoveActor(player, returnPosition, returnRotation);

        if (disabledNormalRespawn is Behaviour normalRespawnBehaviour)
            normalRespawnBehaviour.enabled = true;

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (arena.IsValid() && arena.isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(arena);

            while (unload != null && !unload.isDone)
                yield return null;
        }

        yield return FadeTo(0f);

        Debug.Log("[StoryArena] Demon defeated. Player returned to blacksmith encounter.");
    }

    private IEnumerator FadeTo(float target)
    {
        float start = fadeAlpha;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            fadeAlpha = Mathf.Lerp(
                start,
                target,
                fadeSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeSeconds));

            yield return null;
        }

        fadeAlpha = target;
    }

    private void OnGUI()
    {
        if (fadeTexture == null || fadeAlpha <= 0.001f)
            return;

        Color old = GUI.color;

        GUI.color = new Color(0.025f, 0f, 0.04f, fadeAlpha);

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            fadeTexture);

        GUI.color = old;
    }

    private static bool ActorIsDead(GameObject actor)
    {
        if (actor == null || !actor.activeInHierarchy)
            return true;

        Component health = FindHealthComponent(actor);

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

            if (p != null &&
                p.PropertyType == typeof(bool) &&
                (bool)p.GetValue(health))
            {
                return true;
            }

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null &&
                f.FieldType == typeof(bool) &&
                (bool)f.GetValue(health))
            {
                return true;
            }
        }

        float? value = ReadNumber(
            health,
            "CurrentHealth",
            "currentHealth",
            "Health",
            "health");

        return value.HasValue && value.Value <= 0.01f;
    }

    private static void RestoreHealth(GameObject actor)
    {
        Component health = FindHealthComponent(actor);

        if (health == null)
            return;

        float max =
            ReadNumber(
                health,
                "MaxHealth",
                "maxHealth",
                "MaximumHealth",
                "maximumHealth") ?? 100f;

        if (!WriteNumber(
                health,
                max,
                "CurrentHealth",
                "currentHealth",
                "Health",
                "health"))
        {
            MethodInfo heal = health.GetType().GetMethod(
                "Heal",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (heal != null)
                heal.Invoke(health, new object[] { max });
        }
    }

    private static void ResetShield(GameObject actor)
    {
        Component shield =
            actor.GetComponents<Component>()
                .FirstOrDefault(c =>
                    c != null &&
                    c.GetType().Name.IndexOf(
                        "Shield",
                        StringComparison.OrdinalIgnoreCase) >= 0);

        if (shield == null)
            return;

        WriteNumber(
            shield,
            0f,
            "CurrentShield",
            "currentShield",
            "Shield",
            "shield");
    }

    private static Component FindHealthComponent(GameObject actor)
    {
        return actor
            .GetComponents<Component>()
            .FirstOrDefault(c =>
                c != null &&
                c.GetType().Name.IndexOf(
                    "Health",
                    StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static float? ReadNumber(
        Component component,
        params string[] names)
    {
        Type type = component.GetType();

        foreach (string name in names)
        {
            PropertyInfo p = type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (p != null &&
                IsNumeric(p.PropertyType))
            {
                return Convert.ToSingle(p.GetValue(component));
            }

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null &&
                IsNumeric(f.FieldType))
            {
                return Convert.ToSingle(f.GetValue(component));
            }
        }

        return null;
    }

    private static bool WriteNumber(
        Component component,
        float value,
        params string[] names)
    {
        Type type = component.GetType();

        foreach (string name in names)
        {
            PropertyInfo p = type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (p != null &&
                p.CanWrite &&
                IsNumeric(p.PropertyType))
            {
                p.SetValue(
                    component,
                    Convert.ChangeType(value, p.PropertyType));

                return true;
            }

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null &&
                IsNumeric(f.FieldType))
            {
                f.SetValue(
                    component,
                    Convert.ChangeType(value, f.FieldType));

                return true;
            }
        }

        return false;
    }

    private static bool IsNumeric(Type type)
    {
        return
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(int) ||
            type == typeof(long);
    }

    private static void MoveActor(
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
