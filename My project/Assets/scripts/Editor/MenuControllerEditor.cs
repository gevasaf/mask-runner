using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

[CustomEditor(typeof(MenuController))]
public class MenuControllerEditor : Editor
{
    const float TitleHeight = 72f;
    const float PanelPadding = 40f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Create Menu UI", GUILayout.Height(28)))
        {
            CreateMenuUI((MenuController)target);
        }
    }

    static void CreateMenuUI(MenuController menu)
    {
        Undo.SetCurrentGroupName("Create Menu UI");
        int undoGroup = Undo.GetCurrentGroup();

        // EventSystem
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create Menu UI");
        }

        // Canvas (responsive)
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Menu UI");
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
            canvasRect = canvas.gameObject.AddComponent<RectTransform>();

        float bw = menu.buttonWidth;
        float bh = menu.buttonHeight;
        float spacing = menu.buttonSpacing;
        float panelH = TitleHeight + spacing + bh + spacing + bh + PanelPadding;
        float panelW = Mathf.Max(bw + PanelPadding, 320f);

        // MenuPanel
        GameObject panelGo = new GameObject("MenuPanel");
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelRect.anchoredPosition = new Vector2(0f, -50f);

        Image panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        Undo.RegisterCreatedObjectUndo(panelGo, "Create Menu UI");

        // Title
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(panelGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(panelW - 20, TitleHeight);
        titleRect.anchoredPosition = new Vector2(0f, -TitleHeight / 2f - 10f);

        Text titleText = titleGo.AddComponent<Text>();
        titleText.text = menu.gameTitleText;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // New Game button
        Button newGameBtn = CreateButton(panelGo.transform, "New Game", new Vector2(0, spacing / 2f + bh / 2f + 10f), bw, bh);

        // Skip Intro button
        Button skipBtn = CreateButton(panelGo.transform, "Skip Intro", new Vector2(0, -spacing / 2f - bh / 2f - 10f), bw, bh);

        Undo.RegisterCreatedObjectUndo(panelGo, "Create Menu UI");

        // Assign references on MenuController
        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
        so.FindProperty("skipIntroButton").objectReferenceValue = skipBtn;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.RecordObject(menu, "Create Menu UI");
        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(menu);
    }

    static Button CreateButton(Transform parent, string label, Vector2 anchoredPos, float width, float height)
    {
        GameObject btnGo = new GameObject(label + "Button");
        btnGo.transform.SetParent(parent, false);

        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = anchoredPos;

        Image image = btnGo.AddComponent<Image>();
        image.color = new Color(0.25f, 0.5f, 0.85f);

        Button button = btnGo.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.65f, 1f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.7f);
        button.colors = colors;

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
}
