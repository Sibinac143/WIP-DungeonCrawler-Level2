using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class FirstPersonQuestInteractor : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [SerializeField] private QuestHUD hud;
    [SerializeField, Min(1f)] private float maximumRange = 6f;
    [SerializeField, Range(-1f, 1f)] private float minimumViewDot = 0.15f;

    private QuestInteractable focused;

    public void Configure(
        Camera cameraReference,
        QuestHUD questHud)
    {
        viewCamera = cameraReference;
        hud = questHud;
    }

    private void Awake()
    {
        if (viewCamera == null)
            viewCamera = Camera.main;

        if (hud == null)
            hud = FindFirstObjectByType<QuestHUD>();
    }

    private void Update()
    {
        if (hud != null &&
            hud.IsModalOpen)
        {
            focused = null;
            hud.SetInteractionPrompt(string.Empty);
            return;
        }

        if (viewCamera == null ||
            !viewCamera.enabled)
        {
            viewCamera = Camera.main;
        }

        focused = FindBestInteractable();

        hud?.SetInteractionPrompt(
            focused != null
                ? focused.GetPrompt()
                : string.Empty);

        if (focused != null &&
            ReadInteractPressed())
        {
            focused.Interact();
        }
    }

    private QuestInteractable FindBestInteractable()
    {
        if (viewCamera == null)
            return null;

        QuestInteractable best = null;
        float bestScore = float.PositiveInfinity;

        Vector3 cameraPosition =
            viewCamera.transform.position;

        Vector3 cameraForward =
            viewCamera.transform.forward;

        foreach (QuestInteractable candidate in
                 QuestInteractable.Active)
        {
            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                !candidate.IsAvailable())
            {
                continue;
            }

            Vector3 offset =
                candidate.WorldPosition -
                cameraPosition;

            float distance =
                offset.magnitude;

            float allowedRange =
                Mathf.Min(
                    maximumRange,
                    candidate.InteractionRadius);

            if (distance > allowedRange ||
                distance <= 0.001f)
            {
                continue;
            }

            float viewDot =
                Vector3.Dot(
                    cameraForward,
                    offset / distance);

            if (viewDot < minimumViewDot)
                continue;

            float score =
                distance +
                (1f - viewDot) * 2.5f;

            if (score >= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        return best;
    }

    private static bool ReadInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
               Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
