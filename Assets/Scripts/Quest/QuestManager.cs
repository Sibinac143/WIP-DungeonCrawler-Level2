using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestManager : MonoBehaviour
{
    private const string StageKey =
        "DungeonGame.Phase3.Stage";

    private const string HasKeyKey =
        "DungeonGame.Phase3.HasKey";

    private const string CompleteKey =
        "DungeonGame.Phase3.Complete";

    public static QuestManager Instance { get; private set; }

    [SerializeField] private QuestStage currentStage =
        QuestStage.InspectMainGate;

    [SerializeField] private bool hasGateKey;
    [SerializeField] private bool gameComplete;
    [SerializeField] private QuestHUD hud;
    [SerializeField] private QuestStoryReferences storyReferences;

    public QuestStage CurrentStage => currentStage;
    public bool HasGateKey => hasGateKey;
    public bool GameComplete => gameComplete;

    public event Action<QuestStage> StageChanged;

    public void Configure(
        QuestHUD questHud,
        QuestStoryReferences references)
    {
        hud = questHud;
        storyReferences = references;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hud == null)
            hud = FindFirstObjectByType<QuestHUD>();

        if (storyReferences == null)
        {
            storyReferences =
                FindFirstObjectByType<QuestStoryReferences>();
        }

        LoadProgress();
    }

    private void Start()
    {
        storyReferences?.ApplyProgress(
            currentStage,
            hasGateKey);

        hud?.SetObjective(
            GetObjectiveText());

        if (currentStage ==
            QuestStage.InspectMainGate)
        {
            hud?.ShowToast(
                "You awaken in the Realm of Death. Find a way out.",
                4.5f);
        }
    }

    public bool CanInteract(
        QuestAction action)
    {
        switch (action)
        {
            case QuestAction.MainGate:
                return
                    currentStage ==
                    QuestStage.InspectMainGate ||
                    currentStage ==
                    QuestStage.ReturnToMainGate;

            case QuestAction.EmptyKeyBox:
                return
                    currentStage ==
                    QuestStage.SearchMountainVillage;

            case QuestAction.WarningLetter:
                return
                    currentStage ==
                    QuestStage.ReadWarningLetter;

            case QuestAction.CastleEntrance:
                return
                    currentStage ==
                    QuestStage.ReachRuinedCastle;

            case QuestAction.RescueKeyMaker:
                return
                    currentStage ==
                    QuestStage.ReachRuinedCastle ||
                    currentStage ==
                    QuestStage.FindKeyMaker;

            case QuestAction.StealthStrike:
                return
                    currentStage ==
                    QuestStage.DefeatDemon &&
                    storyReferences != null &&
                    storyReferences.CanStealthStrike();

            case QuestAction.FreeTreeCaptive:
                return
                    currentStage ==
                    QuestStage.FreeKeyMakerFromTree;

            case QuestAction.ForgeKey:
                return
                    currentStage ==
                    QuestStage.ReceiveGateKey;

            case QuestAction.EscapeRealm:
                return
                    currentStage ==
                    QuestStage.EscapeRealm;

            default:
                return false;
        }
    }

    public string GetPrompt(
        QuestAction action)
    {
        switch (action)
        {
            case QuestAction.MainGate:
                return
                    currentStage ==
                    QuestStage.ReturnToMainGate
                        ? "Press F to unlock the main gate"
                        : "Press F to inspect the locked gate";

            case QuestAction.EmptyKeyBox:
                return
                    "Press F to inspect the empty key box";

            case QuestAction.WarningLetter:
                return
                    "Press F to read the warning letter";

            case QuestAction.CastleEntrance:
                return
                    "Press F to inspect the ruined castle";

            case QuestAction.RescueKeyMaker:
                return
                    "Press F to free the imprisoned blacksmith";

            case QuestAction.StealthStrike:
                return
                    "Press F for a stealth strike";

            case QuestAction.FreeTreeCaptive:
                return
                    "Press F to cut the chains and free the blacksmith";

            case QuestAction.ForgeKey:
                return
                    "Press F to receive the reforged gate key";

            case QuestAction.EscapeRealm:
                return
                    "Press F to escape the Realm of Death";

            default:
                return
                    "Press F to interact";
        }
    }

    public void HandleInteraction(
        QuestAction action,
        GameObject source)
    {
        if (!CanInteract(action))
            return;

        switch (action)
        {
            case QuestAction.MainGate:
                HandleMainGate();
                break;

            case QuestAction.EmptyKeyBox:
                hud?.ShowToast(
                    "The key box is empty. A folded letter lies beside it.",
                    4f);

                SetStage(
                    QuestStage.ReadWarningLetter);
                break;

            case QuestAction.WarningLetter:
                hud?.ShowLetter(
                    "Please leave the town with the secrets of the ruin " +
                    "and never come back, or you will be forever jailed " +
                    "in the castle.");

                SetStage(
                    QuestStage.ReachRuinedCastle);
                break;

            case QuestAction.CastleEntrance:
                hud?.ShowToast(
                    "The letter's mark matches this castle. " +
                    "The missing blacksmith must be imprisoned inside.",
                    4.5f);

                SetStage(
                    QuestStage.FindKeyMaker);
                break;

            case QuestAction.RescueKeyMaker:
                HandleFirstRescue();
                break;

            case QuestAction.StealthStrike:
                storyReferences?.PerformStealthStrike();

                hud?.ShowToast(
                    "STEALTH STRIKE — the demon is badly wounded!",
                    3.5f);
                break;

            case QuestAction.FreeTreeCaptive:
                HandleSecondRescue();
                break;

            case QuestAction.ForgeKey:
                StartCoroutine(
                    ForgeAndGiveKeyRoutine());
                break;

            case QuestAction.EscapeRealm:
                CompleteGame();
                break;
        }
    }

    // Compatibility for older Phase 3 helper scripts still present.
    public void NotifyBossDefeated()
    {
        NotifyDemonDefeated();
    }

    public void NotifyKeyMakerArrived()
    {
        NotifyBlacksmithReachedWorkshop();
    }

    public void NotifyAmbushStarted()
    {
        if (currentStage !=
            QuestStage.EscortKeyMakerFromPrison)
        {
            return;
        }

        SetStage(
            QuestStage.DefeatDemon);
    }

    public void NotifyDemonDefeated()
    {
        if (currentStage !=
            QuestStage.DefeatDemon)
        {
            return;
        }

        SetStage(
            QuestStage.FreeKeyMakerFromTree);
    }

    public void NotifyBlacksmithReachedWorkshop()
    {
        if (currentStage !=
            QuestStage.FollowKeyMakerToWorkshop)
        {
            return;
        }

        SetStage(
            QuestStage.ReceiveGateKey);

        hud?.ShowToast(
            "The blacksmith reaches his forge. Speak to him to receive the key.",
            5f);
    }

    public void NotifyGateGuardiansDefeated()
    {
        if (currentStage !=
            QuestStage.DefeatGateGuardians)
        {
            return;
        }

        storyReferences?.OpenGate();

        hud?.ShowToast(
            "Both gate guardians are defeated. The gate finally opens.",
            5f);

        SetStage(
            QuestStage.EscapeRealm);
    }

    private void HandleMainGate()
    {
        if (currentStage ==
            QuestStage.InspectMainGate)
        {
            hud?.ShowToast(
                "An enormous lock seals the gate. " +
                "Two petrified demons stand beside the only exit. " +
                "Search the mountain village.",
                5.5f);

            SetStage(
                QuestStage.SearchMountainVillage);

            return;
        }

        if (currentStage ==
            QuestStage.ReturnToMainGate &&
            hasGateKey)
        {
            SetStage(
                QuestStage.DefeatGateGuardians);

            hud?.ShowToast(
                "The key touches the lock. The two frozen demons awaken!",
                4.5f);

            storyReferences?.BeginGateAmbush();
        }
    }

    private void HandleFirstRescue()
    {
        SetStage(
            QuestStage.EscortKeyMakerFromPrison);

        hud?.ShowToast(
            "You free the blacksmith. " +
            "\"Stay close. I know a way out of these ruins.\"",
            5.5f);

        storyReferences?.BeginFirstEscort();
    }

    private void HandleSecondRescue()
    {
        SetStage(
            QuestStage.FollowKeyMakerToWorkshop);

        hud?.ShowToast(
            "You cut the chains. " +
            "\"My workshop is on the mountain. Follow me.\"",
            5.5f);

        storyReferences?.BeginWorkshopEscort();
    }

    private IEnumerator ForgeAndGiveKeyRoutine()
    {
        storyReferences?.BeginForging();

        hud?.ShowToast(
            "The blacksmith heats the metal and restores the ancient key...",
            4f);

        yield return new WaitForSeconds(3.2f);

        hasGateKey = true;
        SaveProgress();

        storyReferences?.ShowKeyReward();
        storyReferences?.EquipMagicSword();

        hud?.ShowToast(
            "The blacksmith gives you the Main Gate Key and a magic sword.",
            4.5f);

        yield return new WaitForSeconds(1.2f);

        hud?.ShowToast(
            "\"This blade carries more power than your old weapon. " +
            "Good luck on your journey... and good luck escaping this realm.\"",
            7f);

        yield return new WaitForSeconds(1.1f);

        storyReferences?.EndForging();

        SetStage(
            QuestStage.ReturnToMainGate);
    }

    private void CompleteGame()
    {
        gameComplete = true;

        SetStage(
            QuestStage.Complete);

        hud?.ShowCompletion(
            "YOU ESCAPED THE REALM OF DEATH");
    }

    private void SetStage(
        QuestStage newStage)
    {
        currentStage = newStage;

        SaveProgress();

        hud?.SetObjective(
            GetObjectiveText());

        StageChanged?.Invoke(
            currentStage);
    }

    public string GetObjectiveText()
    {
        switch (currentStage)
        {
            case QuestStage.InspectMainGate:
                return
                    "Objective: Investigate the enormous main gate.";

            case QuestStage.SearchMountainVillage:
                return
                    "Objective: Search the mountain village for the blacksmith's workshop.";

            case QuestStage.ReadWarningLetter:
                return
                    "Objective: Read the warning letter beside the empty key box.";

            case QuestStage.ReachRuinedCastle:
                return
                    "Objective: Travel to the ruined castle mentioned in the letter.";

            case QuestStage.FindKeyMaker:
                return
                    "Objective: Enter the dungeon and free the imprisoned blacksmith.";

            case QuestStage.EscortKeyMakerFromPrison:
                return
                    "Objective: Stay close and follow the blacksmith out of the castle.";

            case QuestStage.DefeatDemon:
                return
                    "Objective: Defeat the demon. Hold Left Ctrl to sneak behind it for a stealth strike.";

            case QuestStage.FreeKeyMakerFromTree:
                return
                    "Objective: Free the blacksmith from the dead tree.";

            case QuestStage.FollowKeyMakerToWorkshop:
                return
                    "Objective: Follow the blacksmith to his mountain workshop.";

            case QuestStage.ReceiveGateKey:
                return
                    "Objective: Speak to the blacksmith beside the forge.";

            case QuestStage.ReturnToMainGate:
                return
                    "Objective: Carry the reforged key and magic sword back to the main gate.";

            case QuestStage.DefeatGateGuardians:
                return
                    "Objective: Defeat both demons guarding the main gate.";

            case QuestStage.EscapeRealm:
                return
                    "Objective: Pass through the fully opened gate and escape.";

            case QuestStage.Complete:
                return
                    "The Realm of Death has been escaped.";

            default:
                return string.Empty;
        }
    }

    private void LoadProgress()
    {
        currentStage =
            (QuestStage)PlayerPrefs.GetInt(
                StageKey,
                (int)QuestStage.InspectMainGate);

        hasGateKey =
            PlayerPrefs.GetInt(
                HasKeyKey,
                0) == 1;

        gameComplete =
            PlayerPrefs.GetInt(
                CompleteKey,
                0) == 1;
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(
            StageKey,
            (int)currentStage);

        PlayerPrefs.SetInt(
            HasKeyKey,
            hasGateKey ? 1 : 0);

        PlayerPrefs.SetInt(
            CompleteKey,
            gameComplete ? 1 : 0);

        PlayerPrefs.Save();
    }

    public static void ResetSavedProgress()
    {
        PlayerPrefs.DeleteKey(StageKey);
        PlayerPrefs.DeleteKey(HasKeyKey);
        PlayerPrefs.DeleteKey(CompleteKey);

        // Remove obsolete Phase 3.0 keys too.
        PlayerPrefs.DeleteKey(
            "DungeonGame.Phase3.BossDefeated");

        PlayerPrefs.DeleteKey(
            "DungeonGame.Phase3.KeyMakerAtWorkshop");

        PlayerPrefs.Save();
    }
}
