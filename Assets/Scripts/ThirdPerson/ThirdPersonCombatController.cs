using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class ThirdPersonCombatController : MonoBehaviour
{
    [Header("Assigned by setup tool")]
    [SerializeField] private GameObject characterVisualRoot;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera thirdPersonCamera;
    [SerializeField] private GameObject firstPersonVisualRoot;
    [SerializeField] private Behaviour firstPersonMovement;
    [SerializeField] private Behaviour firstPersonSword;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private CombatHealth combatHealth;
    [SerializeField] private SwordDamageDealer swordDamageDealer;
    [SerializeField] private Transform thirdPersonAttackOrigin;

    [Header("View switching")]
    [SerializeField] private bool startInThirdPerson;
    [SerializeField] private bool showSwitchHint = true;
    [SerializeField, Min(0.5f)] private float combatPoseHoldSeconds = 4f;

    [Header("Third-person movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float sprintSpeed = 7.5f;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 14f;
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.45f;
    [SerializeField] private float gravity = -22f;

    [Header("Third-person camera")]
    [SerializeField, Min(1f)] private float cameraDistance = 4.6f;
    [SerializeField, Min(0f)] private float cameraHeight = 1.45f;
    [SerializeField] private float shoulderOffset = 0.65f;
    [SerializeField, Min(0.01f)] private float lookSensitivity = 0.11f;
    [SerializeField, Min(0.01f)] private float cameraSmoothTime = 0.045f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.22f;
    [SerializeField] private LayerMask cameraCollisionMask = ~0;

    private float verticalVelocity;
    private float yaw;
    private float pitch = 13f;
    private float combatPoseUntil;
    private float lastLightAttackTime = -10f;
    private int lightComboIndex;
    private Vector3 cameraVelocity;
    private bool isThirdPerson;
    private bool combatPoseActive;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int CombatHash =
        Animator.StringToHash("Combat");

    private static readonly int BlockHash =
        Animator.StringToHash("Block");

    private static readonly int Attack1Hash =
        Animator.StringToHash("Attack1");

    private static readonly int Attack2Hash =
        Animator.StringToHash("Attack2");

    private static readonly int HeavyHash =
        Animator.StringToHash("Heavy");

    public bool IsThirdPerson => isThirdPerson;

    // Backward-compatible setup signature.
    public void Configure(
        GameObject visualRoot,
        Animator animator,
        Camera fpCamera,
        Camera tpCamera,
        GameObject fpVisualRoot,
        Behaviour fpMovement,
        Behaviour fpSword,
        CharacterController controller,
        CombatHealth health,
        SwordDamageDealer damageDealer)
    {
        Configure(
            visualRoot,
            animator,
            fpCamera,
            tpCamera,
            fpVisualRoot,
            fpMovement,
            fpSword,
            controller,
            health,
            damageDealer,
            null);
    }

    public void Configure(
        GameObject visualRoot,
        Animator animator,
        Camera fpCamera,
        Camera tpCamera,
        GameObject fpVisualRoot,
        Behaviour fpMovement,
        Behaviour fpSword,
        CharacterController controller,
        CombatHealth health,
        SwordDamageDealer damageDealer,
        Transform attackOrigin)
    {
        characterVisualRoot = visualRoot;
        characterAnimator = animator;
        firstPersonCamera = fpCamera;
        thirdPersonCamera = tpCamera;
        firstPersonVisualRoot = fpVisualRoot;
        firstPersonMovement = fpMovement;
        firstPersonSword = fpSword;
        characterController = controller;
        combatHealth = health;
        swordDamageDealer = damageDealer;
        thirdPersonAttackOrigin = attackOrigin;
    }

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (combatHealth == null)
            combatHealth = GetComponent<CombatHealth>();

        if (swordDamageDealer == null)
            swordDamageDealer = GetComponent<SwordDamageDealer>();

        yaw = transform.eulerAngles.y;
        ApplyViewMode(startInThirdPerson, true);
    }

    private void Update()
    {
        if (ReadToggleView())
            ToggleView();

        if (!isThirdPerson)
            return;

        bool lightPressed = ReadLightAttack();
        bool heavyPressed = ReadHeavyAttack();
        bool blockHeld = ReadBlock();
        bool jumpPressed = ReadJump();

        if (combatHealth != null &&
            combatHealth.IsDead)
        {
            SetAnimatorSpeed(0f);
            SetAnimatorBlock(false);
            return;
        }

        bool grounded =
            characterController != null &&
            characterController.isGrounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (grounded && jumpPressed)
        {
            verticalVelocity =
                Mathf.Sqrt(
                    jumpHeight *
                    -2f *
                    gravity);
        }

        if (lightPressed)
        {
            ActivateCombatPose();
            FaceCameraForward();
            PlayLightAttack();

            if (swordDamageDealer != null)
                swordDamageDealer.TryLightAttack();
        }

        if (heavyPressed)
        {
            ActivateCombatPose();
            FaceCameraForward();

            if (characterAnimator != null)
            {
                characterAnimator.ResetTrigger(Attack1Hash);
                characterAnimator.ResetTrigger(Attack2Hash);
                characterAnimator.SetTrigger(HeavyHash);
            }

            if (swordDamageDealer != null)
                swordDamageDealer.TryHeavyAttack();
        }

        if (blockHeld)
            ActivateCombatPose();

        SetAnimatorBlock(blockHeld);
        MoveThirdPerson();

        if (combatPoseActive &&
            !blockHeld &&
            Time.time >= combatPoseUntil &&
            !IsAttackStatePlaying())
        {
            SetCombatPose(false);
        }
    }

    private void LateUpdate()
    {
        if (isThirdPerson &&
            thirdPersonCamera != null)
        {
            UpdateThirdPersonCamera();
        }
    }

    private void OnGUI()
    {
        if (!showSwitchHint)
            return;

        string mode =
            isThirdPerson ? "TPV" : "FPV";

        GUI.Box(
            new Rect(
                Screen.width - 275f,
                18f,
                255f,
                46f),
            "[T] Switch View   [Space] Jump\nCurrent: " + mode);
    }

    public void ToggleView()
    {
        if (isThirdPerson)
            EnterFirstPerson();
        else
            EnterThirdPerson();
    }

    public void EnterThirdPerson()
    {
        if (isThirdPerson)
            return;

        yaw = transform.eulerAngles.y;
        pitch = 13f;

        ApplyViewMode(true, false);
        SetCombatPose(false);
        SnapCameraBehindPlayer();
    }

    public void EnterFirstPerson()
    {
        if (!isThirdPerson)
            return;

        verticalVelocity = 0f;
        SetCombatPose(false);
        SetAnimatorBlock(false);
        SetAnimatorSpeed(0f);
        ApplyViewMode(false, false);
    }

    private void ApplyViewMode(
        bool thirdPerson,
        bool force)
    {
        isThirdPerson = thirdPerson;

        if (characterVisualRoot != null)
            characterVisualRoot.SetActive(thirdPerson);

        if (firstPersonVisualRoot != null)
            firstPersonVisualRoot.SetActive(!thirdPerson);

        if (firstPersonCamera != null)
            firstPersonCamera.enabled = !thirdPerson;

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = thirdPerson;

        if (firstPersonMovement != null)
            firstPersonMovement.enabled = !thirdPerson;

        if (firstPersonSword != null)
            firstPersonSword.enabled = !thirdPerson;

        if (swordDamageDealer != null)
        {
            if (thirdPerson)
            {
                swordDamageDealer.Configure(
                    thirdPersonCamera,
                    combatHealth);

                swordDamageDealer.ConfigureMeleeOrigin(
                    thirdPersonAttackOrigin,
                    false);
            }
            else
            {
                swordDamageDealer.Configure(
                    firstPersonCamera,
                    combatHealth);

                swordDamageDealer.ClearMeleeOrigin(true);
            }
        }

        if (force && thirdPerson)
            SnapCameraBehindPlayer();
    }

    private void ActivateCombatPose()
    {
        combatPoseUntil =
            Time.time + combatPoseHoldSeconds;

        SetCombatPose(true);
    }

    private void SetCombatPose(bool enabledState)
    {
        combatPoseActive = enabledState;

        if (characterAnimator != null)
            characterAnimator.SetBool(
                CombatHash,
                enabledState);
    }

    private void MoveThirdPerson()
    {
        if (characterController == null ||
            thirdPersonCamera == null)
        {
            SetAnimatorSpeed(0f);
            return;
        }

        Vector2 input = ReadMovement();
        bool sprintHeld = ReadSprint();

        Vector3 cameraForward =
            Vector3.ProjectOnPlane(
                thirdPersonCamera.transform.forward,
                Vector3.up).normalized;

        Vector3 cameraRight =
            Vector3.ProjectOnPlane(
                thirdPersonCamera.transform.right,
                Vector3.up).normalized;

        Vector3 movement =
            cameraForward * input.y +
            cameraRight * input.x;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        float speed =
            sprintHeld
                ? sprintSpeed
                : moveSpeed;

        verticalVelocity +=
            gravity * Time.deltaTime;

        characterController.Move(
            (movement * speed +
             Vector3.up * verticalVelocity) *
            Time.deltaTime);

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation =
                Quaternion.LookRotation(
                    movement,
                    Vector3.up);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    desiredRotation,
                    rotationSpeed * Time.deltaTime);
        }

        float normalizedSpeed =
            movement.magnitude *
            (sprintHeld ? 1f : 0.55f);

        SetAnimatorSpeed(normalizedSpeed);
    }

    private void FaceCameraForward()
    {
        if (thirdPersonCamera == null)
            return;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                thirdPersonCamera.transform.forward,
                Vector3.up).normalized;

        if (forward.sqrMagnitude < 0.001f)
            return;

        transform.rotation =
            Quaternion.LookRotation(
                forward,
                Vector3.up);
    }

    private void UpdateThirdPersonCamera()
    {
        Vector2 look = ReadLook();

        yaw += look.x * lookSensitivity;

        pitch = Mathf.Clamp(
            pitch - look.y * lookSensitivity,
            -18f,
            58f);

        Quaternion orbit =
            Quaternion.Euler(
                pitch,
                yaw,
                0f);

        Vector3 target =
            transform.position +
            Vector3.up * cameraHeight;

        Vector3 shoulder =
            orbit *
            Vector3.right *
            shoulderOffset;

        Vector3 backward =
            orbit *
            Vector3.back;

        Vector3 castOrigin =
            target + shoulder;

        float safeDistance =
            ResolveCameraDistance(
                castOrigin,
                backward,
                cameraDistance);

        Vector3 desired =
            castOrigin +
            backward *
            safeDistance;

        thirdPersonCamera.transform.position =
            Vector3.SmoothDamp(
                thirdPersonCamera.transform.position,
                desired,
                ref cameraVelocity,
                cameraSmoothTime);

        Vector3 lookTarget =
            target +
            transform.forward *
            0.8f;

        Vector3 lookDirection =
            lookTarget -
            thirdPersonCamera.transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            thirdPersonCamera.transform.rotation =
                Quaternion.LookRotation(
                    lookDirection,
                    Vector3.up);
        }
    }

    private float ResolveCameraDistance(
        Vector3 origin,
        Vector3 direction,
        float requestedDistance)
    {
        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                collisionRadius,
                direction,
                requestedDistance,
                cameraCollisionMask,
                QueryTriggerInteraction.Ignore);

        float nearest =
            requestedDistance;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == null)
                continue;

            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            nearest =
                Mathf.Min(
                    nearest,
                    Mathf.Max(
                        0.35f,
                        hit.distance -
                        0.08f));
        }

        return nearest;
    }

    private void SnapCameraBehindPlayer()
    {
        if (thirdPersonCamera == null)
            return;

        Quaternion orbit =
            Quaternion.Euler(
                pitch,
                yaw,
                0f);

        Vector3 target =
            transform.position +
            Vector3.up *
            cameraHeight;

        thirdPersonCamera.transform.position =
            target +
            orbit *
            Vector3.right *
            shoulderOffset +
            orbit *
            Vector3.back *
            cameraDistance;

        thirdPersonCamera.transform.LookAt(
            target +
            transform.forward *
            0.8f);
    }

    private void PlayLightAttack()
    {
        if (characterAnimator == null)
            return;

        bool continueCombo =
            Time.time -
            lastLightAttackTime <
            0.85f;

        if (!continueCombo)
            lightComboIndex = 0;

        int trigger =
            lightComboIndex % 2 == 0
                ? Attack1Hash
                : Attack2Hash;

        characterAnimator.ResetTrigger(Attack1Hash);
        characterAnimator.ResetTrigger(Attack2Hash);
        characterAnimator.SetTrigger(trigger);

        lightComboIndex++;
        lastLightAttackTime = Time.time;
    }

    private bool IsAttackStatePlaying()
    {
        if (characterAnimator == null)
            return false;

        AnimatorStateInfo state =
            characterAnimator.GetCurrentAnimatorStateInfo(0);

        return
            state.IsName("Attack 1") ||
            state.IsName("Attack 2") ||
            state.IsName("Heavy Attack");
    }

    private void SetAnimatorSpeed(float value)
    {
        if (characterAnimator != null)
            characterAnimator.SetFloat(
                SpeedHash,
                value);
    }

    private void SetAnimatorBlock(bool value)
    {
        if (characterAnimator != null)
            characterAnimator.SetBool(
                BlockHash,
                value);
    }

    private static Vector2 ReadMovement()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
            return Vector2.zero;

        Vector2 movement =
            Vector2.zero;

        if (keyboard.wKey.isPressed)
            movement.y += 1f;

        if (keyboard.sKey.isPressed)
            movement.y -= 1f;

        if (keyboard.dKey.isPressed)
            movement.x += 1f;

        if (keyboard.aKey.isPressed)
            movement.x -= 1f;

        return Vector2.ClampMagnitude(
            movement,
            1f);
#else
        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
#endif
    }

    private static Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.delta.ReadValue()
            : Vector2.zero;
#else
        return new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y"));
#endif
    }

    private static bool ReadToggleView()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    private static bool ReadJump()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool ReadLightAttack()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool ReadHeavyAttack()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private static bool ReadBlock()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.rightButton.isPressed;
#else
        return Input.GetMouseButton(1);
#endif
    }

    private static bool ReadSprint()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard =
            Keyboard.current;

        return keyboard != null &&
               (keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.RightShift);
#endif
    }
}
