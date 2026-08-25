using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GalleryFreeCamera : MonoBehaviour
{
    public float moveSpeed = 12f;
    public float fastMultiplier = 3f;
    public float lookSensitivity = 0.12f;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard k = Keyboard.current;
        Mouse m = Mouse.current;
        if (k == null || m == null) return;

        Vector3 input = Vector3.zero;
        if (k.wKey.isPressed) input += Vector3.forward;
        if (k.sKey.isPressed) input += Vector3.back;
        if (k.aKey.isPressed) input += Vector3.left;
        if (k.dKey.isPressed) input += Vector3.right;
        if (k.eKey.isPressed) input += Vector3.up;
        if (k.qKey.isPressed) input += Vector3.down;

        bool fast = k.leftShiftKey.isPressed || k.rightShiftKey.isPressed;
        float speed = moveSpeed * (fast ? fastMultiplier : 1f);

        Vector3 move =
            transform.TransformDirection(new Vector3(input.x, 0f, input.z)) +
            Vector3.up * input.y;

        if (move.sqrMagnitude > 0f)
            transform.position += move.normalized * speed * Time.unscaledDeltaTime;

        if (m.rightButton.isPressed)
        {
            Vector2 delta = m.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float scroll = m.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
            moveSpeed = Mathf.Clamp(moveSpeed + Mathf.Sign(scroll) * 2f, 2f, 80f);
#endif
    }
}
