using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestStoryReferences : MonoBehaviour
{
    [SerializeField] private GatePassageController gatePassage;
    [SerializeField] private DemonAmbushStorySequence storySequence;
    [SerializeField] private KeyRewardPopup keyRewardPopup;
    [SerializeField] private GameObject finalKeyPreview;

    [Header("Phase 4")]
    [SerializeField] private GateBattleSequence gateBattle;
    [SerializeField] private MagicSwordUpgrade magicSwordUpgrade;
    [SerializeField] private QuestObjectiveIndicator objectiveIndicator;

    public void Configure(
        GatePassageController passage,
        DemonAmbushStorySequence sequence,
        KeyRewardPopup popup,
        GameObject keyPreview)
    {
        gatePassage = passage;
        storySequence = sequence;
        keyRewardPopup = popup;
        finalKeyPreview = keyPreview;

        if (finalKeyPreview != null)
            finalKeyPreview.SetActive(false);
    }

    public void ConfigurePhaseFour(
        GateBattleSequence battle,
        MagicSwordUpgrade swordUpgrade,
        QuestObjectiveIndicator indicator)
    {
        gateBattle = battle;
        magicSwordUpgrade = swordUpgrade;
        objectiveIndicator = indicator;
    }

    public void ApplyProgress(
        QuestStage stage,
        bool hasKey)
    {
        // The key exists only as saved inventory data and a brief reward popup.
        // Never leave the giant preview key visible in the world or camera.
        if (finalKeyPreview != null)
            finalKeyPreview.SetActive(false);

        storySequence?.ApplyProgress(stage);
        gateBattle?.ApplyProgress(stage);
        magicSwordUpgrade?.SetEquipped(hasKey);

        if (stage >= QuestStage.EscapeRealm)
            gatePassage?.SetOpenInstantly();
        else
            gatePassage?.SetClosedInstantly();
    }

    public void BeginFirstEscort()
    {
        storySequence?.BeginFirstEscort();
    }

    public void BeginWorkshopEscort()
    {
        storySequence?.BeginWorkshopEscort();
    }

    public void BeginForging()
    {
        storySequence?.BeginForging();
    }

    public void EndForging()
    {
        storySequence?.EndForging();
    }

    public bool CanStealthStrike()
    {
        return storySequence != null &&
               storySequence.CanStealthStrike();
    }

    public void PerformStealthStrike()
    {
        storySequence?.PerformStealthStrike();
    }

    public void BeginGateAmbush()
    {
        gateBattle?.BeginGateAmbush();
    }

    public void EquipMagicSword()
    {
        magicSwordUpgrade?.SetEquipped(true);
    }

    public void ShowKeyReward()
    {
        keyRewardPopup?.Show();
    }

    public void ShowFinalKey(bool visible)
    {
        if (finalKeyPreview != null)
            finalKeyPreview.SetActive(false);
    }

    public void OpenGate()
    {
        gatePassage?.OpenGate();
    }
}
