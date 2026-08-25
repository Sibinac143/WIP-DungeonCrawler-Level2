using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class ReturnToTitleOnCompletion : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private string titleSceneName = "TitleScreen";
    [SerializeField, Min(1f)] private float completionDisplaySeconds = 6f;
    [SerializeField, Min(0.1f)] private float fadeSeconds = 1.25f;

    private bool returning;
    private float fadeAlpha;

    public void Configure(
        QuestManager manager,
        string titleScene,
        float delay)
    {
        questManager = manager;
        titleSceneName = titleScene;
        completionDisplaySeconds =
            Mathf.Max(1f, delay);
    }

    private void Start()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager != null)
        {
            questManager.StageChanged -=
                HandleStageChanged;

            questManager.StageChanged +=
                HandleStageChanged;

            if (questManager.GameComplete ||
                questManager.CurrentStage ==
                    QuestStage.Complete)
            {
                BeginReturn();
            }
        }
    }

    private void OnDestroy()
    {
        if (questManager != null)
        {
            questManager.StageChanged -=
                HandleStageChanged;
        }
    }

    private void Update()
    {
        if (questManager == null)
        {
            questManager =
                QuestManager.Instance;

            if (questManager != null)
            {
                questManager.StageChanged +=
                    HandleStageChanged;
            }
        }
    }

    private void HandleStageChanged(
        QuestStage stage)
    {
        if (stage ==
            QuestStage.Complete)
        {
            BeginReturn();
        }
    }

    private void BeginReturn()
    {
        if (returning)
            return;

        returning = true;

        StartCoroutine(
            ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        float visibleTime =
            Mathf.Max(
                0f,
                completionDisplaySeconds -
                fadeSeconds);

        yield return new WaitForSecondsRealtime(
            visibleTime);

        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            fadeAlpha =
                Mathf.Clamp01(
                    elapsed /
                    fadeSeconds);

            yield return null;
        }

        QuestManager.ResetSavedProgress();

        Time.timeScale = 1f;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        SceneManager.LoadScene(
            titleSceneName);
    }

    private void OnGUI()
    {
        if (!returning ||
            fadeAlpha <= 0f)
        {
            return;
        }

        Color original =
            GUI.color;

        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                fadeAlpha);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            Texture2D.whiteTexture);

        GUI.color =
            new Color(
                1f,
                1f,
                1f,
                fadeAlpha);

        GUIStyle style =
            new GUIStyle(
                GUI.skin.label)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize =
                    Mathf.Clamp(
                        Mathf.RoundToInt(
                            Screen.height *
                            0.028f),
                        20,
                        38),
                fontStyle =
                    FontStyle.Bold
            };

        GUI.Label(
            new Rect(
                0f,
                Screen.height * 0.73f,
                Screen.width,
                60f),
            "RETURNING TO TITLE",
            style);

        GUI.color = original;
    }
}
