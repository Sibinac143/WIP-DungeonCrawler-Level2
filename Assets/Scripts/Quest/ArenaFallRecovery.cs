using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ArenaFallRecovery : MonoBehaviour
{
    [SerializeField] private string arenaSceneName = "DemonArena";
    [SerializeField] private string playerName = "player";
    [SerializeField] private string fallbackSpawnName = "Arena Player Spawn";
    [SerializeField] private float minimumY = -2.5f;

    private Transform player;

    private void Update()
    {
        Scene arena = SceneManager.GetSceneByName(arenaSceneName);

        if (!arena.IsValid() || !arena.isLoaded)
            return;

        if (player == null)
        {
            GameObject p = FindSceneObject(playerName);
            if (p != null)
                player = p.transform;
        }

        if (player != null && player.position.y < minimumY)
            Recover(player, FindInScene(arena, fallbackSpawnName), new Vector3(0f, 2f, -13f));

        if (transform.position.y < minimumY)
            Recover(transform, FindInScene(arena, "Arena Demon Spawn"), new Vector3(0f, 1.5f, 10f));
    }

    private static void Recover(Transform actor, Transform spawn, Vector3 fallback)
    {
        Vector3 position = spawn != null ? spawn.position : fallback;
        Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

        CharacterController cc = actor.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = actor.GetComponent<UnityEngine.AI.NavMeshAgent>();
        bool agentEnabled = agent != null && agent.enabled;

        if (agentEnabled)
            agent.enabled = false;

        actor.SetPositionAndRotation(position, rotation);

        if (cc != null)
            cc.enabled = true;

        if (agentEnabled)
            agent.enabled = true;

        Debug.Log("[ArenaFallRecovery] Recovered " + actor.name + " inside DemonArena.");
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
