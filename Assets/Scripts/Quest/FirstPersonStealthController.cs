using System;
using System.Reflection;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class FirstPersonStealthController : MonoBehaviour
{
    public static FirstPersonStealthController Instance { get; private set; }

    [SerializeField] private MonoBehaviour firstPersonMovement;
    [SerializeField, Min(0.25f)] private float sneakSpeed = 1.75f;
    [SerializeField] private bool showSneakIndicator = true;

    private FieldInfo moveSpeedField;
    private FieldInfo sprintSpeedField;
    private float originalMoveSpeed;
    private float originalSprintSpeed;
    private bool cached;
    private bool isSneaking;

    public bool IsSneaking => isSneaking;

    public void Configure(MonoBehaviour movement)
    {
        firstPersonMovement = movement;
        CacheFields();
    }

    private void Awake()
    {
        Instance = this;
        CacheFields();
    }

    private void OnDisable()
    {
        RestoreSpeeds();

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        bool shouldSneak = ReadSneakHeld();

        if (shouldSneak == isSneaking)
            return;

        isSneaking = shouldSneak;
        ApplySpeedMode();
    }

    private void OnGUI()
    {
        if (!showSneakIndicator ||
            !isSneaking)
        {
            return;
        }

        GUI.Box(
            new Rect(
                Screen.width - 170f,
                Screen.height - 84f,
                150f,
                42f),
            "SNEAKING");
    }

    private void CacheFields()
    {
        if (cached ||
            firstPersonMovement == null)
        {
            return;
        }

        Type type = firstPersonMovement.GetType();

        moveSpeedField =
            type.GetField(
                "moveSpeed",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

        sprintSpeedField =
            type.GetField(
                "sprintSpeed",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

        if (moveSpeedField != null &&
            moveSpeedField.FieldType == typeof(float))
        {
            originalMoveSpeed =
                (float)moveSpeedField.GetValue(
                    firstPersonMovement);
        }

        if (sprintSpeedField != null &&
            sprintSpeedField.FieldType == typeof(float))
        {
            originalSprintSpeed =
                (float)sprintSpeedField.GetValue(
                    firstPersonMovement);
        }

        cached = true;
    }

    private void ApplySpeedMode()
    {
        CacheFields();

        if (firstPersonMovement == null)
            return;

        if (moveSpeedField != null)
        {
            moveSpeedField.SetValue(
                firstPersonMovement,
                isSneaking
                    ? sneakSpeed
                    : originalMoveSpeed);
        }

        if (sprintSpeedField != null)
        {
            sprintSpeedField.SetValue(
                firstPersonMovement,
                isSneaking
                    ? sneakSpeed
                    : originalSprintSpeed);
        }
    }

    private void RestoreSpeeds()
    {
        if (!cached ||
            firstPersonMovement == null)
        {
            return;
        }

        if (moveSpeedField != null)
        {
            moveSpeedField.SetValue(
                firstPersonMovement,
                originalMoveSpeed);
        }

        if (sprintSpeedField != null)
        {
            sprintSpeedField.SetValue(
                firstPersonMovement,
                originalSprintSpeed);
        }

        isSneaking = false;
    }

    private static bool ReadSneakHeld()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
               (keyboard.leftCtrlKey.isPressed ||
                keyboard.rightCtrlKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftControl) ||
               Input.GetKey(KeyCode.RightControl);
#endif
    }
}
