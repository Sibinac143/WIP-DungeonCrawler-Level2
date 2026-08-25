using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class GateThrowbackSequence : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Behaviour firstPersonMovement;
    [SerializeField] private Behaviour firstPersonSword;
    [SerializeField] private SwordDamageDealer damageDealer;
    [SerializeField] private CombatHealth playerHealth;

    [Header("Throw")]
    [SerializeField, Min(2f)] private float throwDistance = 17f;
    [SerializeField, Min(0.5f)] private float arcHeight = 3.6f;
    [SerializeField, Min(0.2f)] private float flightDuration = 1.15f;
    [SerializeField, Min(0f)] private float impactDamage = 24f;

    private Coroutine routine;
    private bool overlay;
    private float overlayAlpha;

    public bool IsPlaying => routine != null;

    public void Configure(
        Transform root,
        Camera cameraReference,
        CharacterController controller,
        Behaviour movement,
        Behaviour sword,
        SwordDamageDealer dealer,
        CombatHealth health)
    {
        playerRoot = root;
        playerCamera = cameraReference;
        characterController = controller;
        firstPersonMovement = movement;
        firstPersonSword = sword;
        damageDealer = dealer;
        playerHealth = health;
    }

    public void Play(
        Transform attacker,
        Action completed)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine =
            StartCoroutine(
                ThrowRoutine(
                    attacker,
                    completed));
    }

    private IEnumerator ThrowRoutine(
        Transform attacker,
        Action completed)
    {
        if (playerRoot == null)
            playerRoot = transform;

        if (playerCamera == null)
            playerCamera = Camera.main;

        SetControlsEnabled(false);

        if (characterController != null)
            characterController.enabled = false;

        if (playerHealth != null &&
            !playerHealth.IsDead)
        {
            playerHealth.ApplyDamage(
                impactDamage,
                attacker != null
                    ? attacker.gameObject
                    : gameObject);
        }

        Vector3 start =
            playerRoot.position;

        Vector3 away =
            attacker != null
                ? start - attacker.position
                : -playerRoot.forward;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -playerRoot.forward;

        away.Normalize();

        float allowedDistance =
            CalculateClearDistance(
                start + Vector3.up,
                away,
                throwDistance);

        Vector3 desiredEnd =
            start +
            away *
            allowedDistance;

        Vector3 end =
            FindGroundedDestination(
                desiredEnd);

        Vector3 originalCameraLocalPosition =
            playerCamera != null
                ? playerCamera.transform.localPosition
                : Vector3.zero;

        Quaternion originalCameraLocalRotation =
            playerCamera != null
                ? playerCamera.transform.localRotation
                : Quaternion.identity;

        overlay = true;
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    flightDuration);

            Vector3 horizontal =
                Vector3.Lerp(
                    start,
                    end,
                    t);

            float height =
                Mathf.Sin(
                    t * Mathf.PI) *
                arcHeight;

            playerRoot.position =
                horizontal +
                Vector3.up * height;

            if (playerCamera != null)
            {
                float roll =
                    Mathf.Sin(
                        t * Mathf.PI) *
                    34f;

                float pitch =
                    Mathf.Lerp(
                        0f,
                        48f,
                        t);

                playerCamera.transform.localRotation =
                    originalCameraLocalRotation *
                    Quaternion.Euler(
                        pitch,
                        0f,
                        roll);
            }

            overlayAlpha =
                Mathf.Sin(
                    t * Mathf.PI) *
                0.42f;

            yield return null;
        }

        playerRoot.position = end;

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition =
                originalCameraLocalPosition;

            playerCamera.transform.localRotation =
                originalCameraLocalRotation;
        }

        overlayAlpha = 0.55f;

        yield return new WaitForSeconds(0.75f);

        float recovery = 0f;

        while (recovery < 0.65f)
        {
            recovery += Time.deltaTime;

            overlayAlpha =
                Mathf.Lerp(
                    0.55f,
                    0f,
                    recovery / 0.65f);

            yield return null;
        }

        overlay = false;
        overlayAlpha = 0f;

        if (characterController != null)
            characterController.enabled = true;

        SetControlsEnabled(true);

        routine = null;
        completed?.Invoke();
    }

    private void SetControlsEnabled(
        bool enabledState)
    {
        if (firstPersonMovement != null)
            firstPersonMovement.enabled = enabledState;

        if (firstPersonSword != null)
            firstPersonSword.enabled = enabledState;

        if (damageDealer != null)
            damageDealer.enabled = enabledState;
    }

    private static float CalculateClearDistance(
        Vector3 origin,
        Vector3 direction,
        float requestedDistance)
    {
        if (Physics.SphereCast(
                origin,
                0.55f,
                direction,
                out RaycastHit hit,
                requestedDistance,
                ~0,
                QueryTriggerInteraction.Ignore))
        {
            return
                Mathf.Max(
                    3f,
                    hit.distance - 1.5f);
        }

        return requestedDistance;
    }

    private static Vector3 FindGroundedDestination(
        Vector3 desired)
    {
        if (NavMesh.SamplePosition(
                desired,
                out NavMeshHit navHit,
                12f,
                NavMesh.AllAreas))
        {
            return navHit.position;
        }

        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain != null &&
            terrain.terrainData != null)
        {
            desired.y =
                terrain.SampleHeight(desired) +
                terrain.transform.position.y;
        }

        return desired;
    }

    private void OnGUI()
    {
        if (!overlay)
            return;

        Color original = GUI.color;

        GUI.color =
            new Color(
                0.12f,
                0f,
                0f,
                overlayAlpha);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color = Color.white;

        GUI.Box(
            new Rect(
                Screen.width * 0.5f - 190f,
                Screen.height * 0.47f,
                380f,
                44f),
            "THE GATE GUARDIAN HURLS YOU BACK");

        GUI.color = original;
    }
}
