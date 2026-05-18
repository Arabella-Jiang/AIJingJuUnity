#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character attributes panel — matches main HUD wireframe style (dark panels, gold outline).
/// </summary>
public static class CharacterAttributesUISetup
{
    [MenuItem("JingJu/Setup Character Attributes Panel")]
    public static void SetupStandalone()
    {
        var canvasRoot = MainHUDUISetup.EnsureGameUICanvas();
        EnsurePanel(canvasRoot.transform);
        WireCharacterInfoButton(canvasRoot.transform);
        EditorUtility.SetDirty(canvasRoot);
        Debug.Log("[JingJu] Character attributes panel created under GameUI/CharacterAttributesPanel.");
    }

    public static CharacterAttributesPanel EnsurePanel(Transform canvasRoot)
    {
        var existing = canvasRoot.Find("CharacterAttributesPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var host = new GameObject("CharacterAttributesPanel");
        host.transform.SetParent(canvasRoot, false);
        host.transform.SetAsLastSibling();

        var hostRect = host.AddComponent<RectTransform>();
        StretchFull(hostRect);

        var controller = host.AddComponent<CharacterAttributesPanel>();

        var panelRoot = CreateRect("PanelRoot", host.transform);
        StretchFull(panelRoot);
        var dimmer = panelRoot.gameObject.AddComponent<Image>();
        dimmer.color = MainHUDUISetup.ModalDim;
        dimmer.raycastTarget = true;

        var window = CreateRect("Window", panelRoot);
        window.anchorMin = new Vector2(0.5f, 0.5f);
        window.anchorMax = new Vector2(0.5f, 0.5f);
        window.pivot = new Vector2(0.5f, 0.5f);
        window.anchoredPosition = Vector2.zero;
        window.sizeDelta = new Vector2(1040f, 620f);
        MainHUDUISetup.ApplyHudPanelChrome(window.gameObject);

        BuildPanelContent(window, controller);

        var so = new SerializedObject(controller);
        so.FindProperty("panelRoot").objectReferenceValue = panelRoot.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        panelRoot.gameObject.SetActive(false);
        return controller;
    }

    public static void WireCharacterInfoButton(Transform canvasRoot)
    {
        var charInfo = canvasRoot.Find("MainHUD/CharacterInfo");
        if (charInfo == null)
        {
            Debug.LogWarning("[JingJu] MainHUD/CharacterInfo not found. Run Setup Main HUD UI first.");
            return;
        }

        var controller = canvasRoot.GetComponentInChildren<CharacterAttributesPanel>(true);
        if (controller == null)
        {
            Debug.LogWarning("[JingJu] CharacterAttributesPanel not found.");
            return;
        }

        var charInfoRect = charInfo as RectTransform;
        if (charInfoRect == null)
            return;

        var btn = charInfoRect.GetComponent<Button>();
        if (btn == null)
            btn = MainHUDUISetup.MakePanelClickable(charInfoRect);

        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            var target = btn.onClick.GetPersistentTarget(i);
            if (target == controller)
                return;
        }

        UnityEventTools.AddPersistentListener(btn.onClick, controller.Open);
    }

    private static void BuildPanelContent(RectTransform window, CharacterAttributesPanel controller)
    {
        const float pad = 20f;
        const float titleHeight = 44f;
        const float headerRowHeight = 48f;
        const float contentGap = 16f;

        CreateTextBlock(
            window, "Title",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot: new Vector2(0.5f, 1f),
            pos: new Vector2(0f, -pad),
            size: new Vector2(-pad * 2f, titleHeight),
            text: "角色属性",
            fontSize: 28,
            alignment: TextAnchor.MiddleLeft,
            color: MainHUDUISetup.LabelColor);

        var headerRow = CreateRect("HeaderRow", window);
        headerRow.anchorMin = new Vector2(0f, 1f);
        headerRow.anchorMax = new Vector2(1f, 1f);
        headerRow.pivot = new Vector2(0.5f, 1f);
        headerRow.anchoredPosition = new Vector2(0f, -(pad + titleHeight + 4f));
        headerRow.sizeDelta = new Vector2(-pad * 2f, headerRowHeight);
        headerRow.offsetMin = new Vector2(pad, headerRow.offsetMin.y);
        headerRow.offsetMax = new Vector2(-pad, headerRow.offsetMax.y);

        BuildTabs(headerRow);
        BuildCloseControl(headerRow, controller);

        var content = CreateRect("Content", window);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(pad, pad);
        content.offsetMax = new Vector2(-pad, -(pad + titleHeight + headerRowHeight + contentGap + 4f));

        const float gap = 16f;
        const float characterWidth = 280f;

        CreateHudBlock(content, "CharacterArea",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 0.5f),
            pos: Vector2.zero,
            width: characterWidth,
            label: "角色");

