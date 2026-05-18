#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds main HUD wireframe from producer layout (64px grid). Placeholders only — no button logic.
/// </summary>
public static class MainHUDUISetup
{
    private const float Cell = 64f;
    private const float RefW = 1920f;
    private const float RefH = 1080f;

    public static readonly Color PanelFill = new Color(0.08f, 0.09f, 0.12f, 0.72f);
    public static readonly Color PanelBorder = new Color(0.85f, 0.78f, 0.55f, 0.9f);
    public static readonly Color LabelColor = new Color(0.95f, 0.93f, 0.88f, 1f);
    public static readonly Color ModalDim = new Color(0.04f, 0.05f, 0.07f, 0.55f);

    [MenuItem("JingJu/Setup Main HUD UI")]
    public static void SetupMainHudOnly()
    {
        var canvasRoot = EnsureGameUICanvas();
        BuildMainHUD(canvasRoot.transform);
        EditorUtility.SetDirty(canvasRoot);
        Debug.Log("[JingJu] Main HUD + character attributes panel created under GameUI.");
    }

    public static GameObject EnsureGameUICanvas()
    {
        var existing = GameObject.Find("GameUI");
        if (existing != null)
            return existing;

        var uiRoot = new GameObject("GameUI");
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        uiRoot.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return uiRoot;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    public static void BuildMainHUD(Transform canvasRoot)
    {
        var old = canvasRoot.Find("MainHUD");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var hudRoot = CreateRect("MainHUD", canvasRoot);
        StretchFull(hudRoot);
        hudRoot.gameObject.AddComponent<MainHUDLayout>();

        // --- Top-left: 角色信息 (5×2 cells) — opens character attributes panel ---
        var characterInfo = CreatePanel(
            hudRoot, "CharacterInfo",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 1f),
            anchoredPos: new Vector2(Cell * 0.25f, -Cell * 0.25f),
            size: new Vector2(Cell * 5f, Cell * 2f),
            label: "角色信息");
        MakePanelClickable(characterInfo);

        // --- Top-right: 任务 / 换装 / 歌唱 (1×1 each) + 地图 (~4×4) ---
        float mapSize = Cell * 4f;
        float topMargin = Cell * 0.25f;
        float rightMargin = Cell * 0.25f;

        var mapPanel = CreatePanel(
            hudRoot, "Map",
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 1f),
            anchoredPos: new Vector2(-rightMargin, -topMargin),
            size: new Vector2(mapSize, mapSize),
            label: "地图");

        float actionY = -topMargin;
        float actionX = -rightMargin - mapSize - Cell * 0.5f;
        CreatePanel(hudRoot, "Slot_Task",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(actionX, actionY), new Vector2(Cell, Cell), "任务");
        CreatePanel(hudRoot, "Slot_Outfit",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(actionX - Cell * 1.15f, actionY), new Vector2(Cell, Cell), "换装");
        CreatePanel(hudRoot, "Slot_Sing",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(actionX - Cell * 2.3f, actionY), new Vector2(Cell, Cell), "歌唱");

        // --- Bottom-left: 背包 (2×2) ---
        float bottomMargin = Cell * 0.35f;
        CreatePanel(hudRoot, "Backpack",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(Cell * 0.35f, bottomMargin),
            new Vector2(Cell * 2f, Cell * 2f), "背包");

        // --- Bottom: 工具栏 (10×1), to the right of backpack ---
        CreatePanel(hudRoot, "Toolbar",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(Cell * 0.35f + Cell * 2f + Cell * 0.25f, bottomMargin),
            new Vector2(Cell * 10f, Cell * 1f), "工具栏");

        // Center hint (optional wireframe — very faint, not blocking play)
        var centerHint = CreatePanel(
            hudRoot, "PlayfieldHint",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(Cell * 14f, Cell * 8f), "游戏画面");
        var hintImage = centerHint.GetComponent<Image>();
        hintImage.color = new Color(1f, 1f, 1f, 0.04f);
        var hintText = centerHint.GetComponentInChildren<Text>();
        if (hintText != null)
            hintText.color = new Color(1f, 1f, 1f, 0.25f);

        CharacterAttributesUISetup.EnsurePanel(canvasRoot);
        CharacterAttributesUISetup.WireCharacterInfoButton(canvasRoot);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static RectTransform CreatePanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 size,
        string label)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var image = rect.gameObject.AddComponent<Image>();
        image.color = PanelFill;
        image.raycastTarget = false;

        var outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = PanelBorder;
        outline.effectDistance = new Vector2(2f, -2f);

        AddLabel(rect, label);
        return rect;
    }

    public static void ApplyHudPanelChrome(GameObject go, bool raycast = false)
    {
        var image = go.GetComponent<Image>();
        if (image == null)
            image = go.AddComponent<Image>();
        image.color = PanelFill;
        image.raycastTarget = raycast;

        if (go.GetComponent<Outline>() == null)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = PanelBorder;
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }

    public static Button MakePanelClickable(RectTransform panel)
    {
        var image = panel.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        var existing = panel.GetComponent<Button>();
        if (existing != null)
            return existing;

        var btn = panel.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 0.95f);
        btn.colors = colors;
        return btn;
    }

    private static void AddLabel(RectTransform parent, string label)
    {
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(parent, false);
        var textRect = textGo.AddComponent<RectTransform>();
        StretchFull(textRect);

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 22;
        text.color = LabelColor;
        text.raycastTarget = false;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
#endif
