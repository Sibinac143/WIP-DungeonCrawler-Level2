using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class QuestEscapeTrigger : MonoBehaviour
{
    private void Reset()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        QuestManager manager = QuestManager.Instance;

        if (manager == null ||
            !manager.CanInteract(QuestAction.EscapeRealm))
        {
            return;
        }

        CombatHealth health =
            other.GetComponentInParent<CombatHealth>();

        bool isPlayer =
            health != null &&
            health.Team == CombatTeam.Player;

        if (!isPlayer &&
            !other.transform.root.name.Equals(
                "player",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        manager.HandleInteraction(
            QuestAction.EscapeRealm,
            gameObject);
    }
}