        var attributesArea = CreateRect("AttributesArea", content);
        attributesArea.anchorMin = Vector2.zero;
        attributesArea.anchorMax = Vector2.one;
        attributesArea.offsetMin = new Vector2(characterWidth + gap, 0f);
        attributesArea.offsetMax = Vector2.zero;
        MainHUDUISetup.ApplyHudPanelChrome(attributesArea.gameObject);
        AddCenteredLabel(attributesArea, "基本属性描述", 22, MainHUDUISetup.LabelColor);
    }

    private static void BuildTabs(RectTransform headerRow)
    {
        var tabsRoot = CreateRect("Tabs", headerRow);
        tabsRoot.anchorMin = new Vector2(0f, 0f);
        tabsRoot.anchorMax = new Vector2(0.65f, 1f);
        tabsRoot.offsetMin = Vector2.zero;
        tabsRoot.offsetMax = Vector2.zero;

        var layout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        foreach (var label in new[] { "角色", "背包", "装扮" })
            CreateTab(tabsRoot, label, 108f);
    }

    private static void CreateTab(Transform parent, string label, float width)
    {
        var tab = CreateRect($"Tab_{label}", parent);
        var le = tab.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minHeight = 40f;

        MainHUDUISetup.ApplyHudPanelChrome(tab.gameObject);
        AddCenteredLabel(tab, label, 20, MainHUDUISetup.LabelColor);
    }

    private static void BuildCloseControl(RectTransform headerRow, CharacterAttributesPanel controller)
    {
        var closeRow = CreateRect("CloseRow", headerRow);
        closeRow.anchorMin = new Vector2(1f, 0f);
        closeRow.anchorMax = new Vector2(1f, 1f);
        closeRow.pivot = new Vector2(1f, 0.5f);
        closeRow.anchoredPosition = Vector2.zero;
        closeRow.sizeDelta = new Vector2(140f, 0f);

        var layout = closeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var btnRect = CreateRect("CloseButton", closeRow);
        var btnLe = btnRect.gameObject.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 88f;
        btnLe.preferredHeight = 40f;

        MainHUDUISetup.ApplyHudPanelChrome(btnRect.gameObject, raycast: true);
        AddCenteredLabel(btnRect, "关闭", 20, MainHUDUISetup.LabelColor);

        var btn = btnRect.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 0.95f);
        btn.colors = colors;

        UnityEventTools.AddPersistentListener(btn.onClick, controller.OnCloseClicked);
    }

    private static void CreateHudBlock(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 pos,
        float width,
        string label)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.offsetMin = new Vector2(0f, 0f);
        rect.offsetMax = new Vector2(width, 0f);

        MainHUDUISetup.ApplyHudPanelChrome(rect.gameObject);
        AddCenteredLabel(rect, label, 22, MainHUDUISetup.LabelColor);
    }

    private static RectTransform CreateTextBlock(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 pos,
        Vector2 size,
        string text,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        var rect = CreateRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return rect;
    }

    private static void AddCenteredLabel(RectTransform parent, string label, int fontSize, Color color)
    {
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(parent, false);
        var textRect = textGo.AddComponent<RectTransform>();
        StretchFull(textRect);

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
}
#endif
