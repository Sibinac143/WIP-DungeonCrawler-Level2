using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BlacksmithCaptivePresentation : MonoBehaviour
{
    public QuestManager questManager;
    public GameObject originalVisual;
    public GameObject captiveVisual;
    public Animator captiveAnimator;
    public Transform leftHand;
    public Transform rightHand;
    public Transform leftAnchor;
    public Transform rightAnchor;
    public GameObject chainPrefab;

    private readonly List<Transform> leftLinks = new();
    private readonly List<Transform> rightLinks = new();
    private QuestStage lastStage;
    private bool started;

    private void Awake()
    {
        if (questManager == null) questManager = QuestManager.Instance;
        if (captiveAnimator == null && captiveVisual != null)
            captiveAnimator = captiveVisual.GetComponentInChildren<Animator>(true);
        if (leftHand == null && captiveVisual != null)
            leftHand = FindBone(captiveVisual.transform, "LeftHand");
        if (rightHand == null && captiveVisual != null)
            rightHand = FindBone(captiveVisual.transform, "RightHand");
    }

    private void Start() => Apply(true);

    private void LateUpdate()
    {
        Apply(false);
        if (captiveVisual != null && captiveVisual.activeInHierarchy)
        {
            UpdateChain(leftAnchor, leftHand, leftLinks);
            UpdateChain(rightAnchor, rightHand, rightLinks);
        }
    }

    private void Apply(bool force)
    {
        if (questManager == null) questManager = QuestManager.Instance;
        if (questManager == null) return;

        QuestStage stage = questManager.CurrentStage;
        if (!force && started && stage == lastStage) return;
        started = true;
        lastStage = stage;

        bool jail =
            stage == QuestStage.ReachRuinedCastle ||
            stage == QuestStage.FindKeyMaker;

        bool tied =
            stage == QuestStage.DefeatDemon ||
            stage == QuestStage.FreeKeyMakerFromTree;

        if (jail)
        {
            SetVisuals(true);
            Play("Jail Kneeling Idle");
            return;
        }

        if (tied)
        {
            SetVisuals(true);
            Play("Tied Injured Idle");
            return;
        }

        SetVisuals(false);
    }

    private void SetVisuals(bool captive)
    {
        if (captiveVisual != null) captiveVisual.SetActive(captive);
        if (originalVisual != null) originalVisual.SetActive(!captive);
        foreach (Transform t in leftLinks) if (t != null) t.gameObject.SetActive(captive);
        foreach (Transform t in rightLinks) if (t != null) t.gameObject.SetActive(captive);
    }

    private void Play(string state)
    {
        if (captiveAnimator == null) return;
        int hash = Animator.StringToHash(state);
        if (captiveAnimator.HasState(0, hash))
            captiveAnimator.CrossFade(hash, 0.1f);
    }

    private void UpdateChain(Transform start, Transform end, List<Transform> links)
    {
        if (start == null || end == null || chainPrefab == null) return;

        float distance = Vector3.Distance(start.position, end.position);
        int needed = Mathf.Clamp(Mathf.CeilToInt(distance / 0.22f), 1, 24);

        while (links.Count < needed)
        {
            GameObject link = Instantiate(chainPrefab, transform);
            link.name = "Captive Chain Link";
            foreach (Collider c in link.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            links.Add(link.transform);
        }

        for (int i = 0; i < links.Count; i++)
        {
            bool active = i < needed;
            links[i].gameObject.SetActive(active);
            if (!active) continue;

            float t = needed <= 1 ? 0.5f : i / (float)(needed - 1);
            Vector3 p = Vector3.Lerp(start.position, end.position, t);
            p += Vector3.down * Mathf.Sin(t * Mathf.PI) * Mathf.Min(0.25f, distance * 0.08f);
            links[i].position = p;

            Vector3 dir = end.position - p;
            if (dir.sqrMagnitude > 0.001f)
                links[i].rotation = Quaternion.LookRotation(dir.normalized);
        }
    }

    private static Transform FindBone(Transform root, string suffix)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }
}
