using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class BlacksmithStoryActor : MonoBehaviour
{
    private const string StandingState = "Stand";
    private const string WalkingState = "Walk";

    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform player;

    [Header("Escort movement")]
    [SerializeField, Min(0.5f)] private float moveSpeed = 3.75f;
    [SerializeField, Min(2f)] private float waitForPlayerDistance = 20f;
    [SerializeField, Min(1f)] private float resumeDistance = 13f;
    [SerializeField, Min(0.1f)] private float waypointTolerance = 0.75f;

    private readonly List<Transform> route =
        new List<Transform>();

    private int routeIndex;
    private bool guiding;
    private bool waitingForPlayer;
    private bool tiedToTree;
    private Action routeCompleted;
    private string requestedState;
    private float repathAt;
    private Vector3 lastProgressPosition;
    private float stuckSeconds;
    private int detourDirection = 1;
    private bool usingTemporaryDetour;

    private Vector3 visualLocalPosition;
    private Quaternion visualLocalRotation;
    private Vector3 visualLocalScale;
    private bool visualPoseCached;

    public bool IsGuiding => guiding;
    public bool IsWaitingForPlayer => waitingForPlayer;
    public Transform ActorRoot => transform;
    public Animator Animator => animator;

    public void Configure(
        Transform actorVisualRoot,
        Animator actorAnimator,
        NavMeshAgent navMeshAgent,
        Transform playerTransform)
    {
        visualRoot = actorVisualRoot;
        animator = actorAnimator;
        agent = navMeshAgent;
        player = playerTransform;

        CacheVisualLocalPose();
        ConfigureAnimator();
        ConfigureAgent();
        ForceStandingPose();
    }

    private void Awake()
    {
        if (visualRoot == null &&
            animator != null)
        {
            visualRoot = animator.transform;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (visualRoot == null &&
            animator != null)
        {
            visualRoot = animator.transform;
        }

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        CacheVisualLocalPose();
        ConfigureAnimator();
        ConfigureAgent();
    }

    private void OnEnable()
    {
        RestoreVisualLocalPose();

        if (!guiding &&
            !tiedToTree)
        {
            ForceStandingPose();
        }
    }

    public void BeginRoute(
        IEnumerable<Transform> waypoints,
        Action completed)
    {
        route.Clear();

        if (waypoints != null)
        {
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    route.Add(waypoint);
            }
        }

        tiedToTree = false;
        routeIndex = 0;
        routeCompleted = completed;
        waitingForPlayer = false;
        guiding = route.Count > 0;
        lastProgressPosition = transform.position;
        stuckSeconds = 0f;
        usingTemporaryDetour = false;

        if (agent != null &&
            !agent.enabled)
        {
            agent.enabled = true;
        }

        KeepActorUpright();
        RestoreVisualLocalPose();

        if (!guiding)
        {
            ForceStandingPose();
            return;
        }

        if (!EnsureOnNavMesh())
        {
            guiding = false;
            ForceStandingPose();
            return;
        }

        SetCurrentDestination();
    }

    public void StopGuiding()
    {
        guiding = false;
        waitingForPlayer = false;
        route.Clear();
        routeCompleted = null;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        ForceStandingPose();
    }

    public void PlaceAt(
        Transform point,
        string ignoredStateName = StandingState)
    {
        if (point == null)
            return;

        gameObject.SetActive(true);
        tiedToTree = false;

        if (agent != null &&
            !agent.enabled)
        {
            agent.enabled = true;
        }

        Vector3 position =
            FindNearestNavMeshPoint(
                point.position,
                8f);

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }

        SetYawOnly(point.rotation);
        StopGuiding();
        RestoreVisualLocalPose();
        ForceStandingPose();
    }

    public void TieToTree(
        Transform captivePoint)
    {
        if (captivePoint == null)
            return;

        guiding = false;
        waitingForPlayer = false;
        tiedToTree = true;
        route.Clear();
        routeCompleted = null;

        if (agent != null &&
            agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        transform.position = captivePoint.position;
        SetYawOnly(captivePoint.rotation);

        RestoreVisualLocalPose();
        ForceStandingPose();
    }

    public void PlayState(
        string stateName,
        float transitionDuration = 0.12f)
    {
        if (string.Equals(
                stateName,
                "Walk",
                StringComparison.OrdinalIgnoreCase))
        {
            PlayWalkingPose(transitionDuration);
        }
        else
        {
            ForceStandingPose();
        }
    }

    private void Update()
    {
        KeepActorUpright();
        RestoreVisualLocalPose();

        if (tiedToTree)
        {
            ForceStandingPose();
            return;
        }

        if (!guiding ||
            agent == null ||
            !agent.enabled)
        {
            ForceStandingPose();
            return;
        }

        if (!EnsureOnNavMesh())
        {
            ForceStandingPose();
            return;
        }

        if (player != null)
        {
            float playerDistance =
                Vector3.Distance(
                    transform.position,
                    player.position);

            if (!waitingForPlayer &&
                playerDistance > waitForPlayerDistance)
            {
                waitingForPlayer = true;
                agent.isStopped = true;
                ForceStandingPose();
            }
            else if (waitingForPlayer &&
                     playerDistance <= resumeDistance)
            {
                waitingForPlayer = false;
                agent.isStopped = false;
                SetCurrentDestination();
            }

            if (waitingForPlayer)
            {
                FacePlayer();
                return;
            }
        }

        MonitorAndRecoverFromObstruction();

        if (agent.pathPending)
        {
            PlayWalkingPose(0.08f);
            return;
        }

        if (agent.pathStatus ==
                NavMeshPathStatus.PathInvalid ||
            (!agent.hasPath &&
             Time.time >= repathAt))
        {
            repathAt = Time.time + 0.5f;
            SetCurrentDestination();
        }

        if (HasReachedDestination())
        {
            if (usingTemporaryDetour)
            {
                usingTemporaryDetour = false;
                SetCurrentDestination();
                return;
            }

            routeIndex++;

            if (routeIndex >= route.Count)
            {
                guiding = false;
                agent.isStopped = true;
                agent.ResetPath();
                ForceStandingPose();

                Action callback = routeCompleted;
                routeCompleted = null;
                callback?.Invoke();
                return;
            }

            SetCurrentDestination();
        }

        if (agent.velocity.sqrMagnitude > 0.04f)
            PlayWalkingPose(0.08f);
        else
            ForceStandingPose();
    }

    private void LateUpdate()
    {
        KeepActorUpright();
        RestoreVisualLocalPose();

        if (tiedToTree ||
            !guiding ||
            waitingForPlayer)
        {
            ForceStandingPose();
        }
    }

    private void MonitorAndRecoverFromObstruction()
    {
        if (!guiding ||
            waitingForPlayer ||
            agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh ||
            routeIndex >= route.Count)
        {
            stuckSeconds = 0f;
            lastProgressPosition = transform.position;
            return;
        }

        float movement =
            Vector3.Distance(
                transform.position,
                lastProgressPosition);

        if (movement >= 0.09f)
        {
            stuckSeconds = 0f;
            lastProgressPosition = transform.position;
            return;
        }

        if (agent.pathPending ||
            agent.remainingDistance <=
            agent.stoppingDistance + 0.5f)
        {
            return;
        }

        stuckSeconds += Time.deltaTime;

        if (stuckSeconds >= 1.5f &&
            stuckSeconds < 3.8f &&
            Time.time >= repathAt)
        {
            repathAt = Time.time + 0.55f;
            TrySideDetour();
        }

        if (stuckSeconds >= 3.8f)
        {
            RecoverToNearestClearNavMeshPoint();
            stuckSeconds = 0f;
            lastProgressPosition = transform.position;
        }
    }

    private void TrySideDetour()
    {
        if (routeIndex >= route.Count ||
            agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        Vector3 destination =
            route[routeIndex].position;

        Vector3 forward =
            destination -
            transform.position;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;

        forward.Normalize();

        Vector3 side =
            Vector3.Cross(
                Vector3.up,
                forward) *
            detourDirection;

        detourDirection *= -1;

        Vector3 candidate =
            transform.position +
            forward * 1.5f +
            side * 2.7f;

        if (!NavMesh.SamplePosition(
                candidate,
                out NavMeshHit hit,
                3.5f,
                NavMesh.AllAreas))
        {
            agent.SetDestination(
                FindNearestNavMeshPoint(
                    destination,
                    10f));

            return;
        }

        NavMeshPath path =
            new NavMeshPath();

        if (NavMesh.CalculatePath(
                transform.position,
                hit.position,
                NavMesh.AllAreas,
                path) &&
            path.status ==
                NavMeshPathStatus.PathComplete)
        {
            usingTemporaryDetour = true;
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    private void RecoverToNearestClearNavMeshPoint()
    {
        if (routeIndex >= route.Count ||
            agent == null)
        {
            return;
        }

        Vector3 destination =
            route[routeIndex].position;

        Vector3 bestPoint =
            transform.position;

        float bestScore =
            float.PositiveInfinity;

        for (int index = 0;
             index < 12;
             index++)
        {
            float angle =
                index *
                30f *
                Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)) *
                3.2f;

            if (!NavMesh.SamplePosition(
                    transform.position + offset,
                    out NavMeshHit hit,
                    2.2f,
                    NavMesh.AllAreas))
            {
                continue;
            }

            NavMeshPath candidatePath =
                new NavMeshPath();

            if (!NavMesh.CalculatePath(
                    hit.position,
                    destination,
                    NavMesh.AllAreas,
                    candidatePath) ||
                candidatePath.status !=
                    NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            float score =
                Vector3.Distance(
                    hit.position,
                    destination);

            if (score < bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        if (bestScore <
            float.PositiveInfinity)
        {
            agent.Warp(bestPoint);
        }

        agent.isStopped = false;

        agent.SetDestination(
            FindNearestNavMeshPoint(
                destination,
                12f));
    }

    private void ConfigureAnimator()
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.cullingMode =
            AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode =
            AnimatorUpdateMode.Normal;
        animator.speed = 1f;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.speed = moveSpeed;
        agent.acceleration = 18f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = waypointTolerance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.updatePosition = true;
        agent.updateRotation = true;

        // Critical: never tilt the actor to match a steep NavMesh polygon.
        agent.updateUpAxis = false;

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = 25;
    }

    private void SetCurrentDestination()
    {
        if (!guiding ||
            routeIndex >= route.Count ||
            agent == null ||
            !EnsureOnNavMesh())
        {
            return;
        }

        Vector3 destination =
            FindNearestNavMeshPoint(
                route[routeIndex].position,
                12f);

        usingTemporaryDetour = false;
        agent.isStopped = false;
        agent.SetDestination(destination);
        repathAt = Time.time + 0.75f;
        PlayWalkingPose(0.08f);
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending)
            return false;

        if (agent.remainingDistance >
            agent.stoppingDistance + 0.15f)
        {
            return false;
        }

        return
            !agent.hasPath ||
            agent.velocity.sqrMagnitude < 0.04f;
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
                15f,
                NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position);
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion target =
            Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                target,
                7f * Time.deltaTime);

        KeepActorUpright();
    }

    private void ForceStandingPose()
    {
        PlayAnimatorState(
            StandingState,
            0.04f,
            true);
    }

    private void PlayWalkingPose(
        float transitionDuration)
    {
        PlayAnimatorState(
            WalkingState,
            transitionDuration,
            false);
    }

    private void PlayAnimatorState(
        string stateName,
        float transitionDuration,
        bool force)
    {
        if (animator == null ||
            !Application.isPlaying)
        {
            return;
        }

        if (!force &&
            requestedState == stateName)
        {
            return;
        }

        requestedState = stateName;
        animator.speed = 1f;

        if (force)
        {
            AnimatorStateInfo current =
                animator.GetCurrentAnimatorStateInfo(0);

            if (!current.IsName(stateName) ||
                animator.IsInTransition(0))
            {
                animator.Play(
                    stateName,
                    0,
                    0f);

                animator.Update(0f);
            }

            return;
        }

        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            0);
    }

    private void CacheVisualLocalPose()
    {
        if (visualPoseCached ||
            visualRoot == null)
        {
            return;
        }

        visualLocalPosition =
            visualRoot.localPosition;

        visualLocalRotation =
            visualRoot.localRotation;

        visualLocalScale =
            visualRoot.localScale;

        visualPoseCached = true;
    }

    private void RestoreVisualLocalPose()
    {
        if (!visualPoseCached ||
            visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition =
            visualLocalPosition;

        visualRoot.localRotation =
            visualLocalRotation;

        visualRoot.localScale =
            visualLocalScale;
    }

    private void KeepActorUpright()
    {
        Vector3 euler =
            transform.eulerAngles;

        transform.rotation =
            Quaternion.Euler(
                0f,
                euler.y,
                0f);
    }

    private void SetYawOnly(
        Quaternion rotation)
    {
        Vector3 forward =
            rotation * Vector3.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.001f)
            forward = transform.forward;

        transform.rotation =
            Quaternion.LookRotation(
                forward.normalized,
                Vector3.up);
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
