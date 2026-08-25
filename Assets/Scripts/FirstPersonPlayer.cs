using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maximumLookAngle = 85f;

    [Header("Player Setup")]
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private bool placeOnTerrainAtStart = true;

    [Header("Automatically Created")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform swordHolder;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    private void Reset()
    {
        BuildPlayerRig();
    }

    private void Awake()
    {
        BuildPlayerRig();

        if (placeOnTerrainAtStart)
            PlacePlayerOnTerrain();

        LockCursor();
    }

    private void Update()
    {
        HandleCursor();

        if (Cursor.lockState == CursorLockMode.Locked)
            HandleMouseLook();

        HandleMovement();
    }

    [ContextMenu("Build/Repair Player Rig")]
    private void BuildPlayerRig()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        // Character Controller settings
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;

        CreateOrFindCamera();
        CreateSwordHolder();
    }

    private void CreateOrFindCamera()
    {
        if (cameraTransform == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>(true);

            if (childCamera != null)
            {
                cameraTransform = childCamera.transform;
            }
            else if (Camera.main != null)
            {
                // Reuse the Main Camera already in the scene.
                cameraTransform = Camera.main.transform;
            }
            else
            {
                GameObject cameraObject = new GameObject("Main Camera");

                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();

                cameraTransform = cameraObject.transform;
            }
        }

        cameraTransform.SetParent(transform);
        cameraTransform.localPosition = new Vector3(0f, eyeHeight, 0f);
        cameraTransform.localRotation = Quaternion.identity;

        if (cameraTransform.GetComponent<Camera>() != null)
            cameraTransform.gameObject.tag = "MainCamera";
    }

    private void CreateSwordHolder()
    {
        if (cameraTransform == null)
            return;

        if (swordHolder == null)
        {
            Transform existingHolder =
                cameraTransform.Find("SwordHolder");

            if (existingHolder != null)
            {
                swordHolder = existingHolder;
            }
            else
            {
                GameObject holderObject =
                    new GameObject("SwordHolder");

                swordHolder = holderObject.transform;
                swordHolder.SetParent(cameraTransform);

                swordHolder.localPosition =
                    new Vector3(0.45f, -0.35f, 0.7f);

                swordHolder.localRotation =
                    Quaternion.identity;
            }
        }
    }

    private void HandleMovement()
    {
        Vector2 input = ReadMovementInput();

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        float currentSpeed =
            IsSprintPressed() ? sprintSpeed : walkSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && IsJumpPressed())
        {
            verticalVelocity =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMovement =
            movement * currentSpeed;

        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        Vector2 mouseInput = ReadMouseInput();

        float mouseX = mouseInput.x * mouseSensitivity;
        float mouseY = mouseInput.y * mouseSensitivity;

        // Rotate the complete player left and right.
        transform.Rotate(Vector3.up * mouseX);

        // Rotate only the camera up and down.
        cameraPitch -= mouseY;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maximumLookAngle,
            maximumLookAngle
        );

        cameraTransform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void PlacePlayerOnTerrain()
    {
        Terrain terrain = Terrain.activeTerrain;

        if (terrain == null || terrain.terrainData == null)
            return;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 playerPosition = transform.position;

        bool playerIsInsideTerrain =
            playerPosition.x >= terrainPosition.x &&
            playerPosition.x <= terrainPosition.x + terrainSize.x &&
            playerPosition.z >= terrainPosition.z &&
            playerPosition.z <= terrainPosition.z + terrainSize.z;

        if (!playerIsInsideTerrain)
        {
            Debug.LogWarning(
                "Player X and Z position are outside the terrain."
            );

            return;
        }

        float terrainHeight =
            terrain.SampleHeight(playerPosition) +
            terrainPosition.y;

        playerPosition.y = terrainHeight + 0.1f;
        transform.position = playerPosition;
    }

    private void HandleCursor()
    {
        if (IsEscapePressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (IsLeftMousePressed() &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Vector2 ReadMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return Vector2.zero;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.sKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.wKey.isPressed)
            vertical += 1f;

        return new Vector2(horizontal, vertical);
#else
        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
#endif
    }

    private Vector2 ReadMouseInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
            return Vector2.zero;

        // Reduce raw pixel movement to a useful sensitivity.
        return Mouse.current.delta.ReadValue() * 0.1f;
#else
        return new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
#endif
    }

    private bool IsSprintPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.leftShiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    private bool IsJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private bool IsLeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}