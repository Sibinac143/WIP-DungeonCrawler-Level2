using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class QuestHUD : MonoBehaviour
{
    [SerializeField] private string objectiveText;
    [SerializeField] private string interactionPrompt;

    private string toastText;
    private float toastUntil;

    private bool letterOpen;
    private string letterText;
    private float letterCanCloseAt;

    private bool completionOpen;
    private string completionText;

    public bool IsModalOpen =>
        letterOpen ||
        completionOpen;

    public void SetObjective(string text)
    {
        objectiveText = text;
    }

    public void SetInteractionPrompt(string text)
    {
        interactionPrompt = text;
    }

    public void ShowToast(
        string text,
        float duration)
    {
        toastText = text;
        toastUntil =
            Time.time +
            Mathf.Max(0.5f, duration);
    }

    public void ShowLetter(string text)
    {
        letterText = text;
        letterOpen = true;
        letterCanCloseAt = Time.time + 0.35f;
    }

    public void ShowCompletion(string text)
    {
        completionText = text;
        completionOpen = true;
        interactionPrompt = string.Empty;
    }

    private void Update()
    {
        if (letterOpen &&
            Time.time >= letterCanCloseAt &&
            ReadInteractPressed())
        {
            letterOpen = false;
        }
    }

    private void OnGUI()
    {
        DrawObjective();
        DrawPrompt();
        DrawToast();

        if (letterOpen)
            DrawLetter();

        if (completionOpen)
            DrawCompletion();
    }

    private void DrawObjective()
    {
        if (string.IsNullOrWhiteSpace(objectiveText))
            return;

        GUI.Box(
            new Rect(
                Screen.width * 0.5f - 310f,
                18f,
                620f,
                52f),
            objectiveText);
    }

    private void DrawPrompt()
    {
        if (letterOpen ||
            completionOpen ||
            string.IsNullOrWhiteSpace(interactionPrompt))
        {
            return;
        }

        GUI.Box(
            new Rect(
                Screen.width * 0.5f - 210f,
                Screen.height - 120f,
                420f,
                44f),
            interactionPrompt);
    }

    private void DrawToast()
    {
        if (Time.time >= toastUntil ||
            string.IsNullOrWhiteSpace(toastText))
        {
            return;
        }

        GUI.Box(
            new Rect(
                Screen.width * 0.5f - 330f,
                Screen.height * 0.68f,
                660f,
                72f),
            toastText);
    }

    private void DrawLetter()
    {
        Rect panel =
            new Rect(
                Screen.width * 0.5f - 300f,
                Screen.height * 0.5f - 175f,
                600f,
                350f);

        GUI.Box(
            panel,
            "WARNING LETTER");

        GUI.Label(
            new Rect(
                panel.x + 35f,
                panel.y + 70f,
                panel.width - 70f,
                panel.height - 135f),
            letterText);

        GUI.Label(
            new Rect(
                panel.x + 35f,
                panel.yMax - 55f,
                panel.width - 70f,
                30f),
            "Press F to close");
    }

    private void DrawCompletion()
    {
        GUI.Box(
            new Rect(
                Screen.width * 0.5f - 330f,
                Screen.height * 0.5f - 90f,
                660f,
                180f),
            completionText +
            "\n\nThe story route is complete.");
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
