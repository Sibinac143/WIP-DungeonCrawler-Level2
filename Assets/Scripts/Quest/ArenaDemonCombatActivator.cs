using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ArenaDemonCombatActivator : MonoBehaviour
{
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string demonSpawnName = "Arena Demon Spawn";
    [SerializeField] private string playerName = "player";
    [SerializeField] private float groundProbeHeight = 8f;
    [SerializeField] private float groundProbeDistance = 30f;
    [SerializeField] private float fallbackMoveSpeed = 3.2f;
    [SerializeField] private float stopDistance = 3.2f;

    private Transform player;
    private NavMeshAgent agent;
    private bool arenaActive;
    private bool initializedForArena;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        Scene arena = SceneManager.GetSceneByName(arenaSceneName);
        bool nowActive = arena.IsValid() && arena.isLoaded;

        if (!nowActive)
        {
            arenaActive = false;
            initializedForArena = false;
            return;
        }

        arenaActive = true;

        if (!initializedForArena)
        {
            initializedForArena = true;
            InitializeArenaCombat(arena);
        }

        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        if (player == null)
            return;

        // Existing AI is preferred. If the NavMeshAgent is valid, keep it attached.
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            if (Vector3.Distance(transform.position, player.position) > stopDistance)
                agent.SetDestination(player.position);

            return;
        }

        // Fallback chase if the imported enemy AI cannot use the arena NavMesh.
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude > stopDistance && toPlayer.sqrMagnitude > 0.01f)
        {
            Vector3 direction = toPlayer.normalized;
            transform.position += direction * fallbackMoveSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime);
        }

        KeepGrounded();
    }

    private void InitializeArenaCombat(Scene arena)
    {
        Transform spawn = FindInScene(arena, demonSpawnName);

        if (spawn != null)
        {
            Vector3 position = FindGroundPosition(spawn.position);
            transform.SetPositionAndRotation(position, spawn.rotation);
        }
        else
        {
            transform.position = FindGroundPosition(transform.position);
        }

        // Re-enable combat/AI scripts that may have been disabled while the demon
        // was guarding the blacksmith in the main world.
        foreach (Behaviour behaviour in GetComponents<Behaviour>())
        {
            if (behaviour == null || behaviour == this)
                continue;

            string typeName = behaviour.GetType().Name;

            bool looksLikeCombatAI =
                typeName.IndexOf("AI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Combat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0;

            if (looksLikeCombatAI)
                behaviour.enabled = true;
        }

        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = false;

            NavMeshHit navHit;

            if (NavMesh.SamplePosition(
                    transform.position,
                    out navHit,
                    12f,
                    NavMesh.AllAreas))
            {
                transform.position = navHit.position;
                agent.enabled = true;
                agent.Warp(navHit.position);

                Debug.Log("[ArenaDemonAI] Demon attached to arena NavMesh.");
            }
            else
            {
                Debug.LogWarning(
                    "[ArenaDemonAI] No NavMesh found near demon spawn. " +
                    "Using fallback chase movement.");
            }
        }

        Animator animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.gameObject.SetActive(true);
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        Debug.Log("[ArenaDemonAI] Demon grounded and combat activated.");
    }

    private void KeepGrounded()
    {
        Vector3 grounded = FindGroundPosition(transform.position);

        // Only correct obvious floating/falling.
        if (Mathf.Abs(transform.position.y - grounded.y) > 0.45f)
        {
            Vector3 p = transform.position;
            p.y = grounded.y;
            transform.position = p;
        }
    }

    private Vector3 FindGroundPosition(Vector3 around)
    {
        RaycastHit hit;

        Vector3 origin =
            new Vector3(
                around.x,
                around.y + groundProbeHeight,
                around.z);

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out hit,
                groundProbeDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return new Vector3(
                around.x,
                hit.point.y + 0.08f,
                around.z);
        }

        return new Vector3(
            around.x,
            0.15f,
            around.z);
    }

    private static Transform FindInScene(Scene scene, string exactName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found =
                root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t =>
                        string.Equals(
                            t.name,
                            exactName,
                            StringComparison.OrdinalIgnoreCase));

            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindSceneObject(string exactName)
    {
        return UnityEngine.Object
            .FindObjectsByType<Transform>(
                FindObjectsInactive.Include)
            .Where(t =>
                t != null &&
                t.gameObject.scene.IsValid())
            .FirstOrDefault(t =>
                string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
