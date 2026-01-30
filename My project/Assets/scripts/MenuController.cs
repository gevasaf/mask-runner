using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// Creates two big menu buttons: New Game (loads story) and Skip Intro (loads runner).
/// Add this to any GameObject in the menu scene (e.g. an empty "MenuController").
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scene Names (must be in Build Settings)")]
    [Tooltip("Scene to load when pressing New Game")]
    public string storySceneName = "story";
    [Tooltip("Scene to load when pressing Skip Intro")]
    public string runnerSceneName = "runner";

    [Header("Button Style (optional)")]
    [Tooltip("Width of each button")]
    public float buttonWidth = 400f;
    [Tooltip("Height of each button")]
    public float buttonHeight = 80f;
    [Tooltip("Vertical gap between buttons")]
    public float buttonSpacing = 24f;

    void Awake()
    {
        SetupCanvasAndButtons();
    }

    void SetupCanvasAndButtons()
    {
        // Ensure EventSystem exists (required for UI)
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }

        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            canvasRect = canvas.gameObject.AddComponent<RectTransform>();

        // Create panel to hold buttons (top third of screen)
        GameObject panelGo = new GameObject("MenuPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);   // top center
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(buttonWidth + 40, buttonHeight * 2 + buttonSpacing + 40);
        panelRect.anchoredPosition = new Vector2(0f, -50f);  // 50px from top

        // Optional background
        Image panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        // --- New Game button ---
        Button newGameBtn = CreateButton(panelGo.transform, "New Game", new Vector2(0, buttonSpacing / 2 + buttonHeight / 2));
        newGameBtn.onClick.AddListener(OnNewGame);

        // --- Skip Intro button ---
        Button skipBtn = CreateButton(panelGo.transform, "Skip Intro", new Vector2(0, -buttonSpacing / 2 - buttonHeight / 2));
        skipBtn.onClick.AddListener(OnSkipIntro);
    }

    Button CreateButton(Transform parent, string label, Vector2 anchoredPos)
    {
        GameObject btnGo = new GameObject(label + "Button");
        btnGo.transform.SetParent(parent, false);

        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rect.anchoredPosition = anchoredPos;

        Image image = btnGo.AddComponent<Image>();
        image.color = new Color(0.25f, 0.5f, 0.85f);

        Button button = btnGo.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.65f, 1f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.7f);
        button.colors = colors;

        // Label
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);

        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        Text text = textGo.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return button;
    }

    void OnNewGame()
    {
        if (string.IsNullOrEmpty(storySceneName))
        {
            Debug.LogWarning("MenuController: storySceneName is empty.");
            return;
        }
        SceneManager.LoadScene(storySceneName);
    }

    void OnSkipIntro()
    {
        if (string.IsNullOrEmpty(runnerSceneName))
        {
            Debug.LogWarning("MenuController: runnerSceneName is empty.");
            return;
        }
        SceneManager.LoadScene(runnerSceneName);
    }
}
