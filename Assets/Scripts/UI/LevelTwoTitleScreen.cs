using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class LevelTwoTitleScreen : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "SampleScene";

    [Header("Presentation")]
    [SerializeField] private string gameTitle = "REALM OF DEATH";
    [SerializeField] private string levelTitle = "LEVEL II";
    [SerializeField] private string tagline =
        "Break the curse. Rescue the blacksmith. Escape the realm.";

    private Texture2D gradientTexture;
    private GUIStyle gameTitleStyle;
    private GUIStyle levelStyle;
    private GUIStyle taglineStyle;
    private GUIStyle buttonStyle;
    private GUIStyle smallStyle;
    private bool stylesReady;

    public void Configure(
        string gameplayScene)
    {
        gameplaySceneName = gameplayScene;
    }

    private void Awake()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        Time.timeScale = 1f;

        BuildGradientTexture();
    }

    private void OnDestroy()
    {
        if (gradientTexture != null)
            Destroy(gradientTexture);
    }

    private void OnGUI()
    {
        PrepareStyles();

        DrawBackground();
        DrawAtmosphere();
        DrawTitle();
        DrawButtons();
        DrawFooter();
    }

    private void DrawBackground()
    {
        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height),
            gradientTexture,
            ScaleMode.StretchToFill);

        Color original = GUI.color;

        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                0.28f);

        GUI.DrawTexture(
            new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height * 0.19f),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(
                0f,
                Screen.height * 0.78f,
                Screen.width,
                Screen.height * 0.22f),
            Texture2D.whiteTexture);

        GUI.color = original;
    }

    private void DrawAtmosphere()
    {
        Color original = GUI.color;

        float time =
            Time.unscaledTime;

        for (int index = 0;
             index < 32;
             index++)
        {
            float seed =
                index * 17.31f;

            float x =
                Mathf.Repeat(
                    seed * 73f,
                    Mathf.Max(
                        1f,
                        Screen.width));

            float baseY =
                Mathf.Repeat(
                    seed * 41f,
                    Mathf.Max(
                        1f,
                        Screen.height));

            float y =
                Mathf.Repeat(
                    baseY -
                    time *
                    (8f + index % 5),
                    Mathf.Max(
                        1f,
                        Screen.height));

            float pulse =
                0.32f +
                0.22f *
                Mathf.Sin(
                    time * 1.7f +
                    seed);

            float size =
                2f +
                index % 3;

            GUI.color =
                new Color(
                    0.75f,
                    0.24f,
                    0.07f,
                    pulse);

            GUI.DrawTexture(
                new Rect(
                    x,
                    y,
                    size,
                    size),
                Texture2D.whiteTexture);
        }

        GUI.color = original;
    }

    private void DrawTitle()
    {
        float centerX =
            Screen.width * 0.5f;

        GUI.Label(
            new Rect(
                centerX - 500f,
                Screen.height * 0.19f,
                1000f,
                78f),
            levelTitle,
            levelStyle);

        GUI.Label(
            new Rect(
                centerX - 650f,
                Screen.height * 0.27f,
                1300f,
                130f),
            gameTitle,
            gameTitleStyle);

        GUI.Label(
            new Rect(
                centerX - 430f,
                Screen.height * 0.41f,
                860f,
                58f),
            tagline,
            taglineStyle);

        Color original = GUI.color;

        GUI.color =
            new Color(
                0.85f,
                0.28f,
                0.08f,
                0.75f);

        GUI.DrawTexture(
            new Rect(
                centerX - 215f,
                Screen.height * 0.475f,
                430f,
                2f),
            Texture2D.whiteTexture);

        GUI.color = original;
    }

    private void DrawButtons()
    {
        float width =
            Mathf.Clamp(
                Screen.width * 0.23f,
                280f,
                430f);

        float height =
            Mathf.Clamp(
                Screen.height * 0.065f,
                54f,
                76f);

        float x =
            Screen.width * 0.5f -
            width * 0.5f;

        float y =
            Screen.height * 0.56f;

        if (GUI.Button(
                new Rect(
                    x,
                    y,
                    width,
                    height),
                "BEGIN LEVEL II",
                buttonStyle))
        {
            BeginNewGame();
        }

        bool hasProgress =
            PlayerPrefs.HasKey(
                "DungeonGame.Phase3.Stage") &&
            PlayerPrefs.GetInt(
                "DungeonGame.Phase3.Complete",
                0) == 0;

        if (hasProgress)
        {
            if (GUI.Button(
                    new Rect(
                        x,
                        y + height + 18f,
                        width,
                        height),
                    "CONTINUE",
                    buttonStyle))
            {
                LoadGameplay();
            }

            y += height + 18f;
        }

        if (GUI.Button(
                new Rect(
                    x,
                    y + height + 18f,
                    width,
                    height),
                "QUIT",
                buttonStyle))
        {
            QuitGame();
        }
    }

    private void DrawFooter()
    {
        GUI.Label(
            new Rect(
                24f,
                Screen.height - 54f,
                Screen.width - 48f,
                34f),
            "DUNGEON CRAWLER  •  LEVEL 2",
            smallStyle);
    }

    private void BeginNewGame()
    {
        QuestManager.ResetSavedProgress();
        LoadGameplay();
    }

    private void LoadGameplay()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        SceneManager.LoadScene(
            gameplaySceneName);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PrepareStyles()
    {
        if (stylesReady)
            return;

        int titleSize =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    Screen.height * 0.085f),
                54,
                104);

        int levelSize =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    Screen.height * 0.032f),
                22,
                42);

        int normalSize =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    Screen.height * 0.022f),
                17,
                28);

        gameTitleStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize = titleSize,
                fontStyle =
                    FontStyle.Bold,
                normal =
                {
                    textColor =
                        new Color(
                            0.91f,
                            0.9f,
                            0.84f)
                }
            };

        levelStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize = levelSize,
                fontStyle =
                    FontStyle.Bold,
                normal =
                {
                    textColor =
                        new Color(
                            0.84f,
                            0.3f,
                            0.09f)
                }
            };

        taglineStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize = normalSize,
                fontStyle =
                    FontStyle.Italic,
                normal =
                {
                    textColor =
                        new Color(
                            0.7f,
                            0.73f,
                            0.77f)
                }
            };

        buttonStyle =
            new GUIStyle(
                GUI.skin.button)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize =
                    Mathf.Clamp(
                        normalSize + 2,
                        20,
                        31),
                fontStyle =
                    FontStyle.Bold,
                normal =
                {
                    textColor =
                        new Color(
                            0.92f,
                            0.9f,
                            0.84f)
                },
                hover =
                {
                    textColor =
                        new Color(
                            1f,
                            0.5f,
                            0.15f)
                },
                active =
                {
                    textColor = Color.white
                }
            };

        smallStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                alignment =
                    TextAnchor.MiddleCenter,
                fontSize =
                    Mathf.Clamp(
                        normalSize - 4,
                        12,
                        19),
                normal =
                {
                    textColor =
                        new Color(
                            0.45f,
                            0.48f,
                            0.52f)
                }
            };

        stylesReady = true;
    }

    private void BuildGradientTexture()
    {
        gradientTexture =
            new Texture2D(
                1,
                256,
                TextureFormat.RGBA32,
                false);

        gradientTexture.name =
            "LevelTwoTitleGradient";

        gradientTexture.wrapMode =
            TextureWrapMode.Clamp;

        gradientTexture.filterMode =
            FilterMode.Bilinear;

        Color top =
            new Color(
                0.012f,
                0.018f,
                0.035f,
                1f);

        Color middle =
            new Color(
                0.035f,
                0.045f,
                0.06f,
                1f);

        Color bottom =
            new Color(
                0.006f,
                0.006f,
                0.009f,
                1f);

        for (int y = 0;
             y < 256;
             y++)
        {
            float t =
                y / 255f;

            Color color =
                t < 0.52f
                    ? Color.Lerp(
                        bottom,
                        middle,
                        t / 0.52f)
                    : Color.Lerp(
                        middle,
                        top,
                        (t - 0.52f) /
                        0.48f);

            gradientTexture.SetPixel(
                0,
                y,
                color);
        }

        gradientTexture.Apply(
            false,
            true);
    }
}
