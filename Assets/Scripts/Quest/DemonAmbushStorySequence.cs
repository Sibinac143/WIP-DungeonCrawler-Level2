using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DemonAmbushStorySequence : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestHUD hud;
    [SerializeField] private Transform player;
    [SerializeField] private BlacksmithStoryActor blacksmith;
    [SerializeField] private DemonBossAI demon;
    [SerializeField] private PlayerKnockdownSequence knockdown;

    [Header("Story locations")]
    [SerializeField] private Transform prisonPoint;
    [SerializeField] private Transform ambushPoint;
    [SerializeField] private Transform captivePoint;
    [SerializeField] private Transform demonGuardPoint;
    [SerializeField] private Transform workshopPoint;
    [SerializeField] private Transform[] prisonEscapeRoute;
    [SerializeField] private Transform[] workshopRoute;

    [Header("Scene objects")]
    [SerializeField] private GameObject captiveChains;
    [SerializeField] private QuestInteractable treeRescueInteractable;
    [SerializeField] private GameObject forgingBlacksmith;

    private bool waitingToStartAmbush;
    private bool ambushRunning;
    private bool subscribed;

    public void Configure(
        QuestManager manager,
        QuestHUD questHud,
        Transform playerTransform,
        BlacksmithStoryActor blacksmithActor,
        DemonBossAI demonBoss,
        PlayerKnockdownSequence playerKnockdown,
        Transform prison,
        Transform ambush,
        Transform captive,
        Transform guardPoint,
        Transform workshop,
        Transform[] firstRoute,
        Transform[] secondRoute,
        GameObject chains,
        QuestInteractable treeInteraction,
        GameObject forgingActor)
    {
        questManager = manager;
        hud = questHud;
        player = playerTransform;
        blacksmith = blacksmithActor;
        demon = demonBoss;
        knockdown = playerKnockdown;
        prisonPoint = prison;
        ambushPoint = ambush;
        captivePoint = captive;
        demonGuardPoint = guardPoint;
        workshopPoint = workshop;
        prisonEscapeRoute = firstRoute;
        workshopRoute = secondRoute;
        captiveChains = chains;
        treeRescueInteractable = treeInteraction;
        forgingBlacksmith = forgingActor;

        SubscribeToDemon();
    }

    private void OnEnable()
    {
        SubscribeToDemon();
    }

    private void OnDisable()
    {
        UnsubscribeFromDemon();
    }

    private void Update()
    {
        if (!waitingToStartAmbush ||
            ambushRunning ||
            player == null ||
            ambushPoint == null)
        {
            return;
        }

        float distance =
            Vector3.Distance(
                player.position,
                ambushPoint.position);

        if (distance <= 12f)
        {
            waitingToStartAmbush = false;
            StartCoroutine(AmbushRoutine());
        }
    }

    public void ApplyProgress(QuestStage stage)
    {
        waitingToStartAmbush = false;
        ambushRunning = false;

        if (forgingBlacksmith != null)
            forgingBlacksmith.SetActive(false);

        if (captiveChains != null)
            captiveChains.SetActive(false);

        if (treeRescueInteractable != null)
            treeRescueInteractable.gameObject.SetActive(false);

        if (demon != null)
            demon.gameObject.SetActive(false);

        switch (stage)
        {
            case QuestStage.EscortKeyMakerFromPrison:
                PlaceBlacksmithAtPrison();
                BeginFirstEscort();
                break;

            case QuestStage.DefeatDemon:
                PlaceCaptiveAtTree();
                demon?.PlaceDormantAt(demonGuardPoint);
                break;

            case QuestStage.FreeKeyMakerFromTree:
                PlaceCaptiveAtTree();

                if (demon != null)
                {
                    demon.gameObject.SetActive(true);
                    demon.SetDefeatedInstantly();
                }

                if (treeRescueInteractable != null)
                    treeRescueInteractable.gameObject.SetActive(true);
                break;

            case QuestStage.FollowKeyMakerToWorkshop:
                PlaceCaptiveAtTree();
                BeginWorkshopEscort();
                break;

            case QuestStage.ReceiveGateKey:
            case QuestStage.ReturnToMainGate:
            case QuestStage.EscapeRealm:
            case QuestStage.Complete:
                PlaceBlacksmithAtWorkshop();
                break;

            default:
                PlaceBlacksmithAtPrison();
                break;
        }
    }

    public void BeginFirstEscort()
    {
        if (blacksmith == null)
            return;

        if (captiveChains != null)
            captiveChains.SetActive(false);

        blacksmith.BeginRoute(
            prisonEscapeRoute,
            HandleFirstRouteReached);

        hud?.ShowToast(
            "The blacksmith is free. Stay close and follow him out of the castle.",
            5f);
    }

    public void BeginWorkshopEscort()
    {
        if (blacksmith == null)
            return;

        if (captiveChains != null)
            captiveChains.SetActive(false);

        if (treeRescueInteractable != null)
            treeRescueInteractable.gameObject.SetActive(false);

        blacksmith.BeginRoute(
            workshopRoute,
            HandleWorkshopReached);

        hud?.ShowToast(
            "The blacksmith is free again. Follow him to the mountain workshop.",
            5f);
    }

    public void BeginForging()
    {
        PlaceBlacksmithAtWorkshop();

        if (blacksmith != null)
            blacksmith.gameObject.SetActive(false);

        if (forgingBlacksmith == null)
            return;

        forgingBlacksmith.SetActive(true);

        Animator forgingAnimator =
            forgingBlacksmith.GetComponentInChildren<Animator>(true);

        if (forgingAnimator != null)
        {
            forgingAnimator.Play(
                "Action_Forging",
                0,
                0f);
        }
    }

    public void EndForging()
    {
        if (forgingBlacksmith != null)
            forgingBlacksmith.SetActive(false);

        if (blacksmith != null)
        {
            blacksmith.gameObject.SetActive(true);
            blacksmith.PlaceAt(
                workshopPoint,
                "Idle");
        }
    }

    public bool CanStealthStrike()
    {
        return demon != null &&
               demon.CanStealthStrike();
    }

    public void PerformStealthStrike()
    {
        demon?.PerformStealthStrike();
    }

    private void HandleFirstRouteReached()
    {
        waitingToStartAmbush = true;

        blacksmith?.PlayState(
            "Idle",
            0.12f);

        hud?.ShowToast(
            "The blacksmith stops. Something is moving beyond the ruined wall...",
            4.5f);
    }

    private IEnumerator AmbushRoutine()
    {
        ambushRunning = true;
        blacksmith?.StopGuiding();

        hud?.ShowToast(
            "The demon leaps into the escape path!",
            3f);

        bool attackFinished = false;
        bool knockdownFinished = false;
        bool guardReady = false;
        bool captivePlaced = false;

        if (demon == null)
        {
            PlaceCaptiveAtTree();
            questManager?.NotifyAmbushStarted();
            ambushRunning = false;
            yield break;
        }

        knockdown?.BeginAmbushFocus(
            demon.transform,
            0.42f);

        demon.BeginAmbushAttack(
            impact: () =>
            {
                if (!captivePlaced)
                {
                    captivePlaced = true;
                    StartCoroutine(
                        TieBlacksmithDuringKnockdown());
                }

                if (knockdown != null)
                {
                    knockdown.Play(
                        3.15f,
                        demon.gameObject,
                        () => knockdownFinished = true);
                }
                else
                {
                    knockdownFinished = true;
                }
            },
            completed: () =>
            {
                attackFinished = true;

                demon.MoveToGuardAndSleep(
                    demonGuardPoint,
                    () => guardReady = true);
            });

        float safetyTimeout = Time.time + 9f;

        while (Time.time < safetyTimeout &&
               (!attackFinished ||
                !knockdownFinished ||
                !guardReady))
        {
            yield return null;
        }

        if (!captivePlaced)
            PlaceCaptiveAtTree();

        questManager?.NotifyAmbushStarted();

        hud?.ShowToast(
            "The demon guards the chained blacksmith. Sneak with Left Ctrl or fight it directly.",
            5.5f);

        ambushRunning = false;
    }

    private IEnumerator TieBlacksmithDuringKnockdown()
    {
        yield return new WaitForSeconds(0.35f);
        PlaceCaptiveAtTree();
    }

    private void PlaceCaptiveAtTree()
    {
        blacksmith?.TieToTree(
            captivePoint);

        if (captiveChains != null)
            captiveChains.SetActive(true);

        if (treeRescueInteractable != null)
            treeRescueInteractable.gameObject.SetActive(false);
    }

    private void PlaceBlacksmithAtPrison()
    {
        blacksmith?.PlaceAt(
            prisonPoint,
            "Idle");
    }

    private void PlaceBlacksmithAtWorkshop()
    {
        blacksmith?.PlaceAt(
            workshopPoint,
            "Idle");

        if (captiveChains != null)
            captiveChains.SetActive(false);

        if (treeRescueInteractable != null)
            treeRescueInteractable.gameObject.SetActive(false);
    }

    private void HandleDemonDefeated()
    {
        questManager?.NotifyDemonDefeated();

        if (treeRescueInteractable != null)
            treeRescueInteractable.gameObject.SetActive(true);

        hud?.ShowToast(
            "The demon is defeated. Free the blacksmith from the tree.",
            5f);
    }

    private void HandleWorkshopReached()
    {
        PlaceBlacksmithAtWorkshop();
        questManager?.NotifyBlacksmithReachedWorkshop();
    }

    private void SubscribeToDemon()
    {
        if (subscribed ||
            demon == null)
        {
            return;
        }

        demon.Defeated += HandleDemonDefeated;
        subscribed = true;
    }

    private void UnsubscribeFromDemon()
    {
        if (!subscribed ||
            demon == null)
        {
            return;
        }

        demon.Defeated -= HandleDemonDefeated;
        subscribed = false;
    }
}
