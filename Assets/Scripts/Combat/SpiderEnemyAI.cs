using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class SpiderEnemyAI : MonoBehaviour
{
    [SerializeField] private Animation legacyAnimation;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CombatHealth health;
    [SerializeField] private Transform player;
    [SerializeField] private CombatHealth playerHealth;

    [Header("Temperament")]
    [SerializeField] private bool provoked;
    [SerializeField] private bool remainProvoked = true;

    [Header("Awareness after being attacked")]
    [SerializeField, Min(1f)] private float detectionRange = 12f;
    [SerializeField, Min(1f)] private float disengageRange = 24f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1.45f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 3.4f;
    [SerializeField, Min(0.5f)] private float patrolRadius = 8f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.55f;

    [Header("Bite")]
    [SerializeField, Min(0f)] private float biteDamage = 6f;
    [SerializeField, Min(0.1f)] private float biteCooldown = 1.45f;
    [SerializeField, Min(0.05f)] private float biteImpactDelay = 0.34f;

    private Vector3 homePosition;
    private float nextAttackTime;
    private float nextPatrolChoiceTime;
    private float nextRepathTime;
    private bool attacking;
    private bool dead;
    private int attackIndex;
    private int hitIndex;
    private string currentAnimation;

    public bool IsProvoked => provoked;

    public void Configure(
        Animation animationReference,
        NavMeshAgent navMeshAgent,
        CombatHealth spiderHealth,
        Transform playerTransform,
        CombatHealth targetHealth)
    {
        legacyAnimation = animationReference;
        agent = navMeshAgent;
        health = spiderHealth;
        player = playerTransform;
        playerHealth = targetHealth;

        provoked = false;
        homePosition = transform.position;

        ConfigureAnimations();
        ConfigureAgent();
    }

    private void Awake()
    {
        if (legacyAnimation == null)
            legacyAnimation = GetComponentInChildren<Animation>(true);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (health == null)
            health = GetComponent<CombatHealth>();

        homePosition = transform.position;

        ConfigureAnimations();
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

    private void Start()
    {
        PlayLoop("idle");

        nextPatrolChoiceTime =
            Time.time +
            Random.Range(1f, 3f);
    }

    private void Update()
    {
        if (dead ||
            attacking ||
            player == null ||
            playerHealth == null ||
            playerHealth.IsDead)
        {
            return;
        }

        if (!EnsureOnNavMesh())
            return;

        // Spiders remain harmless wildlife until the player attacks them.
        if (!provoked)
        {
            Patrol();
            return;
        }

        Vector3 offset =
            player.position -
            transform.position;

        offset.y = 0f;

        float distance =
            offset.magnitude;

        if (distance <= attackRange &&
            Time.time >= nextAttackTime)
        {
            StartCoroutine(
                BiteRoutine());

            return;
        }

        if (distance <= detectionRange ||
            (agent.hasPath &&
             distance <= disengageRange))
        {
            ChasePlayer(distance);
            return;
        }

        if (!remainProvoked &&
            distance > disengageRange)
        {
            provoked = false;
        }

        ReturnHomeOrPatrol();
    }

    private void ChasePlayer(float distance)
    {
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime =
                Time.time + 0.25f;

            agent.speed = chaseSpeed;
            agent.isStopped = false;

            agent.SetDestination(
                FindNearestNavMeshPoint(
                    player.position,
                    4f));
        }

        PlayLoop(
            distance > 5f
                ? "run"
                : "walk");
    }

    private void ReturnHomeOrPatrol()
    {
        float homeDistance =
            Vector3.Distance(
                transform.position,
                homePosition);

        if (homeDistance > patrolRadius * 1.4f)
        {
            agent.speed = patrolSpeed;
            agent.isStopped = false;

            agent.SetDestination(
                FindNearestNavMeshPoint(
                    homePosition,
                    5f));

            PlayLoop("walk");
            return;
        }

        Patrol();
    }

    private void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance <=
                agent.stoppingDistance + 0.25f)
        {
            agent.ResetPath();

            nextPatrolChoiceTime =
                Time.time +
                Random.Range(
                    1.2f,
                    3.8f);
        }

        if (!agent.hasPath &&
            Time.time >= nextPatrolChoiceTime)
        {
            Vector2 circle =
                Random.insideUnitCircle *
                patrolRadius;

            Vector3 desired =
                homePosition +
                new Vector3(
                    circle.x,
                    0f,
                    circle.y);

            if (NavMesh.SamplePosition(
                    desired,
                    out NavMeshHit hit,
                    6f,
                    NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }

            nextPatrolChoiceTime =
                Time.time +
                Random.Range(
                    2.5f,
                    5.5f);
        }

        if (agent.hasPath &&
            agent.velocity.sqrMagnitude > 0.02f)
        {
            PlayLoop("walk");
        }
        else
        {
            PlayLoop("idle");
        }
    }

    private IEnumerator BiteRoutine()
    {
        attacking = true;

        nextAttackTime =
            Time.time +
            biteCooldown;

        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        FacePlayer();

        string attack =
            attackIndex++ % 2 == 0
                ? "attack1"
                : "attack2";

        PlayOneShot(attack);

        yield return new WaitForSeconds(
            biteImpactDelay);

        if (!dead &&
            player != null &&
            playerHealth != null &&
            !playerHealth.IsDead)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.position);

            if (distance <=
                attackRange + 0.5f)
            {
                playerHealth.ApplyDamage(
                    biteDamage,
                    gameObject);
            }
        }

        yield return new WaitForSeconds(
            0.38f);

        attacking = false;

        if (!dead &&
            agent != null &&
            agent.enabled &&
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

        CombatHealth attackerHealth =
            source != null
                ? source.GetComponentInParent<CombatHealth>()
                : null;

        // Only player aggression wakes the spider into combat.
        if (attackerHealth == null ||
            attackerHealth.Team == CombatTeam.Player)
        {
            provoked = true;
        }

        if (attacking)
            return;

        string hit =
            hitIndex++ % 2 == 0
                ? "hit1"
                : "hit2";

        PlayOneShot(hit);
    }

    private void HandleDeath(
        CombatHealth deadHealth,
        GameObject source)
    {
        if (dead)
            return;

        StopAllCoroutines();

        dead = true;
        attacking = false;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        PlayOneShot(
            Random.value < 0.5f
                ? "death1"
                : "death2");

        StartCoroutine(
            DeathCleanupRoutine());
    }

    private IEnumerator DeathCleanupRoutine()
    {
        yield return new WaitForSeconds(
            0.75f);

        if (agent != null)
            agent.enabled = false;

        foreach (Collider collider in
                 GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        yield return new WaitForSeconds(
            4.5f);

        Destroy(gameObject);
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = patrolSpeed;
        agent.acceleration = 18f;
        agent.angularSpeed = 900f;
        agent.stoppingDistance =
            attackRange * 0.68f;

        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.updateUpAxis = false;

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        agent.avoidancePriority = 72;
    }

    private void ConfigureAnimations()
    {
        if (legacyAnimation == null)
            return;

        SetWrapMode(
            "idle",
            WrapMode.Loop);

        SetWrapMode(
            "walk",
            WrapMode.Loop);

        SetWrapMode(
            "run",
            WrapMode.Loop);

        SetWrapMode(
            "attack1",
            WrapMode.ClampForever);

        SetWrapMode(
            "attack2",
            WrapMode.ClampForever);

        SetWrapMode(
            "hit1",
            WrapMode.ClampForever);

        SetWrapMode(
            "hit2",
            WrapMode.ClampForever);

        SetWrapMode(
            "death1",
            WrapMode.ClampForever);

        SetWrapMode(
            "death2",
            WrapMode.ClampForever);

        SetWrapMode(
            "jump",
            WrapMode.ClampForever);

        SetWrapMode(
            "taunt",
            WrapMode.ClampForever);
    }

    private void SetWrapMode(
        string clipName,
        WrapMode wrapMode)
    {
        if (legacyAnimation == null ||
            legacyAnimation[clipName] == null)
        {
            return;
        }

        legacyAnimation[clipName].wrapMode =
            wrapMode;
    }

    private void PlayLoop(string clipName)
    {
        if (legacyAnimation == null ||
            legacyAnimation[clipName] == null)
        {
            return;
        }

        if (currentAnimation == clipName &&
            legacyAnimation.IsPlaying(
                clipName))
        {
            return;
        }

        currentAnimation = clipName;

        legacyAnimation.CrossFade(
            clipName,
            0.12f);
    }

    private void PlayOneShot(
        string clipName)
    {
        if (legacyAnimation == null ||
            legacyAnimation[clipName] == null)
        {
            return;
        }

        currentAnimation = clipName;
        legacyAnimation.Play(clipName);
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
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
                12f,
                NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position);
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
