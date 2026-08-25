using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class PlayerDefense : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float blockedDamageMultiplier = 0.35f;

    public bool IsBlocking { get; private set; }

    public float BlockedDamageMultiplier =>
        Mathf.Clamp01(blockedDamageMultiplier);

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        IsBlocking =
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed;
#else
        IsBlocking = Input.GetMouseButton(1);
#endif
    }

    private void OnDisable()
    {
        IsBlocking = false;
    }
}
