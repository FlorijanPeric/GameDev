using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ModernHudRuntime : MonoBehaviour
{
    private const string SensitivityPref = "hud_sensitivity";
    private const string CrosshairPref = "hud_crosshair_style";

    private Canvas rootCanvas;
    private TextMeshProUGUI ammoValueText;
    private TextMeshProUGUI ammoLabelText;
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI roundText;
    private TextMeshProUGUI runStatsText;
    private TextMeshProUGUI upgradePanelText;
    private TextMeshProUGUI healthUpgradeButtonText;
    private TextMeshProUGUI damageUpgradeButtonText;
    private TextMeshProUGUI speedUpgradeButtonText;
    private TextMeshProUGUI sensitivityValueText;
    private TextMeshProUGUI crosshairStyleText;

    private GameObject crosshairDot;
    private GameObject crosshairPlus;
    private GameObject crosshairWide;

    private GameObject menuRoot;
    private GameObject mainMenuPanel;
    private GameObject settingsPanel;
    private GameObject upgradePanel;
    private Button healthUpgradeButton;
    private Button damageUpgradeButton;
    private Button speedUpgradeButton;
    private Slider sensitivitySlider;

    private GunShootTracer cachedShooter;
    private FPSMouseLook cachedMouseLook;
    private PlayerSurvivalHealth cachedPlayerHealth;
    private SurvivalWaveDirector cachedWaveDirector;
    private float nextShooterLookupTime;
    private float nextLookLookupTime;
    private float nextHealthLookupTime;
    private float nextWaveLookupTime;
    private bool specialRoundActive;
    private bool menuOpen;
    private float pendingSensitivity = 2f;
    private int crosshairStyleIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<ModernHudRuntime>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("ModernHudRuntime");
        DontDestroyOnLoad(hudObject);
        hudObject.AddComponent<ModernHudRuntime>();
    }

    private void OnEnable()
    {
        SpecialRoundDomeDirector.SpecialRoundStateChanged += OnSpecialRoundChanged;
    }

    private void OnDisable()
    {
        SpecialRoundDomeDirector.SpecialRoundStateChanged -= OnSpecialRoundChanged;
    }

    private void Start()
    {
        BuildHud();
    }

    private void Update()
    {
        if (rootCanvas == null)
        {
            BuildHud();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        RefreshShooterReference();
        RefreshMouseLookReference();
        RefreshHealthReference();
        RefreshWaveDirectorReference();
        RefreshAmmo();
        RefreshHealth();
        RefreshRoundBanner();
        RefreshUpgradePanel();
        RefreshRunStats();
        RefreshSettingsValues();
    }

    private void BuildHud()
    {
        EnsureEventSystem();

        rootCanvas = FindOrCreateCanvas();
        if (rootCanvas == null)
        {
            return;
        }

        ApplyCanvasDefaults(rootCanvas);

        Transform existing = rootCanvas.transform.Find("ModernHUD");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject hudRoot = CreateUiObject("ModernHUD", rootCanvas.transform);
        RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
        StretchToFill(hudRect);

        CreateCrosshair(hudRoot.transform);
        CreateBottomPanels(hudRoot.transform);
        CreateRoundBanner(hudRoot.transform);
        CreateUpgradePanel(hudRoot.transform);
        CreateRunStats(hudRoot.transform);
        CreateMenu(hudRoot.transform);

        LoadPreferences();
        ApplyCrosshairStyle();
        OpenMenu(true);
    }

    private Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasObject = new GameObject("Canvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void ApplyCanvasDefaults(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
        }
    }

    private void CreateBottomPanels(Transform parent)
    {
        GameObject ammoPanel = CreatePanel("AmmoPanel", parent, new Color(0.04f, 0.07f, 0.1f, 0.75f));
        RectTransform ammoRect = ammoPanel.GetComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(1f, 0f);
        ammoRect.anchorMax = new Vector2(1f, 0f);
        ammoRect.pivot = new Vector2(1f, 0f);
        ammoRect.anchoredPosition = new Vector2(-34f, 34f);
        ammoRect.sizeDelta = new Vector2(290f, 112f);

        ammoLabelText = CreateTmpText("AmmoLabel", ammoPanel.transform, "AMMO", 24, TextAlignmentOptions.TopLeft, new Color(0.62f, 0.84f, 1f, 1f));
        RectTransform ammoLabelRect = ammoLabelText.rectTransform;
        ammoLabelRect.anchorMin = new Vector2(0f, 1f);
        ammoLabelRect.anchorMax = new Vector2(0f, 1f);
        ammoLabelRect.pivot = new Vector2(0f, 1f);
        ammoLabelRect.anchoredPosition = new Vector2(16f, -10f);
        ammoLabelRect.sizeDelta = new Vector2(220f, 28f);

        ammoValueText = CreateTmpText("AmmoValue", ammoPanel.transform, "-- / --", 52, TextAlignmentOptions.BottomLeft, Color.white);
        RectTransform ammoValueRect = ammoValueText.rectTransform;
        ammoValueRect.anchorMin = new Vector2(0f, 0f);
        ammoValueRect.anchorMax = new Vector2(0f, 0f);
        ammoValueRect.pivot = new Vector2(0f, 0f);
        ammoValueRect.anchoredPosition = new Vector2(16f, 12f);
        ammoValueRect.sizeDelta = new Vector2(250f, 62f);

        GameObject healthPanel = CreatePanel("HealthPanel", parent, new Color(0.1f, 0.05f, 0.05f, 0.72f));
        RectTransform healthRect = healthPanel.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 0f);
        healthRect.anchorMax = new Vector2(0f, 0f);
        healthRect.pivot = new Vector2(0f, 0f);
        healthRect.anchoredPosition = new Vector2(34f, 34f);
        healthRect.sizeDelta = new Vector2(220f, 78f);

        healthText = CreateTmpText("HealthValue", healthPanel.transform, "HEALTH 100", 30, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.58f, 0.58f, 1f));
        RectTransform healthTextRect = healthText.rectTransform;
        StretchToFill(healthTextRect);
        healthTextRect.offsetMin = new Vector2(16f, 0f);
        healthTextRect.offsetMax = new Vector2(-10f, 0f);
    }

    private void CreateRoundBanner(Transform parent)
    {
        GameObject roundPanel = CreatePanel("RoundBanner", parent, new Color(0.04f, 0.08f, 0.12f, 0.8f));
        RectTransform roundRect = roundPanel.GetComponent<RectTransform>();
        roundRect.anchorMin = new Vector2(0.5f, 1f);
        roundRect.anchorMax = new Vector2(0.5f, 1f);
        roundRect.pivot = new Vector2(0.5f, 1f);
        roundRect.anchoredPosition = new Vector2(0f, -26f);
        roundRect.sizeDelta = new Vector2(420f, 56f);

        roundText = CreateTmpText("RoundText", roundPanel.transform, "NORMAL ROUND", 28, TextAlignmentOptions.Center, new Color(0.72f, 0.92f, 1f, 1f));
        RectTransform roundTextRect = roundText.rectTransform;
        StretchToFill(roundTextRect);
    }

    private void CreateRunStats(Transform parent)
    {
        GameObject statsPanel = CreatePanel("RunStatsPanel", parent, new Color(0.03f, 0.08f, 0.08f, 0.72f));
        RectTransform statsRect = statsPanel.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 1f);
        statsRect.anchorMax = new Vector2(0f, 1f);
        statsRect.pivot = new Vector2(0f, 1f);
        statsRect.anchoredPosition = new Vector2(34f, -34f);
        statsRect.sizeDelta = new Vector2(360f, 70f);

        runStatsText = CreateTmpText("RunStatsText", statsPanel.transform, "WAVE 0  KILLS 0  CREDITS 0", 24f, TextAlignmentOptions.MidlineLeft, new Color(0.72f, 0.95f, 0.88f, 1f));
        RectTransform runStatsRect = runStatsText.rectTransform;
        StretchToFill(runStatsRect);
        runStatsRect.offsetMin = new Vector2(14f, 0f);
        runStatsRect.offsetMax = new Vector2(-10f, 0f);
    }

    private void CreateUpgradePanel(Transform parent)
    {
        upgradePanel = CreatePanel("UpgradePanel", parent, new Color(0.03f, 0.11f, 0.1f, 0.86f));
        RectTransform panelRect = upgradePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -92f);
        panelRect.sizeDelta = new Vector2(700f, 116f);

        upgradePanelText = CreateTmpText("UpgradePanelText", upgradePanel.transform,
            "INTERMISSION UPGRADES", 23f, TextAlignmentOptions.MidlineLeft, new Color(0.86f, 1f, 0.92f, 1f));

        RectTransform textRect = upgradePanelText.rectTransform;
        StretchToFill(textRect);
        textRect.offsetMin = new Vector2(14f, 56f);
        textRect.offsetMax = new Vector2(-14f, -6f);

        healthUpgradeButton = CreateUpgradeOptionButton(upgradePanel.transform, "HealthUpgradeButton", new Vector2(18f, 14f), out healthUpgradeButtonText);
        healthUpgradeButton.onClick.AddListener(OnHealthUpgradeClicked);

        damageUpgradeButton = CreateUpgradeOptionButton(upgradePanel.transform, "DamageUpgradeButton", new Vector2(238f, 14f), out damageUpgradeButtonText);
        damageUpgradeButton.onClick.AddListener(OnDamageUpgradeClicked);

        speedUpgradeButton = CreateUpgradeOptionButton(upgradePanel.transform, "SpeedUpgradeButton", new Vector2(458f, 14f), out speedUpgradeButtonText);
        speedUpgradeButton.onClick.AddListener(OnSpeedUpgradeClicked);

        upgradePanel.SetActive(false);
    }

    private static Button CreateUpgradeOptionButton(Transform parent, string name, Vector2 anchoredPos, out TextMeshProUGUI labelText)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.09f, 0.25f, 0.22f, 0.96f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(206f, 36f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.09f, 0.25f, 0.22f, 0.96f);
        colors.highlightedColor = new Color(0.16f, 0.35f, 0.31f, 1f);
        colors.pressedColor = new Color(0.07f, 0.18f, 0.16f, 1f);
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.88f);
        button.colors = colors;

        labelText = CreateTmpText("Label", buttonObject.transform, "", 19f, TextAlignmentOptions.Center, Color.white);
        StretchToFill(labelText.rectTransform);

        return button;
    }

    private void CreateCrosshair(Transform parent)
    {
        crosshairDot = CreateUiObject("CrosshairDot", parent);
        Image dotImage = crosshairDot.AddComponent<Image>();
        dotImage.color = new Color(0.65f, 1f, 0.95f, 0.95f);

        RectTransform dotRect = crosshairDot.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0.5f, 0.5f);
        dotRect.anchorMax = new Vector2(0.5f, 0.5f);
        dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;
        dotRect.sizeDelta = new Vector2(7f, 7f);

        crosshairPlus = CreateUiObject("CrosshairPlus", parent);
        RectTransform plusRect = crosshairPlus.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(0.5f, 0.5f);
        plusRect.anchorMax = new Vector2(0.5f, 0.5f);
        plusRect.pivot = new Vector2(0.5f, 0.5f);
        plusRect.anchoredPosition = Vector2.zero;
        plusRect.sizeDelta = new Vector2(24f, 24f);

        CreateBar("H", crosshairPlus.transform, new Vector2(14f, 2f));
        CreateBar("V", crosshairPlus.transform, new Vector2(2f, 14f));

        crosshairWide = CreateUiObject("CrosshairWide", parent);
        RectTransform wideRect = crosshairWide.GetComponent<RectTransform>();
        wideRect.anchorMin = new Vector2(0.5f, 0.5f);
        wideRect.anchorMax = new Vector2(0.5f, 0.5f);
        wideRect.pivot = new Vector2(0.5f, 0.5f);
        wideRect.anchoredPosition = Vector2.zero;
        wideRect.sizeDelta = new Vector2(34f, 34f);

        CreateOffsetBar("L", crosshairWide.transform, new Vector2(-9f, 0f), new Vector2(8f, 2f));
        CreateOffsetBar("R", crosshairWide.transform, new Vector2(9f, 0f), new Vector2(8f, 2f));
        CreateOffsetBar("T", crosshairWide.transform, new Vector2(0f, 9f), new Vector2(2f, 8f));
        CreateOffsetBar("B", crosshairWide.transform, new Vector2(0f, -9f), new Vector2(2f, 8f));
    }

    private void CreateMenu(Transform parent)
    {
        menuRoot = CreateUiObject("MenuRoot", parent);
        RectTransform menuRect = menuRoot.GetComponent<RectTransform>();
        StretchToFill(menuRect);

        Image dim = menuRoot.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.02f, 0.04f, 0.82f);

        mainMenuPanel = CreatePanel("MainMenuPanel", menuRoot.transform, new Color(0.06f, 0.1f, 0.15f, 0.92f));
        RectTransform mainRect = mainMenuPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.sizeDelta = new Vector2(520f, 430f);

        TextMeshProUGUI title = CreateTmpText("Title", mainMenuPanel.transform, "OUTBREAK PROTOCOL", 54f, TextAlignmentOptions.Center, new Color(0.75f, 0.95f, 1f, 1f));
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -50f);
        titleRect.sizeDelta = new Vector2(460f, 90f);

        Button startButton = CreateButton(mainMenuPanel.transform, "StartGameButton", "START GAME", new Vector2(0f, -70f));
        startButton.onClick.AddListener(StartGame);

        Button settingsButton = CreateButton(mainMenuPanel.transform, "SettingsButton", "SETTINGS", new Vector2(0f, -165f));
        settingsButton.onClick.AddListener(OpenSettings);

        Button quitButton = CreateButton(mainMenuPanel.transform, "QuitButton", "QUIT", new Vector2(0f, -260f));
        quitButton.onClick.AddListener(QuitGame);

        settingsPanel = CreatePanel("SettingsPanel", menuRoot.transform, new Color(0.06f, 0.1f, 0.15f, 0.94f));
        RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
        settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
        settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.sizeDelta = new Vector2(620f, 430f);

        TextMeshProUGUI settingsTitle = CreateTmpText("SettingsTitle", settingsPanel.transform, "SETTINGS", 46f, TextAlignmentOptions.Center, new Color(0.75f, 0.95f, 1f, 1f));
        RectTransform settingsTitleRect = settingsTitle.rectTransform;
        settingsTitleRect.anchorMin = new Vector2(0.5f, 1f);
        settingsTitleRect.anchorMax = new Vector2(0.5f, 1f);
        settingsTitleRect.pivot = new Vector2(0.5f, 1f);
        settingsTitleRect.anchoredPosition = new Vector2(0f, -42f);
        settingsTitleRect.sizeDelta = new Vector2(520f, 70f);

        CreateTmpText("SensitivityLabel", settingsPanel.transform, "LOOK SENSITIVITY", 26f, TextAlignmentOptions.Left, new Color(0.65f, 0.88f, 1f, 1f));
        RectTransform sensLabelRect = settingsPanel.transform.Find("SensitivityLabel").GetComponent<RectTransform>();
        sensLabelRect.anchorMin = new Vector2(0f, 1f);
        sensLabelRect.anchorMax = new Vector2(0f, 1f);
        sensLabelRect.pivot = new Vector2(0f, 1f);
        sensLabelRect.anchoredPosition = new Vector2(52f, -132f);
        sensLabelRect.sizeDelta = new Vector2(340f, 34f);

        GameObject sliderGo = CreateUiObject("SensitivitySlider", settingsPanel.transform);
        sensitivitySlider = sliderGo.AddComponent<Slider>();
        sensitivitySlider.minValue = 0.4f;
        sensitivitySlider.maxValue = 8f;
        sensitivitySlider.wholeNumbers = false;
        sensitivitySlider.value = pendingSensitivity;
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(0f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(52f, -182f);
        sliderRect.sizeDelta = new Vector2(390f, 24f);

        Image sliderBg = sliderGo.AddComponent<Image>();
        sliderBg.color = new Color(0.2f, 0.28f, 0.36f, 0.9f);

        GameObject fillArea = CreateUiObject("FillArea", sliderGo.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(8f, 6f);
        fillAreaRect.offsetMax = new Vector2(-8f, -6f);

        GameObject fillObj = CreateUiObject("Fill", fillArea.transform);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.4f, 0.9f, 1f, 1f);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        StretchToFill(fillRect);

        GameObject handle = CreateUiObject("Handle", sliderGo.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.95f, 0.98f, 1f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 30f);

        sensitivitySlider.targetGraphic = handleImage;
        sensitivitySlider.fillRect = fillRect;
        sensitivitySlider.handleRect = handleRect;
        sensitivitySlider.direction = Slider.Direction.LeftToRight;

        sensitivityValueText = CreateTmpText("SensitivityValue", settingsPanel.transform, pendingSensitivity.ToString("0.00"), 24f, TextAlignmentOptions.Left, Color.white);
        RectTransform sensValRect = sensitivityValueText.rectTransform;
        sensValRect.anchorMin = new Vector2(0f, 1f);
        sensValRect.anchorMax = new Vector2(0f, 1f);
        sensValRect.pivot = new Vector2(0f, 1f);
        sensValRect.anchoredPosition = new Vector2(470f, -176f);
        sensValRect.sizeDelta = new Vector2(120f, 34f);

        CreateTmpText("CrosshairLabel", settingsPanel.transform, "CROSSHAIR STYLE", 26f, TextAlignmentOptions.Left, new Color(0.65f, 0.88f, 1f, 1f));
        RectTransform crossLabelRect = settingsPanel.transform.Find("CrosshairLabel").GetComponent<RectTransform>();
        crossLabelRect.anchorMin = new Vector2(0f, 1f);
        crossLabelRect.anchorMax = new Vector2(0f, 1f);
        crossLabelRect.pivot = new Vector2(0f, 1f);
        crossLabelRect.anchoredPosition = new Vector2(52f, -252f);
        crossLabelRect.sizeDelta = new Vector2(360f, 34f);

        crosshairStyleText = CreateTmpText("CrosshairStyleValue", settingsPanel.transform, "DOT", 30f, TextAlignmentOptions.Left, Color.white);
        RectTransform crossValRect = crosshairStyleText.rectTransform;
        crossValRect.anchorMin = new Vector2(0f, 1f);
        crossValRect.anchorMax = new Vector2(0f, 1f);
        crossValRect.pivot = new Vector2(0f, 1f);
        crossValRect.anchoredPosition = new Vector2(52f, -298f);
        crossValRect.sizeDelta = new Vector2(240f, 38f);

        Button prevCrosshair = CreateSmallButton(settingsPanel.transform, "CrosshairPrev", "<", new Vector2(320f, -290f));
        prevCrosshair.onClick.AddListener(PreviousCrosshairStyle);
        Button nextCrosshair = CreateSmallButton(settingsPanel.transform, "CrosshairNext", ">", new Vector2(380f, -290f));
        nextCrosshair.onClick.AddListener(NextCrosshairStyle);

        Button backButton = CreateButton(settingsPanel.transform, "BackButton", "BACK", new Vector2(0f, -354f));
        backButton.onClick.AddListener(CloseSettings);

        settingsPanel.SetActive(false);
    }

    private void RefreshSettingsValues()
    {
        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = pendingSensitivity.ToString("0.00");
        }

        if (crosshairStyleText != null)
        {
            string[] names = { "DOT", "PLUS", "WIDE" };
            int index = Mathf.Clamp(crosshairStyleIndex, 0, names.Length - 1);
            crosshairStyleText.text = names[index];
        }
    }

    private void RefreshMouseLookReference()
    {
        if (cachedMouseLook != null)
        {
            return;
        }

        if (Time.time < nextLookLookupTime)
        {
            return;
        }

        nextLookLookupTime = Time.time + 0.5f;
        cachedMouseLook = FindObjectOfType<FPSMouseLook>();
        if (cachedMouseLook != null)
        {
            cachedMouseLook.sensitivity = pendingSensitivity;
        }
    }

    private void OnSensitivityChanged(float value)
    {
        pendingSensitivity = value;
        if (cachedMouseLook != null)
        {
            cachedMouseLook.sensitivity = pendingSensitivity;
        }

        PlayerPrefs.SetFloat(SensitivityPref, pendingSensitivity);
        PlayerPrefs.Save();
    }

    private void NextCrosshairStyle()
    {
        crosshairStyleIndex = (crosshairStyleIndex + 1) % 3;
        ApplyCrosshairStyle();
    }

    private void PreviousCrosshairStyle()
    {
        crosshairStyleIndex = (crosshairStyleIndex + 2) % 3;
        ApplyCrosshairStyle();
    }

    private void ApplyCrosshairStyle()
    {
        if (crosshairDot == null || crosshairPlus == null || crosshairWide == null)
        {
            return;
        }

        crosshairDot.SetActive(crosshairStyleIndex == 0);
        crosshairPlus.SetActive(crosshairStyleIndex == 1);
        crosshairWide.SetActive(crosshairStyleIndex == 2);

        PlayerPrefs.SetInt(CrosshairPref, crosshairStyleIndex);
        PlayerPrefs.Save();
    }

    private void ToggleMenu()
    {
        OpenMenu(!menuOpen);
    }

    private void OpenMenu(bool open)
    {
        menuOpen = open;
        if (menuRoot != null)
        {
            menuRoot.SetActive(open);
        }

        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void StartGame()
    {
        OpenMenu(false);
    }

    private void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void LoadPreferences()
    {
        pendingSensitivity = PlayerPrefs.GetFloat(SensitivityPref, 2f);
        crosshairStyleIndex = Mathf.Clamp(PlayerPrefs.GetInt(CrosshairPref, 0), 0, 2);

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(pendingSensitivity);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.1f, 0.22f, 0.33f, 0.94f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(320f, 72f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.1f, 0.22f, 0.33f, 0.94f);
        colors.highlightedColor = new Color(0.2f, 0.36f, 0.5f, 1f);
        colors.pressedColor = new Color(0.07f, 0.16f, 0.24f, 1f);
        button.colors = colors;

        TextMeshProUGUI text = CreateTmpText("Label", buttonObject.transform, label, 30f, TextAlignmentOptions.Center, Color.white);
        StretchToFill(text.rectTransform);
        return button;
    }

    private static Button CreateSmallButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject buttonObject = CreatePanel(name, parent, new Color(0.1f, 0.22f, 0.33f, 0.94f));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(48f, 44f);

        Button button = buttonObject.AddComponent<Button>();
        TextMeshProUGUI text = CreateTmpText("Label", buttonObject.transform, label, 30f, TextAlignmentOptions.Center, Color.white);
        StretchToFill(text.rectTransform);
        return button;
    }

    private static void CreateBar(string name, Transform parent, Vector2 size)
    {
        CreateOffsetBar(name, parent, Vector2.zero, size);
    }

    private static void CreateOffsetBar(string name, Transform parent, Vector2 offset, Vector2 size)
    {
        GameObject bar = CreateUiObject(name, parent);
        Image barImage = bar.AddComponent<Image>();
        barImage.color = new Color(0.65f, 1f, 0.95f, 0.95f);

        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.pivot = new Vector2(0.5f, 0.5f);
        barRect.anchoredPosition = offset;
        barRect.sizeDelta = size;
    }

    private void RefreshShooterReference()
    {
        if (cachedShooter != null && cachedShooter.isActiveAndEnabled && cachedShooter.gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.time < nextShooterLookupTime)
        {
            return;
        }

        nextShooterLookupTime = Time.time + 0.5f;
        GunShootTracer[] shooters = FindObjectsOfType<GunShootTracer>(true);
        cachedShooter = null;

        for (int i = 0; i < shooters.Length; i++)
        {
            if (shooters[i] != null && shooters[i].isActiveAndEnabled && shooters[i].gameObject.activeInHierarchy)
            {
                cachedShooter = shooters[i];
                break;
            }
        }
    }

    private void RefreshAmmo()
    {
        if (ammoValueText == null)
        {
            return;
        }

        if (cachedShooter == null)
        {
            ammoValueText.text = "-- / --";
            return;
        }

        ammoValueText.text = cachedShooter.CurrentAmmo + " / " + cachedShooter.ReserveAmmo;
        if (ammoLabelText != null)
        {
            ammoLabelText.text = cachedShooter.IsReloading ? "RELOADING" : "AMMO";
        }
    }

    private void RefreshHealth()
    {
        if (healthText == null)
        {
            return;
        }

        if (cachedPlayerHealth == null)
        {
            healthText.text = "HEALTH --";
            return;
        }

        int current = Mathf.CeilToInt(cachedPlayerHealth.CurrentHealth);
        int max = Mathf.CeilToInt(cachedPlayerHealth.MaxHealth);
        healthText.text = "HEALTH " + current + " / " + max;

        if (cachedPlayerHealth.IsDead)
        {
            healthText.color = new Color(1f, 0.35f, 0.35f, 1f);
        }
        else if (cachedPlayerHealth.CurrentHealth <= cachedPlayerHealth.MaxHealth * 0.3f)
        {
            healthText.color = new Color(1f, 0.7f, 0.45f, 1f);
        }
        else
        {
            healthText.color = new Color(1f, 0.58f, 0.58f, 1f);
        }
    }

    private void RefreshRoundBanner()
    {
        if (roundText == null)
        {
            return;
        }

        if (cachedWaveDirector != null)
        {
            roundText.text = cachedWaveDirector.CurrentPhaseLabel;

            if (cachedWaveDirector.CurrentState == SurvivalWaveDirector.RunState.Intermission && cachedWaveDirector.PhaseTimeRemaining > 0f)
            {
                int seconds = Mathf.CeilToInt(cachedWaveDirector.PhaseTimeRemaining);
                roundText.text = roundText.text + " - " + seconds + "s";
            }

            switch (cachedWaveDirector.CurrentState)
            {
                case SurvivalWaveDirector.RunState.Victory:
                    roundText.color = new Color(0.58f, 1f, 0.62f, 1f);
                    break;
                case SurvivalWaveDirector.RunState.Defeat:
                    roundText.color = new Color(1f, 0.42f, 0.42f, 1f);
                    break;
                case SurvivalWaveDirector.RunState.WaveActive:
                    roundText.color = new Color(0.88f, 0.94f, 1f, 1f);
                    break;
                default:
                    roundText.color = new Color(0.72f, 0.92f, 1f, 1f);
                    break;
            }

            return;
        }

        if (specialRoundActive)
        {
            roundText.text = "SPECIAL ROUND - DOME OUTBREAK";
            roundText.color = new Color(1f, 0.84f, 0.35f, 1f);
        }
        else
        {
            roundText.text = "NORMAL ROUND";
            roundText.color = new Color(0.72f, 0.92f, 1f, 1f);
        }
    }

    private void OnSpecialRoundChanged(bool isActive)
    {
        specialRoundActive = isActive;
    }

    private void RefreshUpgradePanel()
    {
        if (upgradePanel == null || upgradePanelText == null)
        {
            return;
        }

        if (cachedWaveDirector == null || cachedWaveDirector.CurrentState != SurvivalWaveDirector.RunState.Intermission)
        {
            upgradePanel.SetActive(false);
            return;
        }

        upgradePanel.SetActive(true);

        int credits = cachedWaveDirector.Credits;

        string hpLine = FormatUpgradeLine("1", "MAX HP", cachedWaveDirector.HealthUpgradePrice, cachedWaveDirector.HealthUpgradeLevel, credits);
        string dmgLine = FormatUpgradeLine("2", "DAMAGE", cachedWaveDirector.DamageUpgradePrice, cachedWaveDirector.DamageUpgradeLevel, credits);
        string speedLine = FormatUpgradeLine("3", "SPEED", cachedWaveDirector.SpeedUpgradePrice, cachedWaveDirector.SpeedUpgradeLevel, credits);

        upgradePanelText.text =
            "INTERMISSION UPGRADES  <color=#AEEFDA>(CREDITS " + credits + ")</color>\n" +
            hpLine + "    " + dmgLine + "    " + speedLine;

        UpdateUpgradeButton(healthUpgradeButton, healthUpgradeButtonText, "1", "MAX HP", cachedWaveDirector.HealthUpgradePrice, cachedWaveDirector.HealthUpgradeLevel, cachedWaveDirector.CanBuyHealthUpgrade);
        UpdateUpgradeButton(damageUpgradeButton, damageUpgradeButtonText, "2", "DAMAGE", cachedWaveDirector.DamageUpgradePrice, cachedWaveDirector.DamageUpgradeLevel, cachedWaveDirector.CanBuyDamageUpgrade);
        UpdateUpgradeButton(speedUpgradeButton, speedUpgradeButtonText, "3", "SPEED", cachedWaveDirector.SpeedUpgradePrice, cachedWaveDirector.SpeedUpgradeLevel, cachedWaveDirector.CanBuySpeedUpgrade);
    }

    private static string FormatUpgradeLine(string key, string label, int cost, int level, int currentCredits)
    {
        bool affordable = currentCredits >= cost;
        string color = affordable ? "#9CFFCC" : "#FF8F8F";
        return "<color=" + color + ">[" + key + "] " + label + " L" + level + " $" + cost + "</color>";
    }

    private static void UpdateUpgradeButton(Button button, TextMeshProUGUI text, string hotkey, string label, int cost, int level, bool affordable)
    {
        if (button == null || text == null)
        {
            return;
        }

        button.interactable = affordable;
        text.text = "[" + hotkey + "] " + label + " L" + level + "  $" + cost;
        text.color = affordable ? new Color(0.84f, 1f, 0.89f, 1f) : new Color(1f, 0.62f, 0.62f, 1f);
    }

    private void OnHealthUpgradeClicked()
    {
        if (cachedWaveDirector == null)
        {
            return;
        }

        cachedWaveDirector.TryPurchaseHealthUpgrade();
    }

    private void OnDamageUpgradeClicked()
    {
        if (cachedWaveDirector == null)
        {
            return;
        }

        cachedWaveDirector.TryPurchaseDamageUpgrade();
    }

    private void OnSpeedUpgradeClicked()
    {
        if (cachedWaveDirector == null)
        {
            return;
        }

        cachedWaveDirector.TryPurchaseSpeedUpgrade();
    }

    private void RefreshRunStats()
    {
        if (runStatsText == null)
        {
            return;
        }

        if (cachedWaveDirector == null)
        {
            runStatsText.text = "WAVE 0  KILLS 0  CREDITS 0";
            return;
        }

        runStatsText.text =
            "WAVE " + cachedWaveDirector.CurrentWave +
            "  KILLS " + cachedWaveDirector.TotalKills +
            "  CREDITS " + cachedWaveDirector.Credits +
            "  BEST " + cachedWaveDirector.BestWave +
            "  UPG " +
            cachedWaveDirector.HealthUpgradeLevel + "/" +
            cachedWaveDirector.DamageUpgradeLevel + "/" +
            cachedWaveDirector.SpeedUpgradeLevel;
    }

    private void RefreshHealthReference()
    {
        if (cachedPlayerHealth != null && cachedPlayerHealth.isActiveAndEnabled)
        {
            return;
        }

        if (Time.time < nextHealthLookupTime)
        {
            return;
        }

        nextHealthLookupTime = Time.time + 0.5f;
        cachedPlayerHealth = FindObjectOfType<PlayerSurvivalHealth>(true);
    }

    private void RefreshWaveDirectorReference()
    {
        if (cachedWaveDirector != null && cachedWaveDirector.isActiveAndEnabled)
        {
            return;
        }

        if (Time.time < nextWaveLookupTime)
        {
            return;
        }

        nextWaveLookupTime = Time.time + 0.5f;
        cachedWaveDirector = FindObjectOfType<SurvivalWaveDirector>(true);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TextMeshProUGUI CreateTmpText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static void StretchToFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
