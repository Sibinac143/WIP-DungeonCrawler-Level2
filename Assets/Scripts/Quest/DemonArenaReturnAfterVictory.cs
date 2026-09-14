using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class DemonArenaReturnAfterVictory : MonoBehaviour
{
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerName = "player";
    [SerializeField] private float victoryDelay = 2.0f;
    [SerializeField] private float fadeSeconds = 0.65f;

    private Component demonHealth;
    private Transform player;
    private bool returning;
    private float fadeAlpha;
    private Texture2D fadeTexture;

    private void Awake()
    {
        demonHealth = FindHealth(gameObject);

        fadeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        fadeTexture.SetPixel(0, 0, Color.white);
        fadeTexture.Apply();
    }

    private void Update()
    {
        if (returning)
            return;

        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (!arena.IsValid() || !arena.isLoaded)
            return;

        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        if (player == null)
            return;

        if (IsDead(demonHealth))
        {
            returning = true;
            StartCoroutine(ReturnToMainWorld());
        }
    }

    private IEnumerator ReturnToMainWorld()
    {
        Debug.Log("[ArenaVictory] Demon defeated.");

        yield return new WaitForSeconds(victoryDelay);
        yield return FadeTo(1f);

        Vector3 position =
            DemonArenaReturnPoint.HasReturnPoint
                ? DemonArenaReturnPoint.Position
                : player.position;

        Quaternion rotation =
            DemonArenaReturnPoint.HasReturnPoint
                ? DemonArenaReturnPoint.Rotation
                : player.rotation;

        MoveActor(player, position, rotation);

        Scene arena =
            SceneManager.GetSceneByName(arenaSceneName);

        if (arena.IsValid() && arena.isLoaded)
        {
            AsyncOperation unload =
                SceneManager.UnloadSceneAsync(arena);

            while (unload != null && !unload.isDone)
                yield return null;
        }

        DemonArenaReturnPoint.Clear();

        yield return FadeTo(0f);

        Debug.Log("[ArenaVictory] Returned player to the blacksmith encounter.");
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
        if (fadeTexture == null || fadeAlpha <= 0.001f)
            return;

        Color old = GUI.color;
        GUI.color = new Color(0.015f, 0f, 0.02f, fadeAlpha);

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            fadeTexture);

        GUI.color = old;
    }

    private static Component FindHealth(GameObject actor)
    {
        return actor.GetComponents<Component>()
            .FirstOrDefault(c =>
                c != null &&
                c.GetType().Name.IndexOf(
                    "Health",
                    StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsDead(Component health)
    {
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

        float value = ReadNumber(
            health,
            "CurrentHealth",
            "currentHealth",
            "Health",
            "health");

        return value >= 0f && value <= 0.01f;
    }

    private static float ReadNumber(
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

            if (p != null && IsNumber(p.PropertyType))
                return Convert.ToSingle(p.GetValue(component));

            FieldInfo f = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (f != null && IsNumber(f.FieldType))
                return Convert.ToSingle(f.GetValue(component));
        }

        return -1f;
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

    private static GameObject FindSceneObject(string exactName)
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

public static class DemonArenaReturnPoint
{
    public static bool HasReturnPoint { get; private set; }
    public static Vector3 Position { get; private set; }
    public static Quaternion Rotation { get; private set; }

    public static void Save(Transform player)
    {
        if (player == null)
            return;

        Position = player.position;
        Rotation = player.rotation;
        HasReturnPoint = true;
    }

    public static void Clear()
    {
        HasReturnPoint = false;
    }
}
