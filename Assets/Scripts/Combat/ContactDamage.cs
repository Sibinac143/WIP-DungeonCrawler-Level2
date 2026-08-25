using UnityEngine;

[DisallowMultipleComponent]
public sealed class ContactDamage : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(0.1f)] private float repeatInterval = 1f;
    [SerializeField] private CombatHealth sourceHealth;

    private CombatHealth playerHealth;
    private float nextDamageTime;

    public void Configure(
        CombatHealth source,
        float newDamage,
        float newRadius,
        float newRepeatInterval)
    {
        sourceHealth = source;
        damage = Mathf.Max(0f, newDamage);
        radius = Mathf.Max(0.1f, newRadius);
        repeatInterval = Mathf.Max(0.1f, newRepeatInterval);
    }

    private void Awake()
    {
        if (sourceHealth == null)
            sourceHealth = GetComponent<CombatHealth>();
    }

    private void Update()
    {
        if (sourceHealth != null && sourceHealth.IsDead)
            return;

        if (Time.time < nextDamageTime)
            return;

        if (playerHealth == null || playerHealth.IsDead)
            playerHealth = FindPlayerHealth();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                playerHealth.transform.position);

        if (distance > radius)
            return;

        bool accepted =
            playerHealth.ApplyDamage(
                damage,
                gameObject);

        if (accepted)
            nextDamageTime = Time.time + repeatInterval;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
