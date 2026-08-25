using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestBossWatcher : MonoBehaviour
{
    [SerializeField] private CombatHealth bossHealth;
    private bool notified;

    public void Configure(CombatHealth health)
    {
        bossHealth = health;
    }

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<CombatHealth>();
    }

    private void OnEnable()
    {
        if (bossHealth == null)
            bossHealth = GetComponent<CombatHealth>();

        if (bossHealth != null)
            bossHealth.Died += HandleBossDeath;
    }

    private void OnDisable()
    {
        if (bossHealth != null)
            bossHealth.Died -= HandleBossDeath;
    }

    private void HandleBossDeath(
        CombatHealth health,
        GameObject source)
    {
        if (notified)
            return;

        notified = true;
        QuestManager.Instance?.NotifyBossDefeated();
    }
}
