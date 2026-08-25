using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatHUD : MonoBehaviour
{
    [SerializeField] private CombatHealth playerHealth;
    [SerializeField] private PlayerShield playerShield;

    private string hitMessage = string.Empty;
    private float hitMessageUntil;

    public void Configure(CombatHealth healthReference)
    {
        playerHealth = healthReference;
        playerShield =
            playerHealth != null
                ? playerHealth.GetComponent<PlayerShield>()
                : null;
    }

    public void Configure(
        CombatHealth healthReference,
        PlayerShield shieldReference)
    {
        playerHealth = healthReference;
        playerShield = shieldReference;
    }

    private void OnEnable()
    {
        CombatSignals.HitConfirmed += HandleHitConfirmed;
    }

    private void OnDisable()
    {
        CombatSignals.HitConfirmed -= HandleHitConfirmed;
    }

    private void Update()
    {
        if (playerHealth == null)
            playerHealth = FindPlayerHealth();

        if (playerShield == null && playerHealth != null)
            playerShield = playerHealth.GetComponent<PlayerShield>();
    }

    private void HandleHitConfirmed(
        string targetName,
        float damage,
        bool killed)
    {
        hitMessage =
            killed
                ? targetName + " defeated"
                : targetName + "  -" +
                  Mathf.RoundToInt(damage) +
                  " HP";

        hitMessageUntil = Time.time + 1.2f;
    }

    private void OnGUI()
    {
        DrawPlayerStatus();
        DrawCrosshair();
        DrawControls();

        if (Time.time < hitMessageUntil)
        {
            GUI.Box(
                new Rect(
                    Screen.width * 0.5f - 110f,
                    Screen.height * 0.58f,
                    220f,
                    28f),
                hitMessage);
        }

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            GUI.Box(
                new Rect(
                    Screen.width * 0.5f - 150f,
                    Screen.height * 0.45f,
                    300f,
                    45f),
                "YOU DIED\nReturning to the sanctuary...");
        }
    }

    private void DrawPlayerStatus()
    {
        if (playerHealth == null)
            return;

        float panelHeight =
            playerShield != null
                ? 98f
                : 70f;

        Rect panel =
            new Rect(
                18f,
                18f,
                280f,
                panelHeight);

        GUI.Box(panel, "PLAYER");

        DrawBar(
            new Rect(34f, 50f, 245f, 18f),
            playerHealth.NormalizedHealth,
            new Color(0.12f, 0.02f, 0.02f, 0.95f),
            new Color(0.75f, 0.08f, 0.08f, 0.95f),
            Mathf.CeilToInt(playerHealth.CurrentHealth) +
            " / " +
            Mathf.CeilToInt(playerHealth.MaxHealth));

        if (playerShield == null)
            return;

        DrawBar(
            new Rect(34f, 76f, 245f, 16f),
            playerShield.NormalizedShield,
            new Color(0.01f, 0.06f, 0.13f, 0.95f),
            new Color(0.05f, 0.42f, 0.92f, 0.95f),
            "SHIELD  " +
            Mathf.CeilToInt(playerShield.CurrentShield) +
            " / " +
            Mathf.CeilToInt(playerShield.MaxShield));
    }

    private static void DrawBar(
        Rect background,
        float normalized,
        Color backgroundColor,
        Color fillColor,
        string label)
    {
        Color original = GUI.color;

        GUI.color = backgroundColor;
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = fillColor;
        GUI.DrawTexture(
            new Rect(
                background.x,
                background.y,
                background.width * Mathf.Clamp01(normalized),
                background.height),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(background, label);
        GUI.color = original;
    }

    private static void DrawCrosshair()
    {
        GUI.Label(
            new Rect(
                Screen.width * 0.5f - 8f,
                Screen.height * 0.5f - 12f,
                20f,
                24f),
            "+");
    }

    private static void DrawControls()
    {
        GUI.Box(
            new Rect(
                18f,
                Screen.height - 74f,
                455f,
                54f),
            "F: Interact    LMB: Light Attack    E: Heavy Attack\n" +
            "RMB: Block    Space: Jump    Left Ctrl: Sneak");
    }

    private static CombatHealth FindPlayerHealth()
    {
        CombatHealth[] all =
            Object.FindObjectsByType<CombatHealth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

        foreach (CombatHealth health in all)
        {
            if (health.Team == CombatTeam.Player)
                return health;
        }

        return null;
    }
}
