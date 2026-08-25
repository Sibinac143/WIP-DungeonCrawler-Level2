using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GatePassageController : MonoBehaviour
{
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField, Min(0.5f)] private float openDuration = 2.4f;
    [SerializeField, Min(0.5f)] private float slideDistance = 4.5f;
    [SerializeField] private Collider[] doorBlockingColliders;

    private Vector3 leftClosedWorldPosition;
    private Vector3 rightClosedWorldPosition;
    private Quaternion leftClosedWorldRotation;
    private Quaternion rightClosedWorldRotation;
    private bool cached;
    private bool open;
    private Coroutine routine;

    public bool IsOpen => open;

    public void Configure(
        Transform left,
        Transform right,
        float duration,
        float distance)
    {
        leftDoor = left;
        rightDoor = right;
        openDuration = Mathf.Max(0.5f, duration);
        slideDistance = Mathf.Max(0.5f, distance);

        CacheClosedState();
        CacheDoorColliders();
    }

    private void Awake()
    {
        CacheClosedState();
        CacheDoorColliders();
    }

    private void CacheClosedState()
    {
        if (cached)
            return;

        if (leftDoor != null)
        {
            leftClosedWorldPosition = leftDoor.position;
            leftClosedWorldRotation = leftDoor.rotation;
        }

        if (rightDoor != null)
        {
            rightClosedWorldPosition = rightDoor.position;
            rightClosedWorldRotation = rightDoor.rotation;
        }

        cached = true;
    }

    private void CacheDoorColliders()
    {
        var colliders = new System.Collections.Generic.List<Collider>();

        if (leftDoor != null)
            colliders.AddRange(leftDoor.GetComponentsInChildren<Collider>(true));

        if (rightDoor != null)
            colliders.AddRange(rightDoor.GetComponentsInChildren<Collider>(true));

        doorBlockingColliders = colliders.ToArray();
    }

    public void OpenGate()
    {
        CacheClosedState();

        if (open)
        {
            SetOpenInstantly();
            return;
        }

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(OpenRoutine());
    }

    public void SetClosedInstantly()
    {
        CacheClosedState();

        if (leftDoor != null)
        {
            leftDoor.position = leftClosedWorldPosition;
            leftDoor.rotation = leftClosedWorldRotation;
        }

        if (rightDoor != null)
        {
            rightDoor.position = rightClosedWorldPosition;
            rightDoor.rotation = rightClosedWorldRotation;
        }

        SetDoorColliders(true);
        open = false;
    }

    public void SetOpenInstantly()
    {
        CacheClosedState();

        Vector3 outward = CalculateOutwardDirection();

        if (leftDoor != null)
        {
            leftDoor.position =
                leftClosedWorldPosition +
                outward * slideDistance;
        }

        if (rightDoor != null)
        {
            rightDoor.position =
                rightClosedWorldPosition -
                outward * slideDistance;
        }

        SetDoorColliders(false);
        open = true;
    }

    private IEnumerator OpenRoutine()
    {
        Vector3 leftStart =
            leftDoor != null
                ? leftDoor.position
                : Vector3.zero;

        Vector3 rightStart =
            rightDoor != null
                ? rightDoor.position
                : Vector3.zero;

        Vector3 outward = CalculateOutwardDirection();

        Vector3 leftTarget =
            leftClosedWorldPosition +
            outward * slideDistance;

        Vector3 rightTarget =
            rightClosedWorldPosition -
            outward * slideDistance;

        float elapsed = 0f;
        bool collidersDisabled = false;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed /
                    openDuration);

            float eased =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    normalized);

            if (leftDoor != null)
            {
                leftDoor.position =
                    Vector3.Lerp(
                        leftStart,
                        leftTarget,
                        eased);
            }

            if (rightDoor != null)
            {
                rightDoor.position =
                    Vector3.Lerp(
                        rightStart,
                        rightTarget,
                        eased);
            }

            if (!collidersDisabled &&
                normalized >= 0.35f)
            {
                SetDoorColliders(false);
                collidersDisabled = true;
            }

            yield return null;
        }

        SetOpenInstantly();
        routine = null;
    }

    private Vector3 CalculateOutwardDirection()
    {
        if (leftDoor != null &&
            rightDoor != null)
        {
            Vector3 direction =
                leftClosedWorldPosition -
                rightClosedWorldPosition;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                return direction.normalized;
        }

        return -transform.right;
    }

    private void SetDoorColliders(bool enabledState)
    {
        if (doorBlockingColliders == null)
            return;

        foreach (Collider collider in doorBlockingColliders)
        {
            if (collider != null)
                collider.enabled = enabledState;
        }
    }
}
