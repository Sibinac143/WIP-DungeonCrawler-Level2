using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestInteractable : MonoBehaviour
{
    private static readonly List<QuestInteractable> ActiveItems =
        new List<QuestInteractable>();

    [SerializeField] private QuestAction action;
    [SerializeField] private string customPrompt;
    [SerializeField, Min(0.1f)] private float interactionRadius = 5f;
    [SerializeField] private Transform interactionPoint;

    public static IReadOnlyList<QuestInteractable> Active => ActiveItems;
    public QuestAction Action => action;
    public float InteractionRadius => interactionRadius;
    public Vector3 WorldPosition =>
        interactionPoint != null
            ? interactionPoint.position
            : transform.position;

    public void Configure(
        QuestAction newAction,
        string newPrompt,
        float newInteractionRadius,
        Transform newInteractionPoint = null)
    {
        action = newAction;
        customPrompt = newPrompt;
        interactionRadius = Mathf.Max(0.1f, newInteractionRadius);
        interactionPoint = newInteractionPoint;
    }

    private void OnEnable()
    {
        if (!ActiveItems.Contains(this))
            ActiveItems.Add(this);
    }

    private void OnDisable()
    {
        ActiveItems.Remove(this);
    }

    public bool IsAvailable()
    {
        return QuestManager.Instance != null &&
               QuestManager.Instance.CanInteract(action);
    }

    public string GetPrompt()
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
            return customPrompt;

        return QuestManager.Instance != null
            ? QuestManager.Instance.GetPrompt(action)
            : "Press F to interact";
    }

    public void Interact()
    {
        if (!IsAvailable())
            return;

        QuestManager.Instance.HandleInteraction(
            action,
            gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            WorldPosition,
            interactionRadius);
    }
}
