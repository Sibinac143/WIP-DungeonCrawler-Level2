using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class DemonArenaAttackTeleport : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerName = "player";
    [SerializeField] private string arenaPlayerSpawnName = "Arena Player Spawn";
    [SerializeField] private string arenaDemonSpawnName = "Arena Demon Spawn";
    [SerializeField] private float fadeSeconds = 0.55f;

    private Transform player;
    private Component demonHealth;
    private float initialHealth = -1f;
    private bool triggered;
    private float fadeAlpha;
    private Texture2D fadeTexture;

    private void Awake()
    {
        fadeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fadeTexture.SetPixel(0, 0, Color.white);
        fadeTexture.Apply();

        demonHealth = FindHealth(gameObject);
        initialHealth = ReadHealth(demonHealth);
    }

    private void Update()
    {
        if (triggered)
            return;

        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        if (player == null || demonHealth == null)
            return;

        float current = ReadHealth(demonHealth);

        if (initialHealth >= 0f &&
            current >= 0f &&
            current < initialHealth - 0.01f)
        {
            triggered = true;
            StartCoroutine(TeleportAfterAttack());
        }
    }

    private IEnumerator TeleportAfterAttack()
    {
        // Save the exact main-world position before leaving.
        DemonArenaReturnPoint.Save(player);

        yield return FadeTo(1f);

        if (!Application.CanStreamedLevelBeLoaded(arenaSceneName))
        {
            triggered = false;
            yield return FadeTo(0f);
            yield break;
        }

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (!arena.IsValid() || !arena.isLoaded)
        {
            AsyncOperation load =
                SceneManager.LoadSceneAsync(
                    arenaSceneName,
                    LoadSceneMode.Additive);

            while (load != null && !load.isDone)
                yield return null;

            arena =
                SceneManager.GetSceneByName(arenaSceneName);
        }

        Transform playerSpawn =
            FindInScene(
                arena,
                arenaPlayerSpawnName);

        Transform demonSpawn =
            FindInScene(
                arena,
                arenaDemonSpawnName);

        if (playerSpawn == null || demonSpawn == null)
        {
            triggered = false;
            yield return FadeTo(0f);
            yield break;
        }

        // Restore health taken by the cinematic trigger hit.
        if (initialHealth > 0f)
            WriteHealth(demonHealth, initialHealth);

        MoveActor(
            player,
            playerSpawn.position,
            playerSpawn.rotation);

        MoveActor(
            transform,
            demonSpawn.position,
            demonSpawn.rotation);

        yield return FadeTo(0f);
    }

    private IEnumerator FadeTo(float target)
    {
        float start = fadeAlpha;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;

            fadeAlpha =
                Mathf.Lerp(
                    start,
                    target,
                    fadeSeconds <= 0f
                        ? 1f
                        : Mathf.Clamp01(elapsed / fadeSeconds));

            yield return null;
        }

        fadeAlpha = target;
    }

    private void OnGUI()
    {
        if (fadeTexture == null ||
            fadeAlpha <= 0.001f)
        {
            return;
        }

        Color old = GUI.color;

        GUI.color =
            new Color(
                0.015f,
                0f,
                0.02f,
                fadeAlpha);

        GUI.DrawTexture(
            new Rect(
                0,
                0,
                Screen.width,
                Screen.height),
            fadeTexture);

        GUI.color = old;
    }

    private static Component FindHealth(GameObject actor)
    {
        return actor
            .GetComponents<Component>()
            .FirstOrDefault(c =>
                c != null &&
                c.GetType().Name.IndexOf(
                    "Health",
                    StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static float ReadHealth(Component health)
    {
        if (health == null)
            return -1f;

        Type type = health.GetType();

        foreach (string name in
                 new[]
                 {
                     "CurrentHealth",
                     "currentHealth",
                     "Health",
                     "health"
                 })
        {
            PropertyInfo p =
                type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (p != null && IsNumber(p.PropertyType))
                return Convert.ToSingle(p.GetValue(health));

            FieldInfo f =
                type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (f != null && IsNumber(f.FieldType))
                return Convert.ToSingle(f.GetValue(health));
        }

        return -1f;
    }

    private static void WriteHealth(
        Component health,
        float value)
    {
        if (health == null)
            return;

        Type type = health.GetType();

        foreach (string name in
                 new[]
                 {
                     "CurrentHealth",
                     "currentHealth",
                     "Health",
                     "health"
                 })
        {
            PropertyInfo p =
                type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (p != null &&
                p.CanWrite &&
                IsNumber(p.PropertyType))
            {
                p.SetValue(
                    health,
                    Convert.ChangeType(
                        value,
                        p.PropertyType));

                return;
            }

            FieldInfo f =
                type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            if (f != null &&
                IsNumber(f.FieldType))
            {
                f.SetValue(
                    health,
                    Convert.ChangeType(
                        value,
                        f.FieldType));

                return;
            }
        }
    }

    private static bool IsNumber(Type type)
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
        CharacterController cc =
            actor.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent =
            actor.GetComponent<UnityEngine.AI.NavMeshAgent>();

        bool enabled =
            agent != null &&
            agent.enabled;

        if (enabled)
            agent.enabled = false;

        actor.SetPositionAndRotation(
            position,
            rotation);

        if (cc != null)
            cc.enabled = true;

        if (enabled)
            agent.enabled = true;
    }

    private static Transform FindInScene(
        Scene scene,
        string exactName)
    {
        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            Transform found =
                root
                    .GetComponentsInChildren<Transform>(true)
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
