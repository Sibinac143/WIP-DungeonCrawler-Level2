using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerKnockdownSequence : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Behaviour firstPersonMovement;
    [SerializeField] private Behaviour firstPersonSword;
    [SerializeField] private SwordDamageDealer damageDealer;
    [SerializeField] private CombatHealth playerHealth;

    private Coroutine routine;
    private bool overlayVisible;
    private float overlayAlpha;

    public bool IsPlaying => routine != null;

    public void Configure(
        Camera cameraReference,
        Behaviour movement,
        Behaviour sword,
        SwordDamageDealer dealer,
        CombatHealth health)
    {
        playerCamera = cameraReference;
        firstPersonMovement = movement;
        firstPersonSword = sword;
        damageDealer = dealer;
        playerHealth = health;
    }

    public void BeginAmbushFocus(
        Transform attacker,
        float focusDuration = 0.35f)
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (firstPersonMovement != null)
            firstPersonMovement.enabled = false;

        if (firstPersonSword != null)
            firstPersonSword.enabled = false;

        if (damageDealer != null)
            damageDealer.enabled = false;

        if (attacker != null &&
            playerCamera != null)
        {
            StartCoroutine(
                FocusOnAttackerRoutine(
                    attacker,
                    Mathf.Max(0.1f, focusDuration)));
        }
    }

    private IEnumerator FocusOnAttackerRoutine(
        Transform attacker,
        float duration)
    {
        Quaternion start =
            playerCamera.transform.rotation;

        float elapsed = 0f;

        while (elapsed < duration &&
               attacker != null)
        {
            elapsed += Time.deltaTime;

            Vector3 direction =
                attacker.position +
                Vector3.up * 1.4f -
                playerCamera.transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion target =
                    Quaternion.LookRotation(
                        direction.normalized,
                        Vector3.up);

                playerCamera.transform.rotation =
                    Quaternion.Slerp(
                        start,
                        target,
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.Clamp01(
                                elapsed /
                                duration)));
            }

            yield return null;
        }
    }

    public void Play(
        float duration,
        GameObject damageSource,
        Action completed)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine =
            StartCoroutine(
                KnockdownRoutine(
                    Mathf.Max(1.5f, duration),
                    damageSource,
                    completed));
    }

    private IEnumerator KnockdownRoutine(
        float duration,
        GameObject damageSource,
        Action completed)
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (firstPersonMovement != null)
            firstPersonMovement.enabled = false;

        if (firstPersonSword != null)
            firstPersonSword.enabled = false;

        if (damageDealer != null)
            damageDealer.enabled = false;

        if (playerHealth != null &&
            !playerHealth.IsDead)
        {
            playerHealth.ApplyDamage(
                22f,
                damageSource);
        }

        Vector3 originalPosition =
            playerCamera != null
                ? playerCamera.transform.localPosition
                : Vector3.zero;

        Quaternion originalRotation =
            playerCamera != null
                ? playerCamera.transform.localRotation
                : Quaternion.identity;

        Vector3 downPosition =
            originalPosition +
            new Vector3(
                0.22f,
                -0.85f,
                -0.12f);

        Quaternion downRotation =
            originalRotation *
            Quaternion.Euler(
                58f,
                0f,
                18f);

        overlayVisible = true;

        float fallDuration =
            Mathf.Min(
                0.65f,
                duration * 0.3f);

        float elapsed = 0f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed /
                        fallDuration));

            overlayAlpha =
                Mathf.Lerp(
                    0f,
                    0.58f,
                    t);

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition =
                    Vector3.Lerp(
                        originalPosition,
                        downPosition,
                        t);

                playerCamera.transform.localRotation =
                    Quaternion.Slerp(
                        originalRotation,
                        downRotation,
                        t);
            }

            yield return null;
        }

        float holdDuration =
            Mathf.Max(
                0.35f,
                duration -
                fallDuration -
                0.75f);

        yield return new WaitForSeconds(
            holdDuration);

        elapsed = 0f;
        const float recoverDuration = 0.75f;

        while (elapsed < recoverDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed /
                        recoverDuration));

            overlayAlpha =
                Mathf.Lerp(
                    0.58f,
                    0f,
                    t);

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition =
                    Vector3.Lerp(
                        downPosition,
                        originalPosition,
                        t);

                playerCamera.transform.localRotation =
                    Quaternion.Slerp(
                        downRotation,
                        originalRotation,
                        t);
            }

            yield return null;
        }

        if (playerCamera != null)
        {
            playerCamera.transform.localPosition =
                originalPosition;

            playerCamera.transform.localRotation =
                originalRotation;
        }

        overlayVisible = false;
        overlayAlpha = 0f;

        if (firstPersonMovement != null)
            firstPersonMovement.enabled = true;

        if (firstPersonSword != null)
            firstPersonSword.enabled = true;

        if (damageDealer != null)
            damageDealer.enabled = true;

        routine = null;
        completed?.Invoke();
    }

    private void OnGUI()
    {
        if (!overlayVisible)
            return;

        Color previous = GUI.color;

        GUI.color =
            new Color(
                0.18f,
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

        GUI.Label(
            new Rect(
                Screen.width * 0.5f - 180f,
                Screen.height * 0.5f - 20f,
                360f,
                40f),
            "THE DEMON KNOCKS YOU DOWN");

        GUI.color = previous;
    }
}
