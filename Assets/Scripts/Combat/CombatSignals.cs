using System;

public static class CombatSignals
{
    public static event Action<string, float, bool> HitConfirmed;

    public static void ReportHit(
        string targetName,
        float damage,
        bool killed)
    {
        HitConfirmed?.Invoke(targetName, damage, killed);
    }
}
