using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GateGuardianAI : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CombatHealth health;
    [SerializeField] private Transform player;
    [SerializeField] private CombatHealth playerHealth;
    [SerializeField] private Collider bodyCollider;

    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float detectionRange = 55f;
    [SerializeField, Min(0.1f)] private float walkSpeed = 2.1f;
    [SerializeField, Min(0.1f)] private float runSpeed = 4.7f;
    [SerializeField, Min(0.1f)] private float runRange = 11f;
    [SerializeField, Min(0.1f)] private float attackRange = 3.25f;
    [SerializeField, Min(0f)] private float attackDamage = 31f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 2.2f;
    [SerializeField, Min(0.1f)] private float attackImpactDelay = 0.82f;

    private bool frozen;
    private bool combatActive;
    private bool attacking;
    private bool dead;
    private bool cinematic;
    private float nextAttackTime;
    private float nextRepathTime;
    private int attackIndex;
    private string requestedState;

    public event Action<GateGuardianAI> Defeated;

    public bool IsDead =>
        dead ||
        (health != null && health.IsDead);

    public CombatHealth Health => health;

    public void Configure(
        Animator demonAnimator,
        NavMeshAgent navMeshAgent,
        CombatHealth demonHealth,
        Transform playerTransform,
        CombatHealth targetHealth,
        Collider physicalCollider)
    {
        animator = demonAnimator;
        agent = navMeshAgent;
        health = demonHealth;
        player = playerTransform;
        playerHealth = targetHealth;
        bodyCollider = physicalCollider;

        ConfigureAgent();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<CombatHealth>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider>();

        ConfigureAgent();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDeath;
            health.Damaged += HandleDamaged;
            health.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDeath;
        }
    }

    private void Update()
    {
        if (frozen)
        {
            MaintainFrozenPose();
            return;
        }

        if (!combatActive ||
            cinematic ||
            attacking ||
            IsDead ||
            player == null ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            return;
        }

        UpdateCombat();
    }

    public void SetFrozen()
    {
        StopAllCoroutines();

        gameObject.SetActive(true);

        if (health != null &&
            health.IsDead)
        {
            health.Revive();
        }

        dead = false;
        frozen = true;
        combatActive = false;
        attacking = false;
        cinematic = false;
        requestedState = string.Empty;

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animator != null)
        {
            if (Application.isPlaying)
            {
                animator.speed = 1f;
                animator.Play(
                    "Demon|Idle1",
                    0,
                    0f);

                animator.Update(0f);
                animator.speed = 0f;
            }
            else
            {
                animator.speed = 1f;
            }
        }
    }

    public void BeginCombat(
        float delay = 0f)
    {
        if (IsDead)
            return;

        StopAllCoroutines();
        StartCoroutine(
            BeginCombatRoutine(delay));
    }

    public void BeginThrowAmbush(
        Transform strikePoint,
        Action impact,
        Action completed)
    {
        if (IsDead)
        {
            completed?.Invoke();
            return;
        }

        StopAllCoroutines();

        StartCoroutine(
            ThrowAmbushRoutine(
                strikePoint,
                impact,
                completed));
    }

    public void SetDefeatedInstantly()
    {
        StopAllCoroutines();

        frozen = false;
        combatActive = false;
        cinematic = false;
        attacking = false;
        dead = true;

        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play(
                "Demon|Death",
                0,
                0.95f);
        }

        DisableBlocking();
    }

    private IEnumerator BeginCombatRoutine(
        float delay)
    {
        frozen = false;
        combatActive = false;
        cinematic = true;
        attacking = false;

        if (animator != null)
            animator.speed = 1f;

        PlayOneShot(
            "Demon|Come-out1");

        yield return new WaitForSeconds(
            Mathf.Max(
                delay,
                1.25f));

        cinematic = false;
        combatActive = true;
        nextAttackTime = Time.time + 0.55f;

        if (EnsureOnNavMesh())
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
        }
    }

    private IEnumerator ThrowAmbushRoutine(
        Transform strikePoint,
        Action impact,
        Action completed)
    {
        frozen = false;
        combatActive = false;
        cinematic = true;
        attacking = false;

        if (animator != null)
            animator.speed = 1f;

        PlayOneShot(
            "Demon|Come-out1");

        yield return new WaitForSeconds(1.15f);

        if (EnsureOnNavMesh() &&
            strikePoint != null)
        {
            Vector3 destination =
                FindNearestNavMeshPoint(
                    strikePoint.position,
                    10f);

            agent.speed = 7f;
            agent.acceleration = 35f;
            agent.isStopped = false;
            agent.SetDestination(destination);

            float timeout =
                Time.time + 3.25f;

            while (Time.time < timeout)
            {
                PlayLooping(
                    "Demon|Run1");

                if (!agent.pathPending &&
                    agent.remainingDistance <=
                    agent.stoppingDistance + 0.4f)
                {
                    break;
                }

                yield return null;
            }

            agent.isStopped = true;
            agent.ResetPath();
        }

        yield return FacePlayerRoutine(0.2f);

        PlayOneShot(
            "Demon|Throw");

        yield return new WaitForSeconds(1.05f);

        impact?.Invoke();

        yield return new WaitForSeconds(0.8f);

        cinematic = false;
        completed?.Invoke();
    }

    private void UpdateCombat()
    {
        if (!EnsureOnNavMesh())
            return;

        Vector3 offset =
            player.position -
            transform.position;

        offset.y = 0f;

        float distance =
            offset.magnitude;

        if (distance > detectionRange)
        {
            agent.isStopped = true;
            PlayLooping(
                "Demon|Idle2");
            return;
        }

        if (distance <= attackRange &&
            Time.time >= nextAttackTime)
        {
            StartCoroutine(
                AttackRoutine());
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime =
                Time.time + 0.18f;

            agent.isStopped = false;

            agent.speed =
                distance > runRange
                    ? walkSpeed
                    : runSpeed;

            agent.SetDestination(
                FindNearestNavMeshPoint(
                    player.position,
                    5f));
        }

        if (distance > runRange)
            PlayLooping("Demon|Walk1");
        else
            PlayLooping("Demon|Run1");
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;

        nextAttackTime =
            Time.time + attackCooldown;

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        yield return FacePlayerRoutine(0.14f);

        string state;

        switch (attackIndex++ % 3)
        {
            case 0:
                state = "Demon|Punch1";
                break;

            case 1:
                state = "Demon|Punch2";
                break;

            default:
                state = "Demon|Punch3";
                break;
        }

        PlayOneShot(state);

        yield return new WaitForSeconds(
            attackImpactDelay);

        if (!IsDead &&
            player != null &&
            playerHealth != null &&
            !playerHealth.IsDead)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position);

            if (distance <= attackRange + 0.85f)
            {
                playerHealth.ApplyDamage(
                    attackDamage,
                    gameObject);
            }
        }

        yield return new WaitForSeconds(0.62f);

        attacking = false;

        if (!IsDead &&
            agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void HandleDamaged(
        CombatHealth damagedHealth,
        float amount,
        GameObject source)
    {
        if (IsDead)
            return;

        if (frozen)
            BeginCombat();

        if (!attacking)
            PlayOneShot("Demon|Get-damage");
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        if (dead)
            return;

        StopAllCoroutines();

        dead = true;
        frozen = false;
        combatActive = false;
        cinematic = false;
        attacking = false;

        PlayOneShot(
            "Demon|Death");

        StartCoroutine(
            DisableBlockingAfterDelay());

        Defeated?.Invoke(this);
    }

    private IEnumerator DisableBlockingAfterDelay()
    {
        yield return new WaitForSeconds(0.9f);
        DisableBlocking();
    }

    private void DisableBlocking()
    {
        if (agent != null)
        {
            if (agent.enabled &&
                agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        if (bodyCollider != null)
            bodyCollider.enabled = false;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = runSpeed;
        agent.acceleration = 24f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance =
            attackRange * 0.72f;

        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // The blacksmith uses a higher priority so demons yield to him.
        agent.avoidancePriority = 65;
    }

    private bool EnsureOnNavMesh()
    {
        if (agent == null)
            return false;

        if (!agent.enabled)
            agent.enabled = true;

        if (agent.isOnNavMesh)
            return true;

        if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                18f,
                NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position);
    }

    private IEnumerator FacePlayerRoutine(
        float duration)
    {
        if (player == null)
            yield break;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            yield break;

        Quaternion start =
            transform.rotation;

        Quaternion target =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            transform.rotation =
                Quaternion.Slerp(
                    start,
                    target,
                    Mathf.Clamp01(
                        elapsed /
                        duration));

            yield return null;
        }

        transform.rotation = target;
    }

    private void MaintainFrozenPose()
    {
        if (animator == null)
            return;

        animator.speed = 0f;
    }

    private void PlayOneShot(
        string stateName)
    {
        if (animator == null)
            return;

        requestedState = stateName;
        animator.speed = 1f;

        animator.Play(
            stateName,
            0,
            0f);
    }

    private void PlayLooping(
        string stateName)
    {
        if (animator == null)
            return;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        bool isRequested =
            state.IsName(stateName);

        if (!isRequested ||
            state.normalizedTime >= 0.92f ||
            requestedState != stateName)
        {
            requestedState = stateName;
            animator.speed = 1f;

            animator.Play(
                stateName,
                0,
                0f);
        }
    }

    private static Vector3 FindNearestNavMeshPoint(
        Vector3 desired,
        float radius)
    {
        if (NavMesh.SamplePosition(
                desired,
                out NavMeshHit hit,
                radius,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return desired;
    }
}
