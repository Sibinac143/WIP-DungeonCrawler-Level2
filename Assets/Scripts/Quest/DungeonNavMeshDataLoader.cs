using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class DungeonNavMeshDataLoader : MonoBehaviour
{
    [SerializeField] private NavMeshData navMeshData;

    private NavMeshDataInstance instance;
    private bool loaded;

    public bool Loaded => loaded;

    public void Configure(NavMeshData data)
    {
        navMeshData = data;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying ||
            navMeshData == null ||
            loaded)
        {
            return;
        }

        instance = NavMesh.AddNavMeshData(navMeshData);
        loaded = instance.valid;
    }

    private void OnDisable()
    {
        if (!loaded)
            return;

        instance.Remove();
        loaded = false;
    }
}
