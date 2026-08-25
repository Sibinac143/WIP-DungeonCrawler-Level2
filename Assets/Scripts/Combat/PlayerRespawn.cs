using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private CombatHealth health;
    [SerializeField] private Transform respawnPoint;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float respawnDelay = 1.25f;
    [SerializeField, Min(0f)] private float postRespawnProtection = 3f;

    [Header("Dynamic enemy respawn")]
    [SerializeField, Min(5f)] private float enemyRespawnDistance = 20f;
    [SerializeField, Min(1f)] private float navMeshSearchRadius = 9f;
    [SerializeField, Min(1f)] private float minimumEnemyDistance = 14f;

    [Header("Safe-position tracking")]
    [SerializeField, Min(0.1f)] private float safePositionInterval = 0.5f;

    private CharacterController characterController;
    private Behaviour firstPersonPlayer;
    private Behaviour firstPersonSword;
    private SwordDamageDealer swordDamageDealer;
    private Rigidbody body;
    private PlayerShield shield;

    private Coroutine respawnRoutine;
    private bool hasDeathSource;
    private Vector3 deathSourcePosition;

    private bool hasLastSafePosition;
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private float nextSafePositionUpdate;

    public void Configure(
        CombatHealth healthReference,
        Transform newRespawnPoint)
    {
        health = healthReference;
        respawnPoint = newRespawnPoint;
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        characterController =
            GetComponent<CharacterController>();

        firstPersonPlayer =
            GetComponent("FirstPersonPlayer")
            as Behaviour;

        firstPersonSword =
            GetComponent("FirstPersonSword")
            as Behaviour;

        swordDamageDealer =
            GetComponent<SwordDamageDealer>();

        body = GetComponent<Rigidbody>();
        shield = GetComponent<PlayerShield>();

        CacheCurrentPositionAsSafe();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

        if (health != null)
        {
            health.Died -= HandleDeath;
            health.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDeath;
    }

    private void Update()
    {
        if (respawnRoutine != null ||
            health == null ||
            health.IsDead ||
            Time.time < nextSafePositionUpdate)
        {
            return;
        }

        nextSafePositionUpdate =
            Time.time + safePositionInterval;

        if (characterController != null &&
            !characterController.isGrounded)
        {
            return;
        }

        if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                2.5f,
                NavMesh.AllAreas) &&
            IsSpawnSpaceClear(hit.position))
        {
            lastSafePosition = hit.position;
            lastSafeRotation = YawOnly(transform.rotation);
            hasLastSafePosition = true;
        }
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        hasDeathSource = false;

        if (source != null)
        {
            CombatHealth sourceHealth =
                source.GetComponentInParent<CombatHealth>();

            Transform sourceTransform =
                sourceHealth != null
                    ? sourceHealth.transform
                    : source.transform;

            deathSourcePosition =
                sourceTransform.position;

            hasDeathSource = true;
        }

        if (respawnRoutine == null)
        {
            respawnRoutine =
                StartCoroutine(
                    RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        SetPlayerControl(false);

        yield return new WaitForSeconds(
            respawnDelay);

        Vector3 destination;
        Quaternion destinationRotation;

        if (hasDeathSource &&
            TryFindPositionAwayFromEnemy(
                deathSourcePosition,
                out destination,
                out destinationRotation))
        {
            // Dynamic enemy-relative destination was found.
        }
        else if (hasLastSafePosition)
        {
            destination = lastSafePosition;
            destinationRotation = lastSafeRotation;
        }
        else if (respawnPoint != null)
        {
            destination = respawnPoint.position;
            destinationRotation =
                YawOnly(respawnPoint.rotation);
        }
        else
        {
            destination = transform.position;
            destinationRotation =
                YawOnly(transform.rotation);
        }

        if (characterController != null)
            characterController.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(
            destination + Vector3.up * 0.08f,
            destinationRotation);

        Physics.SyncTransforms();

        if (characterController != null)
            characterController.enabled = true;

        health.Revive();
        health.GrantInvulnerability(
            postRespawnProtection);

        if (shield != null)
            shield.ClearShield();

        CacheCurrentPositionAsSafe();
        SetPlayerControl(true);

        hasDeathSource = false;
        respawnRoutine = null;
    }

    private bool TryFindPositionAwayFromEnemy(
        Vector3 enemyPosition,
        out Vector3 destination,
        out Quaternion rotation)
    {
        Vector3 preferredDirection =
            transform.position -
            enemyPosition;

        preferredDirection.y = 0f;

        if (preferredDirection.sqrMagnitude <
            0.001f)
        {
            preferredDirection =
                -transform.forward;
        }

        preferredDirection.Normalize();

        float[] angleOffsets =
        {
            0f,
            35f,
            -35f,
            70f,
            -70f,
            110f,
            -110f,
            145f,
            -145f,
            180f
        };

        float[] distances =
        {
            enemyRespawnDistance,
            enemyRespawnDistance + 4f,
            enemyRespawnDistance - 3f
        };

        bool found = false;
        float bestScore = float.NegativeInfinity;
        Vector3 best = transform.position;

        foreach (float distance in distances)
        {
            foreach (float angle in angleOffsets)
            {
                Vector3 direction =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f) *
                    preferredDirection;

                Vector3 desired =
                    enemyPosition +
                    direction *
                    distance;

                if (!NavMesh.SamplePosition(
                        desired,
                        out NavMeshHit hit,
                        navMeshSearchRadius,
                        NavMesh.AllAreas))
                {
                    continue;
                }

                float enemyDistance =
                    Vector3.Distance(
                        hit.position,
                        enemyPosition);

                if (enemyDistance <
                    minimumEnemyDistance)
                {
                    continue;
                }

                if (!IsSpawnSpaceClear(
                        hit.position))
                {
                    continue;
                }

                float score =
                    enemyDistance -
                    Vector3.Distance(
                        hit.position,
                        desired) *
                    0.45f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = hit.position;
                    found = true;
                }
            }
        }

        destination = found
            ? best
            : transform.position;

        Vector3 faceAway =
            destination -
            enemyPosition;

        faceAway.y = 0f;

        if (faceAway.sqrMagnitude <
            0.001f)
        {
            faceAway = preferredDirection;
        }

        rotation =
            Quaternion.LookRotation(
                faceAway.normalized,
                Vector3.up);

        return found;
    }

    private bool IsSpawnSpaceClear(
        Vector3 position)
    {
        Vector3 bottom =
            position +
            Vector3.up * 0.42f;

        Vector3 top =
            position +
            Vector3.up * 1.62f;

        Collider[] overlaps =
            Physics.OverlapCapsule(
                bottom,
                top,
                0.38f,
                ~0,
                QueryTriggerInteraction.Ignore);

        foreach (Collider collider in overlaps)
        {
            if (collider == null ||
                collider is TerrainCollider ||
                collider.transform.IsChildOf(transform))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void CacheCurrentPositionAsSafe()
    {
        Vector3 position =
            transform.position;

        if (NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                3f,
                NavMesh.AllAreas))
        {
            position = hit.position;
        }

        lastSafePosition = position;
        lastSafeRotation = YawOnly(transform.rotation);
        hasLastSafePosition = true;
    }

    private static Quaternion YawOnly(
        Quaternion rotation)
    {
        Vector3 forward =
            rotation *
            Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <
            0.001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(
            forward.normalized,
            Vector3.up);
    }

    private void SetPlayerControl(
        bool enabledState)
    {
        if (firstPersonPlayer != null)
            firstPersonPlayer.enabled = enabledState;

        if (firstPersonSword != null)
            firstPersonSword.enabled = enabledState;

        if (swordDamageDealer != null)
            swordDamageDealer.enabled = enabledState;
    }
}
