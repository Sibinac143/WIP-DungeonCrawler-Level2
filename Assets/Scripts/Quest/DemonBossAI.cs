using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class DemonBossAI : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CombatHealth health;
    [SerializeField] private Transform player;
    [SerializeField] private CombatHealth playerHealth;
    [SerializeField] private FirstPersonStealthController stealthController;
    [SerializeField] private QuestInteractable stealthInteractable;

    [Header("Awareness ranges")]
    [SerializeField, Min(1f)] private float normalDetectionRange = 13f;
    [SerializeField, Min(0.5f)] private float sneakingDetectionRange = 2.1f;
    [SerializeField, Min(1f)] private float stalkToRunRange = 8f;
    [SerializeField, Min(1f)] private float disengageRange = 55f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float stalkSpeed = 2.15f;
    [SerializeField, Min(0.1f)] private float runSpeed = 4.8f;
    [SerializeField, Min(0.1f)] private float ambushRunSpeed = 7.2f;
    [SerializeField, Min(0.1f)] private float attackReach = 3.15f;

    [Header("Attacks")]
    [SerializeField, Min(0f)] private float attackDamage = 28f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 2.15f;
    [SerializeField, Min(0.1f)] private float attackHitDelay = 0.72f;
    [SerializeField, Min(0f)] private float stealthStrikeDamage = 190f;

    private bool fightActive;
    private bool dormant;
    private bool cutsceneActive;
    private bool attacking;
    private bool dead;
    private bool stealthStrikeInProgress;
    private float nextAttackTime;
    private float nextRepathTime;
    private int attackIndex;
    private string requestedState;

    public event Action Defeated;

    public CombatHealth Health => health;
    public bool FightActive => fightActive;
    public bool Dormant => dormant;
    public bool CutsceneActive => cutsceneActive;

    public void Configure(
        Animator demonAnimator,
        NavMeshAgent navMeshAgent,
        CombatHealth demonHealth,
        Transform playerTransform,
        CombatHealth targetHealth,
        FirstPersonStealthController stealth,
        QuestInteractable stealthPrompt)
    {
        animator = demonAnimator;
        agent = navMeshAgent;
        health = demonHealth;
        player = playerTransform;
        playerHealth = targetHealth;
        stealthController = stealth;
        stealthInteractable = stealthPrompt;

        ConfigureAgent();
        SetStealthPrompt(false);
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<CombatHealth>();

        ConfigureAgent();
    }

    private void OnEnable()
    {
        if (health == null)
            health = GetComponent<CombatHealth>();

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
        if (dead ||
            cutsceneActive ||
            attacking ||
            player == null ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            return;
        }

        if (dormant)
        {
            UpdateDormantAwareness();
            return;
        }

        if (!fightActive)
            return;

        UpdateCombatMovement();
    }

    public void BeginAmbushAttack(
        Action impact,
        Action completed)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        if (health != null &&
            health.IsDead)
        {
            health.Revive();
        }

        dead = false;
        dormant = false;
        fightActive = false;
        cutsceneActive = true;
        attacking = false;
        stealthStrikeInProgress = false;
        requestedState = string.Empty;
        SetStealthPrompt(false);

        StartCoroutine(
            AmbushAttackRoutine(
                impact,
                completed));
    }

    public void MoveToGuardAndSleep(
        Transform guardPoint,
        Action completed)
    {
        if (dead)
        {
            completed?.Invoke();
            return;
        }

        StartCoroutine(
            MoveToGuardRoutine(
                guardPoint,
                completed));
    }

    public bool CanStealthStrike()
    {
        if (!dormant ||
            dead ||
            player == null ||
            stealthController == null ||
            !stealthController.IsSneaking)
        {
            return false;
        }

        Vector3 toPlayer =
            player.position -
            transform.position;

        toPlayer.y = 0f;

        float distance =
            toPlayer.magnitude;

        if (distance > 3.35f ||
            distance <= 0.001f)
        {
            return false;
        }

        float behindDot =
            Vector3.Dot(
                transform.forward,
                toPlayer.normalized);

        return behindDot < -0.25f;
    }

    public void PerformStealthStrike()
    {
        if (!CanStealthStrike() ||
            stealthStrikeInProgress)
        {
            return;
        }

        StartCoroutine(
            StealthStrikeRoutine());
    }

    public void BeginCombat()
    {
        if (dead)
            return;

        dormant = false;
        fightActive = true;
        cutsceneActive = false;
        attacking = false;
        nextAttackTime = Time.time + 0.65f;
        SetStealthPrompt(false);

        if (EnsureOnNavMesh())
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
        }

        PlayState("Run", 0.1f);
    }

    public void PlaceDormantAt(
        Transform point)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        if (health != null &&
            health.IsDead)
        {
            health.Revive();
        }

        dead = false;
        cutsceneActive = false;
        attacking = false;
        stealthStrikeInProgress = false;
        requestedState = string.Empty;

        Vector3 desired =
            point != null
                ? point.position
                : transform.position;

        Vector3 position =
            FindNearestNavMeshPoint(
                desired,
                12f);

        if (EnsureOnNavMesh())
            agent.Warp(position);
        else
            transform.position = position;

        if (point != null)
            transform.rotation = point.rotation;

        EnterDormant();
    }

    public void SetDefeatedInstantly()
    {
        StopAllCoroutines();

        fightActive = false;
        dormant = false;
        cutsceneActive = false;
        attacking = false;
        dead = true;
        SetStealthPrompt(false);

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        PlayState("Death", 0.02f);
    }

    private IEnumerator AmbushAttackRoutine(
        Action impact,
        Action completed)
    {
        if (!EnsureOnNavMesh())
        {
            cutsceneActive = false;
            impact?.Invoke();
            completed?.Invoke();
            yield break;
        }

        agent.isStopped = true;
        PlayState("Jump_In", 0.03f);

        yield return new WaitForSeconds(0.46f);

        Vector3 strikePosition =
            player.position +
            player.forward * 2.45f;

        strikePosition =
            FindNearestNavMeshPoint(
                strikePosition,
                8f);

        agent.speed = ambushRunSpeed;
        agent.acceleration = 35f;
        agent.isStopped = false;
        agent.SetDestination(strikePosition);
        PlayState("Run", 0.06f);

        float timeout = Time.time + 3.25f;

        while (Time.time < timeout)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= 2.65f)
            {
                break;
            }

            yield return null;
        }

        agent.isStopped = true;
        agent.ResetPath();

        yield return FacePlayerRoutine(0.18f);

        PlayState("Attack_L", 0.04f);

        yield return new WaitForSeconds(
            attackHitDelay);

        impact?.Invoke();

        yield return new WaitForSeconds(0.75f);

        cutsceneActive = false;
        completed?.Invoke();
    }

    private IEnumerator MoveToGuardRoutine(
        Transform guardPoint,
        Action completed)
    {
        if (guardPoint == null ||
            !EnsureOnNavMesh())
        {
            EnterDormant();
            completed?.Invoke();
            yield break;
        }

        cutsceneActive = true;
        fightActive = false;
        dormant = false;
        SetStealthPrompt(false);

        Vector3 target =
            FindNearestNavMeshPoint(
                guardPoint.position,
                10f);

        agent.speed = runSpeed;
        agent.acceleration = 24f;
        agent.isStopped = false;
        agent.SetDestination(target);
        PlayState("Walk", 0.1f);

        float timeout = Time.time + 5f;

        while (Time.time < timeout)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <=
                agent.stoppingDistance + 0.3f)
            {
                break;
            }

            yield return null;
        }

        agent.isStopped = true;
        agent.ResetPath();

        if (player != null)
            yield return FacePlayerRoutine(0.2f);

        cutsceneActive = false;
        EnterDormant();
        completed?.Invoke();
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

        Quaternion start = transform.rotation;
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

    private IEnumerator StealthStrikeRoutine()
    {
        stealthStrikeInProgress = true;
        SetStealthPrompt(false);

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        PlayState("Hit", 0.03f);

        if (health != null)
        {
            health.ApplyDamage(
                stealthStrikeDamage,
                player != null
                    ? player.gameObject
                    : gameObject);
        }

        yield return new WaitForSeconds(0.42f);

        stealthStrikeInProgress = false;

        if (!dead)
            BeginCombat();
    }

    private void EnterDormant()
    {
        if (dead)
            return;

        fightActive = false;
        dormant = true;
        attacking = false;
        nextAttackTime = Time.time + 0.5f;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        PlayState("Fidget", 0.1f);
        SetStealthPrompt(true);
    }

    private void UpdateDormantAwareness()
    {
        if (player == null)
            return;

        Vector3 toPlayer =
            player.position -
            transform.position;

        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        bool sneaking =
            stealthController != null &&
            stealthController.IsSneaking;

        float detectionRange =
            sneaking
                ? sneakingDetectionRange
                : normalDetectionRange;

        if (sneaking &&
            distance > 0.001f)
        {
            float behindDot =
                Vector3.Dot(
                    transform.forward,
                    toPlayer.normalized);

            if (behindDot < -0.2f)
                detectionRange *= 0.7f;
        }

        if (distance <= detectionRange)
            BeginCombat();
        else
            PlayState("Fidget", 0.12f);
    }

    private void UpdateCombatMovement()
    {
        if (!EnsureOnNavMesh())
            return;

        Vector3 toPlayer =
            player.position -
            transform.position;

        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > disengageRange)
        {
            agent.isStopped = true;
            PlayState("Idle", 0.15f);
            return;
        }

        if (distance <= attackReach &&
            Time.time >= nextAttackTime)
        {
            StartCoroutine(
                AttackRoutine());
            return;
        }

        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + 0.18f;

            agent.isStopped = false;
            agent.speed =
                distance > stalkToRunRange
                    ? stalkSpeed
                    : runSpeed;

            agent.SetDestination(
                FindNearestNavMeshPoint(
                    player.position,
                    4f));
        }

        if (distance > stalkToRunRange)
            PlayState("Walk", 0.1f);
        else
            PlayState("Run", 0.1f);
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

        yield return FacePlayerRoutine(0.12f);

        string attackState =
            attackIndex++ % 2 == 0
                ? "Attack_L"
                : "Attack_R";

        PlayState(attackState, 0.04f);

        yield return new WaitForSeconds(
            attackHitDelay);

        if (!dead &&
            player != null &&
            playerHealth != null &&
            !playerHealth.IsDead)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position);

            if (distance <= attackReach + 0.8f)
            {
                playerHealth.ApplyDamage(
                    attackDamage,
                    gameObject);
            }
        }

        yield return new WaitForSeconds(0.62f);

        attacking = false;

        if (!dead &&
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
        if (dead)
            return;

        if (dormant &&
            !stealthStrikeInProgress)
        {
            BeginCombat();
        }

        if (!attacking)
            PlayState("Hit", 0.04f);
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        if (dead)
            return;

        StopAllCoroutines();

        dead = true;
        fightActive = false;
        dormant = false;
        cutsceneActive = false;
        attacking = false;
        SetStealthPrompt(false);

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        PlayState("Death", 0.04f);
        StartCoroutine(
            DisableBlockingAfterDeath());

        Defeated?.Invoke();
    }

    private IEnumerator DisableBlockingAfterDeath()
    {
        yield return new WaitForSeconds(0.9f);

        if (agent != null)
            agent.enabled = false;

        foreach (Collider collider in
                 GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = runSpeed;
        agent.acceleration = 24f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = attackReach * 0.72f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 65;
    }

    private bool EnsureOnNavMesh()
    {
        if (agent == null ||
            !agent.enabled)
        {
            return false;
        }

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

    private void PlayState(
        string stateName,
        float transitionDuration)
    {
        if (animator == null ||
            string.IsNullOrWhiteSpace(stateName) ||
            requestedState == stateName)
        {
            return;
        }

        requestedState = stateName;

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            0);
    }

    private void SetStealthPrompt(
        bool active)
    {
        if (stealthInteractable != null)
            stealthInteractable.gameObject.SetActive(active);
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
