using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerShield : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxShield = 100f;
    [SerializeField, Min(0f)] private float currentShield;

    public float MaxShield => maxShield;
    public float CurrentShield => currentShield;
    public float NormalizedShield =>
        maxShield > 0f
            ? Mathf.Clamp01(currentShield / maxShield)
            : 0f;

    public bool HasShield => currentShield > 0f;

    public event Action<PlayerShield> Changed;
    public event Action<PlayerShield, float> AbsorbedDamage;

    private void Awake()
    {
        maxShield = Mathf.Max(1f, maxShield);
        currentShield = Mathf.Clamp(currentShield, 0f, maxShield);
    }

    private void OnValidate()
    {
        maxShield = Mathf.Max(1f, maxShield);
        currentShield = Mathf.Clamp(currentShield, 0f, maxShield);
    }

    public void Configure(
        float newMaximum,
        float startingShield = 0f)
    {
        maxShield = Mathf.Max(1f, newMaximum);
        currentShield = Mathf.Clamp(startingShield, 0f, maxShield);
        Changed?.Invoke(this);
    }

    public float AddShield(float amount)
    {
        if (amount <= 0f)
            return 0f;

        float before = currentShield;
        currentShield = Mathf.Min(maxShield, currentShield + amount);
        float added = currentShield - before;

        if (added > 0f)
            Changed?.Invoke(this);

        return added;
    }

    public float AbsorbDamage(float incomingDamage)
    {
        if (incomingDamage <= 0f || currentShield <= 0f)
            return Mathf.Max(0f, incomingDamage);

        float absorbed = Mathf.Min(currentShield, incomingDamage);
        currentShield -= absorbed;

        AbsorbedDamage?.Invoke(this, absorbed);
        Changed?.Invoke(this);

        return Mathf.Max(0f, incomingDamage - absorbed);
    }

    public void ClearShield()
    {
        if (currentShield <= 0f)
            return;

        currentShield = 0f;
        Changed?.Invoke(this);
    }
}
