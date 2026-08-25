using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GateBattleSequence : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestHUD hud;
    [SerializeField] private GateGuardianAI leftGuardian;
    [SerializeField] private GateGuardianAI rightGuardian;
    [SerializeField] private GateThrowbackSequence throwback;
    [SerializeField] private Transform strikePoint;

    private bool battleStarted;
    private bool subscribed;

    public void Configure(
        QuestManager manager,
        QuestHUD questHud,
        GateGuardianAI left,
        GateGuardianAI right,
        GateThrowbackSequence throwSequence,
        Transform throwStrikePoint)
    {
        questManager = manager;
        hud = questHud;
        leftGuardian = left;
        rightGuardian = right;
        throwback = throwSequence;
        strikePoint = throwStrikePoint;

        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void ApplyProgress(
        QuestStage stage)
    {
        battleStarted = false;

        if (stage < QuestStage.DefeatGateGuardians)
        {
            leftGuardian?.SetFrozen();
            rightGuardian?.SetFrozen();
            return;
        }

        if (stage == QuestStage.DefeatGateGuardians)
        {
            battleStarted = true;
            leftGuardian?.BeginCombat(0.1f);
            rightGuardian?.BeginCombat(0.65f);
            return;
        }

        leftGuardian?.SetDefeatedInstantly();
        rightGuardian?.SetDefeatedInstantly();
    }

    public void BeginGateAmbush()
    {
        if (battleStarted)
            return;

        battleStarted = true;
        StartCoroutine(
            GateAmbushRoutine());
    }

    private IEnumerator GateAmbushRoutine()
    {
        hud?.ShowToast(
            "The key enters the lock... but the frozen guardians begin to move.",
            4.5f);

        bool throwAnimationFinished = false;
        bool throwRecoveryFinished = false;

        GateGuardianAI attacker =
            leftGuardian != null
                ? leftGuardian
                : rightGuardian;

        if (attacker == null)
        {
            ActivateBothGuardians();
            yield break;
        }

        attacker.BeginThrowAmbush(
            strikePoint,
            impact: () =>
            {
                if (throwback != null)
                {
                    throwback.Play(
                        attacker.transform,
                        () =>
                            throwRecoveryFinished = true);
                }
                else
                {
                    throwRecoveryFinished = true;
                }
            },
            completed: () =>
            {
                throwAnimationFinished = true;
            });

        float timeout =
            Time.time + 10f;

        while (Time.time < timeout &&
               (!throwAnimationFinished ||
                !throwRecoveryFinished))
        {
            yield return null;
        }

        hud?.ShowToast(
            "The gate remains sealed. Defeat both guardians!",
            4.5f);

        ActivateBothGuardians();
    }

    private void ActivateBothGuardians()
    {
        leftGuardian?.BeginCombat(0.05f);
        rightGuardian?.BeginCombat(0.7f);
    }

    private void HandleGuardianDefeated(
        GateGuardianAI guardian)
    {
        bool leftDead =
            leftGuardian == null ||
            leftGuardian.IsDead;

        bool rightDead =
            rightGuardian == null ||
            rightGuardian.IsDead;

        if (!leftDead ||
            !rightDead)
        {
            hud?.ShowToast(
                "One guardian remains.",
                2.8f);
            return;
        }

        questManager?.NotifyGateGuardiansDefeated();
    }

    private void Subscribe()
    {
        if (subscribed)
            return;

        if (leftGuardian != null)
            leftGuardian.Defeated +=
                HandleGuardianDefeated;

        if (rightGuardian != null)
            rightGuardian.Defeated +=
                HandleGuardianDefeated;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (leftGuardian != null)
            leftGuardian.Defeated -=
                HandleGuardianDefeated;

        if (rightGuardian != null)
            rightGuardian.Defeated -=
                HandleGuardianDefeated;

        subscribed = false;
    }
}
