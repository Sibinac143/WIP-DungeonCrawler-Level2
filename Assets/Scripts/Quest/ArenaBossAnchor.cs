using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ArenaBossAnchor : MonoBehaviour
{
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string demonSpawnName = "Arena Demon Spawn";
    [SerializeField] private string playerName = "player";
    [SerializeField] private float holdSeconds = 1.25f;

    private float enteredAt = -1f;
    private Transform player;
    private NavMeshAgent agent;
    private bool anchored;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        Scene arena = SceneManager.GetSceneByName(arenaSceneName);
        if (!arena.IsValid() || !arena.isLoaded)
        {
            enteredAt = -1f;
            anchored = false;
            return;
        }

        Transform spawn = FindInScene(arena, demonSpawnName);
        if (spawn == null)
            return;

        // As soon as the arena is loaded, keep the demon at its proper boss spawn.
        if (!anchored)
        {
            anchored = true;
            enteredAt = Time.time;
            PlaceAtSpawn(spawn);
        }

        // For a short dramatic beat, keep the demon waiting at the far side.
        if (Time.time - enteredAt < holdSeconds)
        {
            PlaceAtSpawn(spawn);
            return;
        }

        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        // If a NavMesh exists in the arena, make sure the demon's agent is attached to it.
        if (agent != null && !agent.enabled)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 4f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.enabled = true;
            }
        }
    }

    private void PlaceAtSpawn(Transform spawn)
    {
        bool agentEnabled = agent != null && agent.enabled;
        if (agentEnabled)
            agent.enabled = false;

        transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        NavMeshHit hit;
        if (agent != null &&
            NavMesh.SamplePosition(spawn.position, out hit, 5f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        if (agentEnabled && agent != null)
        {
            NavMeshHit reconnect;
            if (NavMesh.SamplePosition(transform.position, out reconnect, 4f, NavMesh.AllAreas))
            {
                transform.position = reconnect.position;
                agent.enabled = true;
            }
        }
    }

    private static Transform FindInScene(Scene scene, string exactName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => string.Equals(
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
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.gameObject.scene.IsValid())
            .FirstOrDefault(t => string.Equals(
                t.name,
                exactName,
                StringComparison.OrdinalIgnoreCase))
            ?.gameObject;
    }
}
