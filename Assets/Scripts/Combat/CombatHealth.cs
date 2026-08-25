using System;
using UnityEngine;

public enum CombatTeam
{
    Neutral,
    Player,
    Enemy,
    Friendly
}

[DisallowMultipleComponent]
public sealed class CombatHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private CombatTeam team = CombatTeam.Neutral;
    [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.08f;
    [SerializeField] private bool allowFriendlyFire;
    [SerializeField] private bool resetOnEnable;

    private float lastDamageTime = float.NegativeInfinity;
    private float invulnerableUntil = float.NegativeInfinity;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    public float NormalizedHealth =>
        maxHealth > 0f
            ? Mathf.Clamp01(currentHealth / maxHealth)
            : 0f;

    public CombatTeam Team => team;
    public bool IsDead => currentHealth <= 0f;

    public event Action<CombatHealth, float, GameObject> Damaged;
    public event Action<CombatHealth, GameObject> Died;
    public event Action<CombatHealth> Restored;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        if (currentHealth <= 0f ||
            currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    private void OnEnable()
    {
        if (resetOnEnable)
            Revive();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        invulnerabilitySeconds = Mathf.Max(0f, invulnerabilitySeconds);
    }

    public void Configure(
        float newMaxHealth,
        CombatTeam newTeam,
        float newInvulnerabilitySeconds = 0.08f,
        bool newResetOnEnable = false)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        currentHealth = maxHealth;
        team = newTeam;
        invulnerabilitySeconds =
            Mathf.Max(0f, newInvulnerabilitySeconds);
        resetOnEnable = newResetOnEnable;
    }

    public bool ApplyDamage(
        float rawDamage,
        GameObject source)
    {
        if (rawDamage <= 0f ||
            IsDead ||
            Time.time < invulnerableUntil)
        {
            return false;
        }

        if (Time.time - lastDamageTime <
            invulnerabilitySeconds)
        {
            return false;
        }

        CombatHealth sourceHealth = null;

        if (source != null)
        {
            sourceHealth =
                source.GetComponentInParent<CombatHealth>();
        }

        if (!allowFriendlyFire &&
            sourceHealth != null &&
            sourceHealth != this &&
            sourceHealth.Team != CombatTeam.Neutral &&
            sourceHealth.Team == team)
        {
            return false;
        }

        if (sourceHealth == this)
            return false;

        float damage = rawDamage;

        PlayerDefense defense =
            GetComponent<PlayerDefense>();

        if (defense != null &&
            defense.IsBlocking)
        {
            damage *=
                defense.BlockedDamageMultiplier;
        }

        damage = Mathf.Max(0f, damage);

        if (damage <= 0f)
            return false;

        lastDamageTime = Time.time;

        float healthDamage = damage;

        if (team == CombatTeam.Player)
        {
            PlayerShield shield =
                GetComponent<PlayerShield>();

            if (shield != null)
            {
                healthDamage =
                    shield.AbsorbDamage(damage);
            }
        }

        if (healthDamage > 0f)
        {
            currentHealth =
                Mathf.Max(
                    0f,
                    currentHealth - healthDamage);
        }

        Damaged?.Invoke(
            this,
            healthDamage,
            source);

        if (IsDead)
            Died?.Invoke(this, source);

        return true;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f ||
            IsDead)
        {
            return 0f;
        }

        float before = currentHealth;

        currentHealth =
            Mathf.Min(
                maxHealth,
                currentHealth + amount);

        return currentHealth - before;
    }

    public void Revive()
    {
        currentHealth = maxHealth;
        lastDamageTime = float.NegativeInfinity;
        invulnerableUntil = float.NegativeInfinity;
        Restored?.Invoke(this);
    }

    public void GrantInvulnerability(float seconds)
    {
        if (seconds <= 0f)
            return;

        invulnerableUntil =
            Mathf.Max(
                invulnerableUntil,
                Time.time + seconds);
    }
}
