using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class KeyMakerGuide : MonoBehaviour
{
    [SerializeField] private Transform keyMaker;
    [SerializeField] private Transform player;
    [SerializeField] private Transform castleExit;
    [SerializeField] private Transform workshopDestination;
    [SerializeField, Min(0.5f)] private float moveSpeed = 4.8f;
    [SerializeField, Min(2f)] private float waitForPlayerDistance = 22f;
    [SerializeField, Min(1f)] private float resumeDistance = 15f;
    [SerializeField, Min(0.1f)] private float waypointTolerance = 1.4f;

    private readonly List<Vector3> route =
        new List<Vector3>();

    private int routeIndex;
    private float footOffset;
    private bool guiding;
    private bool waitingForPlayer;

    public bool IsGuiding => guiding;

    public void Configure(
        Transform keyMakerTransform,
        Transform playerTransform,
        Transform castleExitTransform,
        Transform workshopTransform)
    {
        keyMaker = keyMakerTransform;
        player = playerTransform;
        castleExit = castleExitTransform;
        workshopDestination = workshopTransform;

        CalculateFootOffset();
    }

    public void BeginGuide()
    {
        if (keyMaker == null ||
            workshopDestination == null)
        {
            return;
        }

        keyMaker.gameObject.SetActive(true);
        BuildRoute();
        routeIndex = 0;
        waitingForPlayer = false;
        guiding = route.Count > 0;
    }

    public void MoveToWorkshopInstantly()
    {
        if (keyMaker == null ||
            workshopDestination == null)
        {
            return;
        }

        keyMaker.gameObject.SetActive(true);
        keyMaker.SetPositionAndRotation(
            workshopDestination.position,
            workshopDestination.rotation);

        guiding = false;
        waitingForPlayer = false;
    }

    private void Update()
    {
        if (!guiding ||
            keyMaker == null ||
            routeIndex >= route.Count)
        {
            return;
        }

        if (player != null)
        {
            float distanceToPlayer =
                Vector3.Distance(
                    keyMaker.position,
                    player.position);

            if (!waitingForPlayer &&
                distanceToPlayer > waitForPlayerDistance)
            {
                waitingForPlayer = true;
            }
            else if (waitingForPlayer &&
                     distanceToPlayer <= resumeDistance)
            {
                waitingForPlayer = false;
            }

            if (waitingForPlayer)
            {
                FacePlayer();
                return;
            }
        }

        Vector3 target = route[routeIndex];
        Vector3 current = keyMaker.position;

        Vector3 horizontal =
            new Vector3(
                target.x - current.x,
                0f,
                target.z - current.z);

        if (horizontal.magnitude <= waypointTolerance)
        {
            routeIndex++;

            if (routeIndex >= route.Count)
            {
                ArriveAtWorkshop();
                return;
            }

            target = route[routeIndex];

            horizontal =
                new Vector3(
                    target.x - current.x,
                    0f,
                    target.z - current.z);
        }

        if (horizontal.sqrMagnitude > 0.001f)
        {
            Vector3 direction =
                horizontal.normalized;

            keyMaker.rotation =
                Quaternion.Slerp(
                    keyMaker.rotation,
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up),
                    10f * Time.deltaTime);

            Vector3 next =
                current +
                direction *
                moveSpeed *
                Time.deltaTime;

            next.y = SampleTerrainHeight(next) + footOffset;
            keyMaker.position = next;
        }
    }

    private void BuildRoute()
    {
        route.Clear();

        Vector3 start = keyMaker.position;

        if (castleExit != null)
            route.Add(GroundPoint(castleExit.position));

        Vector3 routeStart =
            castleExit != null
                ? castleExit.position
                : start;

        Vector3 end = workshopDestination.position;
        Vector3 direction = end - routeStart;
        direction.y = 0f;

        Vector3 perpendicular =
            direction.sqrMagnitude > 0.001f
                ? Vector3.Cross(
                    Vector3.up,
                    direction.normalized)
                : Vector3.right;

        route.Add(
            GroundPoint(
                Vector3.Lerp(
                    routeStart,
                    end,
                    0.25f) +
                perpendicular * 18f));

        route.Add(
            GroundPoint(
                Vector3.Lerp(
                    routeStart,
                    end,
                    0.50f) -
                perpendicular * 13f));

        route.Add(
            GroundPoint(
                Vector3.Lerp(
                    routeStart,
                    end,
                    0.75f) +
                perpendicular * 8f));

        route.Add(
            GroundPoint(end));
    }

    private void ArriveAtWorkshop()
    {
        guiding = false;
        waitingForPlayer = false;

        if (workshopDestination != null)
        {
            keyMaker.SetPositionAndRotation(
                workshopDestination.position,
                workshopDestination.rotation);
        }

        QuestManager.Instance?.NotifyKeyMakerArrived();
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            keyMaker.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        keyMaker.rotation =
            Quaternion.Slerp(
                keyMaker.rotation,
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up),
                8f * Time.deltaTime);
    }

    private void CalculateFootOffset()
    {
        if (keyMaker == null)
            return;

        Renderer[] renderers =
            keyMaker.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            footOffset = 0f;
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        footOffset =
            keyMaker.position.y -
            bounds.min.y;
    }

    private Vector3 GroundPoint(Vector3 point)
    {
        point.y =
            SampleTerrainHeight(point) +
            footOffset;

        return point;
    }

    private static float SampleTerrainHeight(Vector3 point)
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return point.y;
        }

        return
            terrain.SampleHeight(point) +
            terrain.transform.position.y;
    }
}
