using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Starting screen with Levels, Rules, and Settings panels.
/// Layout is constrained to the same portrait mobile format as gameplay.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    const string Level1SceneName = "SampleScene";
    const int LevelCount = 10;

    static readonly Color BgColor = new(0.12f, 0.16f, 0.22f, 1f);
    static readonly Color PanelColor = new(0.16f, 0.21f, 0.28f, 1f);
    static readonly Color AccentColor = new(0.25f, 0.55f, 0.85f, 1f);
    static readonly Color MutedColor = new(0.35f, 0.4f, 0.48f, 1f);
    static readonly Color CardColor = new(0.22f, 0.28f, 0.36f, 1f);

    GameObject _homePanel;
    GameObject _levelsPanel;
    GameObject _rulesPanel;
    GameObject _settingsPanel;
    GameObject _rulesDetailPanel;
    Text _rulesDetailTitle;
    Text _rulesDetailBody;
    Text _levelsStatus;

    struct RuleEntry
    {
        public string Title;
        public string Body;
        public Sprite Icon;
        public Color IconTint;
    }

    void Awake()
    {
        Time.timeScale = 1f;
        MenuUi.EnsureEventSystem();
        BuildUi();
        ShowHome();
    }

    void BuildUi()
    {
        var canvas = MenuUi.CreateCanvas("MenuCanvas");
        // Same 9:19.5 portrait frame as gameplay — black bars on wide desktop Game views.
        var playRoot = MenuUi.CreateMobilePlayArea(canvas.transform, BgColor);

        _homePanel = BuildHomePanel(playRoot);
        _levelsPanel = BuildLevelsPanel(playRoot);
        _rulesPanel = BuildRulesPanel(playRoot);
        _settingsPanel = BuildSettingsPanel(playRoot);
        _rulesDetailPanel = BuildRulesDetailPanel(playRoot);
    }

    GameObject BuildHomePanel(Transform root)
    {
        var panel = MenuUi.Create("HomePanel", root);
        MenuUi.StretchFull(panel.GetComponent<RectTransform>());

        var title = MenuUi.Create("Title", panel.transform);
        MenuUi.SetAnchors(title.GetComponent<RectTransform>(), new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.82f));
        MenuUi.AddText(title, "RoboCarl", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        var subtitle = MenuUi.Create("Subtitle", panel.transform);
        MenuUi.SetAnchors(subtitle.GetComponent<RectTransform>(), new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.70f));
        MenuUi.AddText(subtitle, "Keep Carl powered and oiled", 26, new Color(0.85f, 0.9f, 0.95f));

        var buttons = MenuUi.Create("HomeButtons", panel.transform);
        MenuUi.SetAnchors(buttons.GetComponent<RectTransform>(), new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.56f));
        var layout = buttons.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        MenuUi.CreateFilledButton(buttons.transform, "LevelsButton", "Levels", AccentColor, ShowLevels, 40);
        MenuUi.CreateFilledButton(buttons.transform, "RulesButton", "Rules", AccentColor, ShowRules, 40);
        MenuUi.CreateFilledButton(buttons.transform, "SettingsButton", "Settings", AccentColor, ShowSettings, 40);

        return panel;
    }

    GameObject BuildLevelsPanel(Transform root)
    {
        var panel = MenuUi.Create("LevelsPanel", root);
        MenuUi.StretchFull(panel.GetComponent<RectTransform>());

        MenuUi.CreateTopLeftBack(panel.transform, ShowHome);
        MenuUi.CreateTopTitle(panel.transform, "Levels");

        var status = MenuUi.Create("Status", panel.transform);
        MenuUi.SetAnchors(status.GetComponent<RectTransform>(), new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.91f));
        _levelsStatus = MenuUi.AddText(status, "Choose a level", 24, new Color(0.8f, 0.85f, 0.9f));

        var grid = MenuUi.CreateGrid(
            panel.transform,
            "LevelGrid",
            new Vector2(0.06f, 0.04f),
            new Vector2(0.94f, 0.85f),
            columns: 2,
            rows: 5,
            spacing: new Vector2(12f, 12f));

        for (var i = 0; i < LevelCount; i++)
        {
            int level = i + 1;
            bool available = level <= 2;
            var color = available ? AccentColor : MutedColor;
            string label = available ? $"Level {level}" : $"Level {level}\nSoon";

            MenuUi.CreateFilledButton(
                grid.transform,
                $"Level{level}",
                label,
                color,
                () => OnLevelSelected(level),
                available ? 32 : 26);
        }

        return panel;
    }

    GameObject BuildRulesPanel(Transform root)
    {
        var panel = MenuUi.Create("RulesPanel", root);
        MenuUi.StretchFull(panel.GetComponent<RectTransform>());

        MenuUi.CreateTopLeftBack(panel.transform, ShowHome);
        MenuUi.CreateTopTitle(panel.transform, "Rules");

        var hint = MenuUi.Create("Hint", panel.transform);
        MenuUi.SetAnchors(hint.GetComponent<RectTransform>(), new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.91f));
        MenuUi.AddText(hint, "Scroll and tap an icon to learn more", 22, new Color(0.8f, 0.85f, 0.9f));

        var entries = BuildRuleEntries();
        var gridContent = MenuUi.CreateScrollableGrid(
            panel.transform,
            "RulesScroll",
            new Vector2(0.05f, 0.04f),
            new Vector2(0.95f, 0.85f),
            columns: 2,
            spacing: new Vector2(10f, 10f),
            cellHeightRatio: 1.05f);

        foreach (var entry in entries)
            CreateRuleCard(gridContent, entry);

        return panel;
    }

    GameObject BuildRulesDetailPanel(Transform root)
    {
        var panel = MenuUi.Create("RulesDetailPanel", root);
        MenuUi.StretchFull(panel.GetComponent<RectTransform>());

        var dim = MenuUi.Create("Dim", panel.transform);
        var dimImage = MenuUi.AddImage(dim, new Color(0f, 0f, 0f, 0.72f));
        MenuUi.StretchFull(dim.GetComponent<RectTransform>());
        var dimButton = dim.AddComponent<Button>();
        dimButton.targetGraphic = dimImage;
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(HideRulesDetail);

        var card = MenuUi.Create("Card", panel.transform);
        MenuUi.SetAnchors(
            card.GetComponent<RectTransform>(),
            new Vector2(0.07f, 0.22f),
            new Vector2(0.93f, 0.78f));
        MenuUi.AddImage(card, PanelColor);

        var title = MenuUi.Create("Title", card.transform);
        MenuUi.SetAnchors(title.GetComponent<RectTransform>(), new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.95f));
        _rulesDetailTitle = MenuUi.AddText(title, "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        var body = MenuUi.Create("Body", card.transform);
        MenuUi.SetAnchors(body.GetComponent<RectTransform>(), new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.76f));
        _rulesDetailBody = MenuUi.AddText(body, "", 26, new Color(0.9f, 0.93f, 0.96f), TextAnchor.UpperCenter);

        var close = MenuUi.Create("CloseButton", card.transform);
        MenuUi.SetAnchors(close.GetComponent<RectTransform>(), new Vector2(0.25f, 0.05f), new Vector2(0.75f, 0.18f));
        var closeImage = MenuUi.AddImage(close, AccentColor);
        var closeButton = close.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(HideRulesDetail);
        var closeLabel = MenuUi.Create("Label", close.transform);
        MenuUi.AddText(closeLabel, "Close", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        MenuUi.StretchFull(closeLabel.GetComponent<RectTransform>());

        panel.SetActive(false);
        return panel;
    }

    GameObject BuildSettingsPanel(Transform root)
    {
        var panel = MenuUi.Create("SettingsPanel", root);
        MenuUi.StretchFull(panel.GetComponent<RectTransform>());

        MenuUi.CreateTopLeftBack(panel.transform, ShowHome);
        MenuUi.CreateTopTitle(panel.transform, "Settings");

        var content = MenuUi.Create("SettingsContent", panel.transform);
        MenuUi.SetAnchors(content.GetComponent<RectTransform>(), new Vector2(0.06f, 0.2f), new Vector2(0.94f, 0.86f));
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24f;
        layout.padding = new RectOffset(0, 0, 20, 20);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateVolumeSlider(content.transform, "Music", "Music Volume", GameAudioSettings.MusicVolume, value =>
        {
            GameAudioSettings.MusicVolume = value;
        });

        CreateVolumeSlider(content.transform, "Sfx", "SFX Volume", GameAudioSettings.SfxVolume, value =>
        {
            GameAudioSettings.SfxVolume = value;
        });

        var note = MenuUi.Create("Note", content.transform);
        var noteLayout = note.AddComponent<LayoutElement>();
        noteLayout.preferredHeight = 90f;
        MenuUi.AddText(
            note,
            "Music and SFX volumes apply immediately and are saved between sessions.",
            22,
            new Color(0.75f, 0.8f, 0.85f),
            TextAnchor.MiddleCenter);

        return panel;
    }

    void CreateVolumeSlider(Transform parent, string id, string label, float initial, Action<float> onChanged)
    {
        var block = MenuUi.Create($"{id}Block", parent);
        var layoutElement = block.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 140f;
        MenuUi.AddImage(block, CardColor);

        var labelObject = MenuUi.Create("Label", block.transform);
        MenuUi.SetAnchors(labelObject.GetComponent<RectTransform>(), new Vector2(0.05f, 0.55f), new Vector2(0.68f, 0.92f));
        MenuUi.AddText(labelObject, label, 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

        var valueObject = MenuUi.Create("Value", block.transform);
        MenuUi.SetAnchors(valueObject.GetComponent<RectTransform>(), new Vector2(0.68f, 0.55f), new Vector2(0.95f, 0.92f));
        var valueText = MenuUi.AddText(valueObject, $"{Mathf.RoundToInt(initial * 100f)}%", 26, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleRight);

        var sliderObject = MenuUi.Create("Slider", block.transform);
        MenuUi.SetAnchors(sliderObject.GetComponent<RectTransform>(), new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.48f));

        var background = MenuUi.Create("Background", sliderObject.transform);
        MenuUi.AddImage(background, MutedColor);
        MenuUi.StretchFull(background.GetComponent<RectTransform>());

        var fillArea = MenuUi.Create("Fill Area", sliderObject.transform);
        MenuUi.StretchFull(fillArea.GetComponent<RectTransform>());
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        var fill = MenuUi.Create("Fill", fillArea.transform);
        MenuUi.AddImage(fill, AccentColor);
        MenuUi.StretchFull(fill.GetComponent<RectTransform>());

        var handleArea = MenuUi.Create("Handle Slide Area", sliderObject.transform);
        MenuUi.StretchFull(handleArea.GetComponent<RectTransform>());
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        var handle = MenuUi.Create("Handle", handleArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 0f);
        var handleImage = MenuUi.AddImage(handle, Color.white);

        var slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = initial;
        slider.onValueChanged.AddListener(value =>
        {
            valueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            onChanged(value);
        });
    }

    RuleEntry[] BuildRuleEntries()
    {
        var appearance = Resources.Load<CarlAppearance>("CarlAppearance");
        var oil = Resources.Load<Sprite>("UI/Icon_Oil");
        var battery = Resources.Load<Sprite>("UI/Icon_Battery");

        return new[]
        {
            new RuleEntry
            {
                Title = "Carl",
                Icon = appearance != null ? appearance.Normal : null,
                IconTint = Color.white,
                Body = "This is Carl in good shape. Keep his energy and oil topped up so he can keep exploring."
            },
            new RuleEntry
            {
                Title = "Tired Carl",
                Icon = appearance != null ? appearance.Tired : null,
                IconTint = Color.white,
                Body = "Carl looks tired when his energy drops below half. Energy drains over time. If it hits zero, the run ends."
            },
            new RuleEntry
            {
                Title = "Grubby Carl",
                Icon = appearance != null ? appearance.Grubby : null,
                IconTint = Color.white,
                Body = "Carl gets grubby when oil is low. Oil is used when he walks. Low oil makes him slower and sparks start to show."
            },
            new RuleEntry
            {
                Title = "Tired & Grubby",
                Icon = appearance != null ? appearance.TiredGrubby : null,
                IconTint = Color.white,
                Body = "When both energy and oil are low, Carl is tired and grubby at once. Grab pickups quickly to recover."
            },
            new RuleEntry
            {
                Title = "Oil Can",
                Icon = oil,
                IconTint = Color.white,
                Body = "Oil cans restore Carl's oil. Walk into one to clean him up and get his normal walking speed back."
            },
            new RuleEntry
            {
                Title = "Battery",
                Icon = battery,
                IconTint = Color.white,
                Body = "Batteries restore Carl's energy. Pick them up before energy runs out or the level is lost."
            },
            new RuleEntry
            {
                Title = "Spring",
                Icon = null,
                IconTint = new Color(0.78f, 0.52f, 0.22f, 1f),
                Body = "Stand on a spring to bounce upward. Use springs to reach high places like the green win line."
            },
            new RuleEntry
            {
                Title = "One-Way Platform",
                Icon = null,
                IconTint = new Color(0.28f, 0.28f, 0.32f, 1f),
                Body = "Thin platforms with corner supports. Carl can rise up through them from below, but lands on top and cannot fall through."
            },
            new RuleEntry
            {
                Title = "Win Line",
                Icon = null,
                IconTint = new Color(0.25f, 0.85f, 0.35f, 1f),
                Body = "The green line is the goal. Bounce high enough from a spring to touch it and finish the level."
            },
            new RuleEntry
            {
                Title = "Draggable Wall",
                Icon = null,
                IconTint = new Color(0.55f, 0.58f, 0.62f, 1f),
                Body = "Click and drag a metal wall to slide it along its length. Each wall can only move a set distance from where it starts — release to leave it in place."
            },
            new RuleEntry
            {
                Title = "Button Wall",
                Icon = null,
                IconTint = new Color(0.95f, 0.18f, 0.18f, 1f),
                Body = "Tap the big red button to send this wall sliding the full length of its track. It starts slow and speeds up. You can't press again until it finishes moving."
            },
            new RuleEntry
            {
                Title = "Spikes",
                Icon = null,
                IconTint = new Color(0.72f, 0.72f, 0.76f, 1f),
                Body = "Sharp floor spikes. Bumping them from the side knocks Carl back and stuns him for a second. Falling on top of them ends the run and returns to the main menu."
            }
        };
    }

    void CreateRuleCard(Transform parent, RuleEntry entry)
    {
        var card = MenuUi.Create(entry.Title.Replace(" ", "").Replace("&", "And").Replace("-", ""), parent);
        var image = MenuUi.AddImage(card, CardColor);
        var button = card.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => ShowRulesDetail(entry.Title, entry.Body));

        if (entry.Title == "Spring")
        {
            CreateSpringRuleIcon(card.transform);
        }
        else if (entry.Title == "Draggable Wall")
        {
            CreateWallRuleIcon(card.transform);
        }
        else if (entry.Title == "Button Wall")
        {
            CreateButtonWallRuleIcon(card.transform);
        }
        else if (entry.Title == "Spikes")
        {
            CreateSpikesRuleIcon(card.transform);
        }
        else if (entry.Title == "One-Way Platform")
        {
            CreatePlatformRuleIcon(card.transform);
        }
        else
        {
            var iconObject = MenuUi.Create("Icon", card.transform);
            bool isLineIcon = entry.Title == "Win Line";
            if (isLineIcon)
                MenuUi.SetAnchors(iconObject.GetComponent<RectTransform>(), new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.68f));
            else
                MenuUi.SetAnchors(iconObject.GetComponent<RectTransform>(), new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.92f));

            var iconImage = MenuUi.AddImage(iconObject, entry.IconTint, entry.Icon);
            iconImage.preserveAspect = !isLineIcon;
            if (entry.Icon == null && entry.IconTint.a <= 0f)
                iconImage.color = new Color(0.4f, 0.45f, 0.5f, 1f);
        }

        var label = MenuUi.Create("Label", card.transform);
        MenuUi.SetAnchors(label.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.26f));
        MenuUi.AddText(label, entry.Title, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    void CreateSpringRuleIcon(Transform card)
    {
        var iconRoot = MenuUi.Create("Icon", card);
        MenuUi.SetAnchors(iconRoot.GetComponent<RectTransform>(), new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.92f));

        AddSpringCoilBar(iconRoot.transform, 0.02f, 0.14f, 0.95f, new Color(0.45f, 0.28f, 0.12f, 1f));
        AddSpringCoilBar(iconRoot.transform, 0.18f, 0.32f, 0.82f, new Color(0.7f, 0.45f, 0.18f, 1f));
        AddSpringCoilBar(iconRoot.transform, 0.36f, 0.50f, 0.78f, new Color(0.78f, 0.52f, 0.22f, 1f));
        AddSpringCoilBar(iconRoot.transform, 0.54f, 0.68f, 0.82f, new Color(0.7f, 0.45f, 0.18f, 1f));
        AddSpringCoilBar(iconRoot.transform, 0.72f, 0.88f, 0.90f, new Color(0.35f, 0.55f, 0.75f, 1f));
    }

    void CreatePlatformRuleIcon(Transform card)
    {
        var iconRoot = MenuUi.Create("Icon", card);
        MenuUi.SetAnchors(iconRoot.GetComponent<RectTransform>(), new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.90f));

        var deck = MenuUi.Create("Deck", iconRoot.transform);
        MenuUi.SetAnchors(deck.GetComponent<RectTransform>(), new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.82f));
        MenuUi.AddImage(deck, new Color(0.28f, 0.28f, 0.32f, 1f));

        var left = MenuUi.Create("LeftSupport", iconRoot.transform);
        MenuUi.SetAnchors(left.GetComponent<RectTransform>(), new Vector2(0.08f, 0.18f), new Vector2(0.32f, 0.62f));
        left.transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        MenuUi.AddImage(left, new Color(0.22f, 0.22f, 0.26f, 1f));

        var right = MenuUi.Create("RightSupport", iconRoot.transform);
        MenuUi.SetAnchors(right.GetComponent<RectTransform>(), new Vector2(0.68f, 0.18f), new Vector2(0.92f, 0.62f));
        right.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        MenuUi.AddImage(right, new Color(0.22f, 0.22f, 0.26f, 1f));
    }

    void CreateWallRuleIcon(Transform card)
    {
        var iconRoot = MenuUi.Create("Icon", card);
        MenuUi.SetAnchors(iconRoot.GetComponent<RectTransform>(), new Vector2(0.34f, 0.28f), new Vector2(0.66f, 0.92f));

        var plate = MenuUi.Create("Plate", iconRoot.transform);
        MenuUi.StretchFull(plate.GetComponent<RectTransform>());
        MenuUi.AddImage(plate, new Color(0.55f, 0.58f, 0.62f, 1f));

        AddWallBolt(iconRoot.transform, 0.18f, 0.78f);
        AddWallBolt(iconRoot.transform, 0.18f, 0.48f);
        AddWallBolt(iconRoot.transform, 0.18f, 0.18f);
        AddWallBolt(iconRoot.transform, 0.62f, 0.78f);
        AddWallBolt(iconRoot.transform, 0.62f, 0.48f);
        AddWallBolt(iconRoot.transform, 0.62f, 0.18f);
    }

    void CreateButtonWallRuleIcon(Transform card)
    {
        var iconRoot = MenuUi.Create("Icon", card);
        MenuUi.SetAnchors(iconRoot.GetComponent<RectTransform>(), new Vector2(0.34f, 0.28f), new Vector2(0.66f, 0.92f));

        var plate = MenuUi.Create("Plate", iconRoot.transform);
        MenuUi.StretchFull(plate.GetComponent<RectTransform>());
        MenuUi.AddImage(plate, new Color(0.42f, 0.48f, 0.58f, 1f));

        var railL = MenuUi.Create("RailL", iconRoot.transform);
        MenuUi.SetAnchors(railL.GetComponent<RectTransform>(), new Vector2(0.12f, 0.12f), new Vector2(0.22f, 0.88f));
        MenuUi.AddImage(railL, new Color(0.35f, 0.85f, 0.95f, 1f));

        var railR = MenuUi.Create("RailR", iconRoot.transform);
        MenuUi.SetAnchors(railR.GetComponent<RectTransform>(), new Vector2(0.78f, 0.12f), new Vector2(0.88f, 0.88f));
        MenuUi.AddImage(railR, new Color(0.35f, 0.85f, 0.95f, 1f));

        var rim = MenuUi.Create("ButtonRim", iconRoot.transform);
        MenuUi.SetAnchors(rim.GetComponent<RectTransform>(), new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.72f));
        MenuUi.AddImage(rim, new Color(0.35f, 0.08f, 0.08f, 1f));

        var face = MenuUi.Create("ButtonFace", iconRoot.transform);
        MenuUi.SetAnchors(face.GetComponent<RectTransform>(), new Vector2(0.28f, 0.38f), new Vector2(0.72f, 0.66f));
        MenuUi.AddImage(face, new Color(0.95f, 0.18f, 0.18f, 1f));
    }

    void CreateSpikesRuleIcon(Transform card)
    {
        var iconRoot = MenuUi.Create("Icon", card);
        MenuUi.SetAnchors(iconRoot.GetComponent<RectTransform>(), new Vector2(0.16f, 0.30f), new Vector2(0.84f, 0.90f));

        var bas = MenuUi.Create("Base", iconRoot.transform);
        MenuUi.SetAnchors(bas.GetComponent<RectTransform>(), new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.28f));
        MenuUi.AddImage(bas, new Color(0.22f, 0.2f, 0.22f, 1f));

        AddSpikeTooth(iconRoot.transform, 0.08f);
        AddSpikeTooth(iconRoot.transform, 0.36f);
        AddSpikeTooth(iconRoot.transform, 0.64f);
    }

    void AddSpikeTooth(Transform parent, float xMin)
    {
        var tooth = MenuUi.Create("Tooth", parent);
        MenuUi.SetAnchors(
            tooth.GetComponent<RectTransform>(),
            new Vector2(xMin, 0.22f),
            new Vector2(xMin + 0.28f, 0.92f));
        tooth.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        MenuUi.AddImage(tooth, new Color(0.72f, 0.72f, 0.76f, 1f));
    }

    void AddWallBolt(Transform parent, float xMin, float yMin)
    {
        var bolt = MenuUi.Create("Bolt", parent);
        MenuUi.SetAnchors(
            bolt.GetComponent<RectTransform>(),
            new Vector2(xMin, yMin),
            new Vector2(xMin + 0.2f, yMin + 0.12f));
        MenuUi.AddImage(bolt, new Color(0.28f, 0.3f, 0.33f, 1f));
    }

    void AddSpringCoilBar(Transform parent, float yMin, float yMax, float widthFraction, Color color)
    {
        float xPad = (1f - widthFraction) * 0.5f;
        var bar = MenuUi.Create("Coil", parent);
        MenuUi.SetAnchors(
            bar.GetComponent<RectTransform>(),
            new Vector2(xPad, yMin),
            new Vector2(1f - xPad, yMax));
        MenuUi.AddImage(bar, color);
    }

    void OnLevelSelected(int level)
    {
        if (level == 1)
        {
            LevelSession.SelectedLevel = 1;
            SceneManager.LoadScene(Level1SceneName);
            return;
        }

        if (level == 2)
        {
            // Use the shared SampleScene base; Level2Layout strips L1 props and
            // spawns the draggable wall from Level2Spawns.
            LevelSession.SelectedLevel = 2;
            SceneManager.LoadScene(Level1SceneName);
            return;
        }

        if (_levelsStatus != null)
            _levelsStatus.text = $"Level {level} is not ready yet.";
    }

    void ShowHome()
    {
        SetPanel(_homePanel);
        HideRulesDetail();
    }

    void ShowLevels()
    {
        if (_levelsStatus != null)
            _levelsStatus.text = "Choose a level";
        SetPanel(_levelsPanel);
        HideRulesDetail();
    }

    void ShowRules()
    {
        SetPanel(_rulesPanel);
        HideRulesDetail();
    }

    void ShowSettings()
    {
        SetPanel(_settingsPanel);
        HideRulesDetail();
    }

    void ShowRulesDetail(string title, string body)
    {
        if (_rulesDetailTitle != null)
            _rulesDetailTitle.text = title;
        if (_rulesDetailBody != null)
            _rulesDetailBody.text = body;
        if (_rulesDetailPanel != null)
            _rulesDetailPanel.SetActive(true);
    }

    void HideRulesDetail()
    {
        if (_rulesDetailPanel != null)
            _rulesDetailPanel.SetActive(false);
    }

    void SetPanel(GameObject active)
    {
        _homePanel.SetActive(active == _homePanel);
        _levelsPanel.SetActive(active == _levelsPanel);
        _rulesPanel.SetActive(active == _rulesPanel);
        _settingsPanel.SetActive(active == _settingsPanel);
    }
}
