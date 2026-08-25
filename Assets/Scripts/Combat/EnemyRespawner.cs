using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRespawner : MonoBehaviour
{
    [SerializeField] private CombatHealth health;
    [SerializeField, Min(0f)] private float hideDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float respawnDelay = 7f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Coroutine respawnRoutine;

    public void Configure(
        CombatHealth healthReference,
        float newRespawnDelay)
    {
        health = healthReference;
        respawnDelay = Mathf.Max(0.1f, newRespawnDelay);
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        if (health != null)
            health.Died += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDeath;
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        if (respawnRoutine == null)
            respawnRoutine =
                StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(hideDelay);

        SetVisibleAndCollidable(false);

        float remaining =
            Mathf.Max(0f, respawnDelay - hideDelay);

        yield return new WaitForSeconds(remaining);

        transform.SetPositionAndRotation(
            initialPosition,
            initialRotation);

        health.Revive();
        SetVisibleAndCollidable(true);
        respawnRoutine = null;
    }

    private void SetVisibleAndCollidable(bool state)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = state;
        }

        foreach (Collider collider in colliders)
        {
            if (collider != null)
                collider.enabled = state;
        }
    }
}
