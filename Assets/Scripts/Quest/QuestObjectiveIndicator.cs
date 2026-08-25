using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestObjectiveIndicator : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private Camera viewCamera;

    [Header("Quest destinations")]
    [SerializeField] private Transform mainGate;
    [SerializeField] private Transform villageWorkshop;
    [SerializeField] private Transform warningLetter;
    [SerializeField] private Transform castleEntrance;
    [SerializeField] private Transform prisonBlacksmith;
    [SerializeField] private Transform castleDemon;
    [SerializeField] private Transform captiveTree;
    [SerializeField] private Transform workshop;
    [SerializeField] private Transform gateBattleCenter;
    [SerializeField] private Transform escapePoint;

    [Header("Display")]
    [SerializeField, Min(1f)] private float hideWithinDistance = 4f;
    [SerializeField, Min(10f)] private float edgePadding = 48f;
    [SerializeField] private bool showDistance = true;

    public void Configure(
        QuestManager manager,
        Camera cameraReference,
        Transform gate,
        Transform village,
        Transform letter,
        Transform castle,
        Transform prisoner,
        Transform firstDemon,
        Transform captive,
        Transform workshopPoint,
        Transform gateFight,
        Transform escape)
    {
        questManager = manager;
        viewCamera = cameraReference;
        mainGate = gate;
        villageWorkshop = village;
        warningLetter = letter;
        castleEntrance = castle;
        prisonBlacksmith = prisoner;
        castleDemon = firstDemon;
        captiveTree = captive;
        workshop = workshopPoint;
        gateBattleCenter = gateFight;
        escapePoint = escape;
    }

    private void Awake()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (viewCamera == null)
            viewCamera = Camera.main;
    }

    private void OnGUI()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (questManager == null ||
            viewCamera == null)
        {
            return;
        }

        Transform target =
            GetCurrentTarget(
                questManager.CurrentStage);

        if (target == null)
            return;

        float distance =
            Vector3.Distance(
                viewCamera.transform.position,
                target.position);

        if (distance <= hideWithinDistance)
            return;

        Vector3 screen =
            viewCamera.WorldToScreenPoint(
                target.position +
                Vector3.up * 1.8f);

        bool behind = screen.z <= 0f;

        if (behind)
        {
            screen.x =
                Screen.width - screen.x;

            screen.y =
                Screen.height - screen.y;
        }

        float x =
            Mathf.Clamp(
                screen.x,
                edgePadding,
                Screen.width - edgePadding);

        float y =
            Mathf.Clamp(
                Screen.height - screen.y,
                edgePadding + 60f,
                Screen.height - edgePadding - 80f);

        bool offscreen =
            behind ||
            screen.x < edgePadding ||
            screen.x > Screen.width - edgePadding ||
            Screen.height - screen.y < edgePadding + 60f ||
            Screen.height - screen.y >
                Screen.height - edgePadding - 80f;

        string symbol;

        if (!offscreen)
        {
            symbol = "◆";
        }
        else if (x <= edgePadding + 1f)
        {
            symbol = "◀";
        }
        else if (x >= Screen.width - edgePadding - 1f)
        {
            symbol = "▶";
        }
        else if (y <= edgePadding + 61f)
        {
            symbol = "▲";
        }
        else
        {
            symbol = "▼";
        }

        string label =
            symbol +
            (showDistance
                ? "  " +
                  Mathf.RoundToInt(distance) +
                  " m"
                : string.Empty);

        GUI.Box(
            new Rect(
                x - 46f,
                y - 18f,
                92f,
                36f),
            label);
    }

    private Transform GetCurrentTarget(
        QuestStage stage)
    {
        switch (stage)
        {
            case QuestStage.InspectMainGate:
            case QuestStage.ReturnToMainGate:
                return mainGate;

            case QuestStage.SearchMountainVillage:
                return villageWorkshop;

            case QuestStage.ReadWarningLetter:
                return warningLetter;

            case QuestStage.ReachRuinedCastle:
                return castleEntrance;

            case QuestStage.FindKeyMaker:
            case QuestStage.EscortKeyMakerFromPrison:
                return prisonBlacksmith;

            case QuestStage.DefeatDemon:
                return castleDemon;

            case QuestStage.FreeKeyMakerFromTree:
                return captiveTree;

            case QuestStage.FollowKeyMakerToWorkshop:
            case QuestStage.ReceiveGateKey:
                return workshop;

            case QuestStage.DefeatGateGuardians:
                return gateBattleCenter;

            case QuestStage.EscapeRealm:
                return escapePoint;

            default:
                return null;
        }
    }
}
