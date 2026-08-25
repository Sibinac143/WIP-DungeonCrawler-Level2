using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GateKeyVisualLifecycle : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private List<GameObject> keyVisuals = new List<GameObject>();
    [SerializeField] private bool destroyInsteadOfDisable;

    private QuestStage lastStage;
    private bool initialized;

    public void Configure(
        QuestManager manager,
        IEnumerable<GameObject> visuals)
    {
        questManager = manager;
        keyVisuals.Clear();

        foreach (GameObject visual in visuals)
        {
            if (visual != null &&
                !keyVisuals.Contains(visual))
            {
                keyVisuals.Add(visual);
            }
        }
    }

    private void Awake()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;
    }

    private void Start()
    {
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager == null)
            return;

        QuestStage stage = questManager.CurrentStage;

        if (!force &&
            initialized &&
            stage == lastStage)
        {
            return;
        }

        initialized = true;
        lastStage = stage;

        bool keyHasBeenUsed =
            stage == QuestStage.DefeatGateGuardians ||
            stage == QuestStage.EscapeRealm ||
            stage == QuestStage.Complete;

        if (!keyHasBeenUsed)
            return;

        HideKeyVisuals();
    }

    public void HideKeyVisuals()
    {
        foreach (GameObject visual in keyVisuals)
        {
            if (visual == null)
                continue;

            if (destroyInsteadOfDisable)
                Destroy(visual);
            else
                visual.SetActive(false);
        }
    }
}
