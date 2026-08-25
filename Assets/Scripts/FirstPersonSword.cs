using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FirstPersonSword : MonoBehaviour
{
    [Header("Basic Attack")]
    [SerializeField] private float attackDuration = 0.32f;
    [SerializeField] private Vector3 attackPositionOffset =
        new Vector3(-0.16f, 0.08f, 0.05f);
    [SerializeField] private Vector3 attackRotationOffset =
        new Vector3(18f, -65f, 30f);

    [Header("Heavy Attack")]
    [SerializeField] private float heavyAttackDuration = 0.55f;
    [SerializeField] private Vector3 heavyPositionOffset =
        new Vector3(-0.08f, 0.16f, 0.12f);
    [SerializeField] private Vector3 heavyRotationOffset =
        new Vector3(-35f, -90f, 15f);

    [Header("Block")]
    [SerializeField] private Vector3 blockPositionOffset =
        new Vector3(-0.10f, 0.08f, -0.08f);
    [SerializeField] private Vector3 blockRotationOffset =
        new Vector3(-20f, 15f, 65f);
    [SerializeField] private float blockTransitionSpeed = 10f;

    private Vector3 idleLocalPosition;
    private Quaternion idleLocalRotation;
    private Coroutine attackRoutine;
    private bool isAttacking;

    private void Awake()
    {
        idleLocalPosition = transform.localPosition;
        idleLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!isAttacking && BasicAttackPressed())
        {
            attackRoutine = StartCoroutine(
                PlayAttack(
                    attackDuration,
                    attackPositionOffset,
                    attackRotationOffset
                )
            );
        }
        else if (!isAttacking && HeavyAttackPressed())
        {
            attackRoutine = StartCoroutine(
                PlayAttack(
                    heavyAttackDuration,
                    heavyPositionOffset,
                    heavyRotationOffset
                )
            );
        }

        if (!isAttacking)
            UpdateBlockPose();
    }

    private IEnumerator PlayAttack(
        float duration,
        Vector3 positionOffset,
        Vector3 rotationOffset
    )
    {
        isAttacking = true;

        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;

        Vector3 attackPosition =
            idleLocalPosition + positionOffset;

        Quaternion attackRotation =
            idleLocalRotation *
            Quaternion.Euler(rotationOffset);

        float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f,
                1f,
                elapsed / halfDuration
            );

            transform.localPosition =
                Vector3.Lerp(startPosition, attackPosition, t);

            transform.localRotation =
                Quaternion.Slerp(startRotation, attackRotation, t);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f,
                1f,
                elapsed / halfDuration
            );

            transform.localPosition =
                Vector3.Lerp(attackPosition, idleLocalPosition, t);

            transform.localRotation =
                Quaternion.Slerp(attackRotation, idleLocalRotation, t);

            yield return null;
        }

        transform.localPosition = idleLocalPosition;
        transform.localRotation = idleLocalRotation;

        isAttacking = false;
        attackRoutine = null;
    }

    private void UpdateBlockPose()
    {
        bool blocking = BlockHeld();

        Vector3 targetPosition = blocking
            ? idleLocalPosition + blockPositionOffset
            : idleLocalPosition;

        Quaternion targetRotation = blocking
            ? idleLocalRotation *
              Quaternion.Euler(blockRotationOffset)
            : idleLocalRotation;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            blockTransitionSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            blockTransitionSpeed * Time.deltaTime
        );
    }

    private bool BasicAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool BlockHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private bool HeavyAttackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
