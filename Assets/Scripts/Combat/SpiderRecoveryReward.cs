using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpiderRecoveryReward : MonoBehaviour
{
    [SerializeField] private CombatHealth spiderHealth;
    [SerializeField, Min(0f)] private float healthReward = 25f;
    [SerializeField, Min(0f)] private float shieldReward = 20f;

    private bool rewarded;

    public void Configure(
        CombatHealth healthReference,
        float healing,
        float shield)
    {
        spiderHealth = healthReference;
        healthReward = Mathf.Max(0f, healing);
        shieldReward = Mathf.Max(0f, shield);
    }

    private void Awake()
    {
        if (spiderHealth == null)
            spiderHealth = GetComponent<CombatHealth>();
    }

    private void OnEnable()
    {
        if (spiderHealth != null)
        {
            spiderHealth.Died -= HandleDeath;
            spiderHealth.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (spiderHealth != null)
            spiderHealth.Died -= HandleDeath;
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        if (rewarded)
            return;

        CombatHealth playerHealth =
            FindPlayerHealth(source);

        if (playerHealth == null)
            return;

        rewarded = true;

        float healed =
            playerHealth.Heal(healthReward);

        PlayerShield shield =
            playerHealth.GetComponent<PlayerShield>();

        float shieldAdded =
            shield != null
                ? shield.AddShield(shieldReward)
                : 0f;

        QuestHUD hud =
            Object.FindFirstObjectByType<QuestHUD>();

        if (hud != null)
        {
            string message =
                "SPIDER ESSENCE  +" +
                Mathf.RoundToInt(healed) +
                " HEALTH   +" +
                Mathf.RoundToInt(shieldAdded) +
                " SHIELD";

            if (healed <= 0f && shieldAdded <= 0f)
                message = "Health and shield are already full.";

            hud.ShowToast(message, 3.5f);
        }
    }

    private static CombatHealth FindPlayerHealth(
        GameObject source)
    {
        if (source != null)
        {
            CombatHealth sourceHealth =
                source.GetComponentInParent<CombatHealth>();

            if (sourceHealth != null &&
                sourceHealth.Team == CombatTeam.Player)
            {
                return sourceHealth;
            }
        }

        foreach (CombatHealth health in
                 Object.FindObjectsByType<CombatHealth>(
                     FindObjectsInactive.Exclude,
                     FindObjectsSortMode.None))
        {
            if (health.Team == CombatTeam.Player)
                return health;
        }

        return null;
    }
}
