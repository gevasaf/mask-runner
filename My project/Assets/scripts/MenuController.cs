using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu scene controller: wires up New Game and Skip Intro buttons.
/// Use the inspector button "Create Menu UI" to build the Canvas, title, and buttons in the editor so you can edit them.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Scene Names (must be in Build Settings)")]
    [Tooltip("Scene to load when pressing New Game")]
    public string storySceneName = "story";
    [Tooltip("Scene to load when pressing Skip Intro")]
    public string runnerSceneName = "runner";

    [Header("Create Menu UI (editor only)")]
    [Tooltip("Title text and button size used when you click Create Menu UI in the inspector")]
    public string gameTitleText = "MASK RUNNER";
    public float buttonWidth = 400f;
    public float buttonHeight = 80f;
    public float buttonSpacing = 24f;

    [Header("Optional references (assign or use Create Menu UI in inspector)")]
    [Tooltip("New Game button - wired at runtime if assigned or found by name")]
    public Button newGameButton;
    [Tooltip("Skip Intro button - wired at runtime if assigned or found by name")]
    public Button skipIntroButton;
    [Tooltip("Title text above buttons (optional)")]
    public Text titleText;

    void Start()
    {
        EnsureEventSystem();
        ResolveAndWireButtons();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    void ResolveAndWireButtons()
    {
        // Resolve New Game button
        if (newGameButton == null)
            newGameButton = FindButtonByName("New GameButton");
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGame);
        }

        // Resolve Skip Intro button
        if (skipIntroButton == null)
            skipIntroButton = FindButtonByName("Skip IntroButton");
        if (skipIntroButton != null)
        {
            skipIntroButton.onClick.RemoveAllListeners();
            skipIntroButton.onClick.AddListener(OnSkipIntro);
        }
    }

    static Button FindButtonByName(string name)
    {
        var all = FindObjectsOfType<Button>(true);
        foreach (var b in all)
        {
            if (b.gameObject.name == name)
                return b;
        }
        return null;
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
