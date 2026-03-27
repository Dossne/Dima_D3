using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TapMiner.Core
{
    public sealed class PauseConfig : ScriptableObject
    {
        public bool soundEnabled = true;
        public bool vibrationEnabled = true;
    }

    [CreateAssetMenu(fileName = "ScrollConfig", menuName = "Tap Miner/Presentation/Scroll Config")]
    public sealed class ScrollConfig : ScriptableObject
    {
        public float scrollSpeedUnitsPerSec = 3.0f;
        public float tileHeightUnits = 4.0f;
        public int visibleTileBuffer = 2;
    }

    /// <summary>
    /// Builds a minimal runtime presentation layer over the existing vertical-slice systems.
    /// </summary>
    public sealed class RunPresentationController : MonoBehaviour
    {
        private const float LaneSpacing = 2f;
        private const string BestDepthPlayerPrefsKey = "tap_miner.best_depth";
        private const string PauseSoundPlayerPrefsKey = "pause.sound";
        private const string PauseVibrationPlayerPrefsKey = "pause.vibration";
        private static readonly Color LaneBaseColor = new Color(0.16f, 0.18f, 0.25f, 1f);
        private static readonly Color LaneCurrentColor = new Color(0.28f, 0.4f, 0.58f, 1f);
        private static readonly Color LaneSafeColor = new Color(0.16f, 0.48f, 0.34f, 1f);
        private static readonly Color LaneDangerColor = new Color(0.5f, 0.2f, 0.2f, 1f);
        private static readonly Color LaneRewardColor = new Color(0.53f, 0.42f, 0.15f, 1f);
        private static readonly Color MarkerHazardColor = new Color(0.92f, 0.28f, 0.24f, 1f);
        private static readonly Color MarkerBreakColor = new Color(0.96f, 0.71f, 0.21f, 1f);
        private static readonly Color MarkerSafeColor = new Color(0.24f, 0.83f, 0.55f, 1f);
        private static readonly Color MarkerNeutralColor = new Color(0.4f, 0.44f, 0.55f, 1f);

        [SerializeField]
        private bool debugPreviewResultsOverlay;

        [SerializeField]
        private Material _urpLitBaseMaterial;

        [SerializeField]
        private ScrollConfig scrollConfig;

        [SerializeField]
        private PauseConfig pauseConfig;

        private AppBootstrap bootstrap;
        private Camera mainCamera;

        private GameObject runtimeRoot;
        private Transform worldScrollRoot;
        private Transform laneRoot;
        private Transform markerRoot;
        private Transform collapseRoot;
        private Transform playerVisualRoot;
        private Renderer[] laneRenderers;
        private Renderer[] markerRenderers;
        private Renderer[] rewardRenderers;
        private Renderer[] collapseRenderers;
        private Renderer[] laneGlowRenderers;
        private Renderer[] laneDividerRenderers;
        private Renderer playerBodyRenderer;
        private Renderer playerHelmetRenderer;
        private Renderer playerShadowRenderer;
        private Renderer playerLampRenderer;

        private Canvas canvas;
        private Text depthText;
        private Text coinText;
        private Image collapseFillImage;
        private Image collapseFrameImage;
        private Text promptText;
        private Text menuTitleText;
        private Text menuSubtitleText;
        private Text menuBestDepthText;
        private GameObject resultsOverlay;
        private Image resultsOverlayImage;
        private Image resultsPanelImage;
        private Text resultsSubtitleText;
        private Text resultsHintText;
        private Text resultsRestartLabelText;
        private Text resultsDepthText;
        private Text resultsCoinsText;
        private Button restartButton;
        private Button upgradesButton;
        private Button upgradeBackButton;
        private Button pauseButton;
        private Button pauseResumeButton;
        private Button pauseRestartButton;
        private Button pauseSoundButton;
        private Button pauseVibrationButton;
        private Button pauseMenuButton;
        private GameObject upgradePanel;
        private GameObject pauseOverlay;
        private Text feedbackText;
        private Image hitFlashImage;
        private Text pauseButtonLabel;
        private Text pauseTitleText;
        private Text pauseSoundLabel;
        private Text pauseVibrationLabel;

        private int lastVisibleDepth;
        private int lastVisibleRunCoins;
        private int bestVisibleDepth;
        private string lastFeedbackMessage = string.Empty;
        private float feedbackEmphasisTimer;
        private float resultsTransition;
        private int lastCommittedLaneIndex = 1;
        private int lastRunRewardValue;
        private int lastRunHealth;
        private bool isUpgradePanelOpen;
        private float laneShiftImpulse;
        private float breakImpulse;
        private float lootImpulse;
        private float hitImpulse;
        private float collapseSurgeImpulse;
        private float hitFlashAlpha;
        private Vector3 baseCameraPosition;
        private readonly List<Transform> scrollTiles = new();
        private readonly Color[] runLaneColors = new Color[3];
        private int scrollPoolSize = -1;
        private RunState lastLaneColorState;
        private bool scrollStateInitialized;
        private RunState lastScrollState;
        private bool scrollTilesLogged;
        private int lastMarkerSegmentIndex = int.MinValue;
        private RunState _prevResultsPresentationState;
        // TM-CORE-01: scroll distance → depth
        private float _totalScrolledUnits;

        private void Awake()
        {
            bootstrap = GetComponent<AppBootstrap>();
            mainCamera = Camera.main;
            bestVisibleDepth = Mathf.Max(0, PlayerPrefs.GetInt(BestDepthPlayerPrefsKey, 0));
            LoadScrollConfig();
            LoadPauseConfig();
            EnsureCamera();
            EnsureWorldPresentation();
            EnsureCanvasPresentation();
            EnsureEventSystem();
        }

        private void Update()
        {
            if (bootstrap == null)
            {
                return;
            }

            var currentState = bootstrap.CurrentRunState;
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log("[SCROLL-CHECK] state=" + currentState + " activeTiles=" + (scrollTiles == null ? "null" : scrollTiles.Count.ToString()));
            }

            DetectPresentationEvents();
            UpdateWorldPresentation();
            UpdateHudPresentation();
            UpdatePausePresentation();
            UpdateResultsPresentation();
            TickPresentationImpulses();
        }

        public void OnPrimaryActionPressed()
        {
            if (bootstrap == null)
            {
                return;
            }

            bootstrap.RequestMenuPlay();
            SetPauseOverlayVisible(false);
        }

        public void OnUpgradesPressed()
        {
            isUpgradePanelOpen = true;
        }

        public void OnUpgradeBackPressed()
        {
            isUpgradePanelOpen = false;
        }

        public void OnPausePressed()
        {
            bootstrap?.RequestTogglePause();
        }

        public void OnPauseResumePressed()
        {
            bootstrap?.RequestTogglePause();
        }

        public void OnPauseRestartPressed()
        {
            if (bootstrap == null)
            {
                return;
            }

            if (bootstrap.CurrentRunState == RunState.RunActive)
            {
                bootstrap.NotifyLethalDamage();
            }

            if (bootstrap.CurrentRunState == RunState.RunDeathResolved)
            {
                bootstrap.RequestRestartRun();
            }

            SetPauseOverlayVisible(false);
        }

        public void OnPauseSoundPressed()
        {
            if (pauseConfig == null)
            {
                return;
            }

            pauseConfig.soundEnabled = !pauseConfig.soundEnabled;
            SavePauseConfig();
            UpdatePauseToggleLabels();
        }

        public void OnPauseVibrationPressed()
        {
            if (pauseConfig == null)
            {
                return;
            }

            pauseConfig.vibrationEnabled = !pauseConfig.vibrationEnabled;
            SavePauseConfig();
            UpdatePauseToggleLabels();
        }

        public void OnPauseMenuPressed()
        {
            Application.Quit();
        }

        public void SetPauseOverlayVisible(bool isVisible)
        {
            if (pauseOverlay == null)
            {
                return;
            }

            var shouldBeVisible = bootstrap != null &&
                                  bootstrap.CurrentRunState == RunState.RunActive &&
                                  isVisible;
            pauseOverlay.SetActive(shouldBeVisible);
            UpdatePauseToggleLabels();
        }

        public Transform GetPlayerTransform()
        {
            if (playerVisualRoot == null)
            {
                playerVisualRoot = FindOrCreate("PlayerVisualRoot", transform).transform;
                playerVisualRoot.localPosition = new Vector3(0f, -1.95f, -0.3f);
            }

            return playerVisualRoot;
        }

        private void EnsureCamera()
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 4.85f;
            mainCamera.transform.position = new Vector3(0f, 0.15f, -10f);
            mainCamera.backgroundColor = new Color(0.03f, 0.04f, 0.06f, 1f);
            baseCameraPosition = mainCamera.transform.position;
        }

        private void EnsureWorldPresentation()
        {
            runtimeRoot = FindOrCreate("PresentationRuntime", transform);
            worldScrollRoot = FindOrCreate("WorldScrollRoot", runtimeRoot.transform).transform;
            laneRoot = FindOrCreate("LanePresentation", runtimeRoot.transform).transform;
            markerRoot = FindOrCreate("CurrentSegmentMarkers", runtimeRoot.transform).transform;
            collapseRoot = FindOrCreate("CollapsePresentation", runtimeRoot.transform).transform;

            EnsureScrollTilePool();
            CreateLaneGuides();
            CreateCollapseCeiling();
            CreatePlayerVisual();      // must run before CreateSegmentMarkers so playerVisualRoot exists
            CreateSegmentMarkers();
        }

        private void LoadScrollConfig()
        {
            if (scrollConfig != null)
            {
                return;
            }

            scrollConfig = Resources.Load<ScrollConfig>("ScrollConfig");
            if (scrollConfig == null)
            {
                scrollConfig = ScriptableObject.CreateInstance<ScrollConfig>();
            }
        }

        private void LoadPauseConfig()
        {
            if (pauseConfig == null)
            {
                pauseConfig = Resources.Load<PauseConfig>("PauseConfig");
            }

            if (pauseConfig == null)
            {
                pauseConfig = ScriptableObject.CreateInstance<PauseConfig>();
            }

            pauseConfig.soundEnabled = PlayerPrefs.GetInt(PauseSoundPlayerPrefsKey, pauseConfig.soundEnabled ? 1 : 0) == 1;
            pauseConfig.vibrationEnabled = PlayerPrefs.GetInt(PauseVibrationPlayerPrefsKey, pauseConfig.vibrationEnabled ? 1 : 0) == 1;
        }

        private void SavePauseConfig()
        {
            if (pauseConfig == null)
            {
                return;
            }

            PlayerPrefs.SetInt(PauseSoundPlayerPrefsKey, pauseConfig.soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PauseVibrationPlayerPrefsKey, pauseConfig.vibrationEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void EnsureCanvasPresentation()
        {
            var canvasObject = GameObject.Find("BootstrapCanvas");
            if (canvasObject == null)
            {
                canvasObject = new GameObject("BootstrapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            var root = canvasObject.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;

            feedbackText = EnsureText(
                "BootstrapStatusText",
                canvasObject.transform,
                "READY",
                46,
                TextAnchor.MiddleCenter,
                new Color(0.97f, 0.98f, 1f, 1f));
            StretchRect(feedbackText.rectTransform, new Vector2(0.2f, 0.14f), new Vector2(0.8f, 0.2f), Vector2.zero, Vector2.zero);

            hitFlashImage = EnsureImage(
                "HitFlash",
                canvasObject.transform,
                new Color(1f, 0.22f, 0.18f, 0f));
            hitFlashImage.raycastTarget = false;
            StretchRect(hitFlashImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            depthText = EnsureText(
                "DepthLabel",
                canvasObject.transform,
                "DEPTH 000",
                40,
                TextAnchor.MiddleLeft,
                new Color(0.95f, 0.97f, 1f, 1f));
            StretchRect(depthText.rectTransform, new Vector2(0.07f, 0.92f), new Vector2(0.45f, 0.97f), Vector2.zero, Vector2.zero);

            coinText = EnsureText(
                "CoinLabel",
                canvasObject.transform,
                "COINS 000",
                40,
                TextAnchor.MiddleRight,
                new Color(1f, 0.85f, 0.26f, 1f));
            StretchRect(coinText.rectTransform, new Vector2(0.55f, 0.92f), new Vector2(0.93f, 0.97f), Vector2.zero, Vector2.zero);

            var collapseBar = EnsureImage(
                "CollapseBar",
                canvasObject.transform,
                new Color(0.1f, 0.11f, 0.15f, 0.92f));
            collapseBar.raycastTarget = false;
            StretchRect(collapseBar.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.895f), Vector2.zero, Vector2.zero);
            collapseFrameImage = collapseBar;

            collapseFillImage = EnsureImage(
                "CollapseBarFill",
                collapseBar.transform,
                new Color(0.25f, 0.82f, 0.46f, 0.95f));
            collapseFillImage.raycastTarget = false;
            collapseFillImage.type = Image.Type.Filled;
            collapseFillImage.fillMethod = Image.FillMethod.Horizontal;
            collapseFillImage.fillOrigin = 0;
            collapseFillImage.fillAmount = 0.1f;
            StretchRect(collapseFillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 5f), new Vector2(-6f, -5f));

            promptText = EnsureText(
                "RunPromptText",
                canvasObject.transform,
                "TAP TO START",
                36,
                TextAnchor.MiddleCenter,
                new Color(0.8f, 0.89f, 1f, 0.82f));
            StretchRect(promptText.rectTransform, new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.83f), Vector2.zero, Vector2.zero);

            pauseButton = EnsureButton("PauseButton", canvasObject.transform, "II");
            var pauseButtonRect = pauseButton.GetComponent<RectTransform>();
            pauseButtonRect.anchorMin = new Vector2(1f, 1f);
            pauseButtonRect.anchorMax = new Vector2(1f, 1f);
            pauseButtonRect.pivot = new Vector2(1f, 1f);
            pauseButtonRect.sizeDelta = new Vector2(80f, 80f);
            pauseButtonRect.anchoredPosition = new Vector2(-48f, -48f);
            ConfigureButtonColors(
                pauseButton,
                new Color(0.28f, 0.4f, 0.58f, 0.98f),
                new Color(0.38f, 0.5f, 0.68f, 1f),
                new Color(0.18f, 0.3f, 0.48f, 1f),
                Color.white);
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPausePressed);
            pauseButtonLabel = pauseButton.transform.Find("Label")?.GetComponent<Text>();
            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.fontSize = 40;
                pauseButtonLabel.fontStyle = FontStyle.Bold;
                pauseButtonLabel.color = Color.white;
            }

            resultsOverlay = FindOrCreate("MainMenuOverlay", canvasObject.transform);
            resultsOverlayImage = EnsureImageComponent(resultsOverlay, new Color(0.04f, 0.05f, 0.08f, 0f));
            resultsOverlayImage.raycastTarget = false;
            StretchRect(resultsOverlayImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var resultsPanel = EnsureImage(
                "MainMenuPanel",
                resultsOverlay.transform,
                new Color(0.12f, 0.13f, 0.18f, 0.96f));
            resultsPanel.raycastTarget = false;
            StretchRect(resultsPanel.rectTransform, new Vector2(0.14f, 0.23f), new Vector2(0.86f, 0.74f), Vector2.zero, Vector2.zero);
            resultsPanelImage = resultsPanel;

            menuTitleText = EnsureText(
                "MenuTitle",
                resultsPanel.transform,
                "TAP MINER",
                84,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.48f, 1f));
            StretchRect(menuTitleText.rectTransform, new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);

            menuSubtitleText = EnsureText(
                "MenuSubtitle",
                resultsPanel.transform,
                "COLLAPSE RUN",
                34,
                TextAnchor.MiddleCenter,
                new Color(0.69f, 0.93f, 0.92f, 0.96f));
            StretchRect(menuSubtitleText.rectTransform, new Vector2(0.14f, 0.67f), new Vector2(0.86f, 0.75f), Vector2.zero, Vector2.zero);

            menuBestDepthText = EnsureText(
                "BestDepthText",
                resultsPanel.transform,
                "BEST: 000",
                32,
                TextAnchor.MiddleCenter,
                new Color(0.92f, 0.95f, 1f, 0.92f));
            StretchRect(menuBestDepthText.rectTransform, new Vector2(0.18f, 0.59f), new Vector2(0.82f, 0.66f), Vector2.zero, Vector2.zero);

            var resultsTitle = EnsureText(
                "ResultsTitle",
                resultsPanel.transform,
                "LAST RUN",
                34,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.48f, 1f));
            StretchRect(resultsTitle.rectTransform, new Vector2(0.16f, 0.44f), new Vector2(0.84f, 0.53f), Vector2.zero, Vector2.zero);

            resultsSubtitleText = EnsureText(
                "ResultsSubtitleText",
                resultsPanel.transform,
                "BANKED THIS RUN",
                24,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 0.84f, 0.93f, 0.95f));
            StretchRect(resultsSubtitleText.rectTransform, new Vector2(0.16f, 0.38f), new Vector2(0.84f, 0.45f), Vector2.zero, Vector2.zero);

            resultsDepthText = EnsureText(
                "ResultsDepthText",
                resultsPanel.transform,
                "DEPTH REACHED 000",
                40,
                TextAnchor.MiddleCenter,
                Color.white);
            StretchRect(resultsDepthText.rectTransform, new Vector2(0.14f, 0.29f), new Vector2(0.86f, 0.38f), Vector2.zero, Vector2.zero);

            resultsCoinsText = EnsureText(
                "ResultsCoinsText",
                resultsPanel.transform,
                "RUN COINS +0",
                38,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.26f, 1f));
            StretchRect(resultsCoinsText.rectTransform, new Vector2(0.14f, 0.21f), new Vector2(0.86f, 0.29f), Vector2.zero, Vector2.zero);

            resultsHintText = EnsureText(
                "ResultsHintText",
                resultsPanel.transform,
                "HIT PLAY TO DROP BACK IN",
                24,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.9f, 0.98f, 0.88f));
            StretchRect(resultsHintText.rectTransform, new Vector2(0.16f, 0.15f), new Vector2(0.84f, 0.21f), Vector2.zero, Vector2.zero);

            upgradesButton = EnsureButton("UpgradesButton", resultsPanel.transform, "UPGRADES");
            StretchRect(upgradesButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0.13f), new Vector2(0.84f, 0.23f), Vector2.zero, Vector2.zero);
            ConfigureButtonColors(
                upgradesButton,
                new Color(0.23f, 0.72f, 0.73f, 1f),
                new Color(0.34f, 0.83f, 0.84f, 1f),
                new Color(0.13f, 0.56f, 0.57f, 1f),
                new Color(0.07f, 0.14f, 0.15f, 1f));
            upgradesButton.onClick.RemoveAllListeners();
            upgradesButton.onClick.AddListener(OnUpgradesPressed);

            restartButton = EnsureButton("PlayButton", resultsPanel.transform, "PLAY");
            StretchRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.16f, 0.03f), new Vector2(0.84f, 0.12f), Vector2.zero, Vector2.zero);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnPrimaryActionPressed);
            resultsRestartLabelText = restartButton.transform.Find("Label")?.GetComponent<Text>();
            ConfigureButtonColors(
                restartButton,
                new Color(0.97f, 0.8f, 0.18f, 1f),
                new Color(1f, 0.88f, 0.38f, 1f),
                new Color(0.86f, 0.63f, 0.08f, 1f),
                new Color(0.12f, 0.12f, 0.12f, 1f));

            upgradePanel = FindOrCreate("UpgradePanel", resultsPanel.transform);
            var upgradePanelImage = EnsureImageComponent(upgradePanel, new Color(0.09f, 0.11f, 0.15f, 0.98f));
            upgradePanelImage.raycastTarget = false;
            StretchRect(upgradePanelImage.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            var upgradeTitleText = EnsureText(
                "UpgradeTitle",
                upgradePanel.transform,
                "UPGRADES",
                62,
                TextAnchor.MiddleCenter,
                new Color(0.23f, 0.87f, 0.84f, 1f));
            StretchRect(upgradeTitleText.rectTransform, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.84f), Vector2.zero, Vector2.zero);

            var upgradeComingSoonText = EnsureText(
                "UpgradeComingSoonText",
                upgradePanel.transform,
                "COMING SOON",
                44,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.48f, 1f));
            StretchRect(upgradeComingSoonText.rectTransform, new Vector2(0.14f, 0.44f), new Vector2(0.86f, 0.57f), Vector2.zero, Vector2.zero);

            var upgradeBodyText = EnsureText(
                "UpgradeBodyText",
                upgradePanel.transform,
                "SHOP WIRING LANDS NEXT.\nTHIS SCREEN IS A STUB.",
                24,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 0.9f, 0.98f, 0.9f));
            StretchRect(upgradeBodyText.rectTransform, new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.42f), Vector2.zero, Vector2.zero);

            upgradeBackButton = EnsureButton("UpgradeBackButton", upgradePanel.transform, "BACK");
            StretchRect(upgradeBackButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.2f), Vector2.zero, Vector2.zero);
            ConfigureButtonColors(
                upgradeBackButton,
                new Color(0.23f, 0.72f, 0.73f, 1f),
                new Color(0.34f, 0.83f, 0.84f, 1f),
                new Color(0.13f, 0.56f, 0.57f, 1f),
                new Color(0.07f, 0.14f, 0.15f, 1f));
            upgradeBackButton.onClick.RemoveAllListeners();
            upgradeBackButton.onClick.AddListener(OnUpgradeBackPressed);

            upgradePanel.SetActive(false);
            resultsOverlay.SetActive(false);

            pauseOverlay = FindOrCreate("PauseOverlay", canvasObject.transform);
            var pauseOverlayImage = EnsureImageComponent(pauseOverlay, new Color(0.1f, 0.1f, 0.1f, 0.85f));
            pauseOverlayImage.raycastTarget = false;
            StretchRect(pauseOverlayImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var pausePanel = FindOrCreateUiObject("PausePanel", pauseOverlay.transform);
            var pausePanelRect = pausePanel.GetComponent<RectTransform>();
            pausePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            pausePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            pausePanelRect.pivot = new Vector2(0.5f, 0.5f);
            pausePanelRect.sizeDelta = new Vector2(620f, 760f);
            pausePanelRect.anchoredPosition = Vector2.zero;

            pauseTitleText = EnsureText("PauseTitle", pausePanel.transform, "PAUSED", 52, TextAnchor.MiddleCenter, Color.white);
            pauseTitleText.fontStyle = FontStyle.Bold;
            SetAnchoredRect(pauseTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 80f), new Vector2(0f, -40f));

            pauseResumeButton = EnsureButton("PauseResumeButton", pausePanel.transform, "RESUME");
            SetAnchoredRect(pauseResumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(560f, 90f), new Vector2(0f, -155f));
            ConfigureButtonColors(
                pauseResumeButton,
                new Color(0.28f, 0.4f, 0.58f, 1f),
                new Color(0.38f, 0.5f, 0.68f, 1f),
                new Color(0.18f, 0.3f, 0.48f, 1f),
                Color.white);
            pauseResumeButton.onClick.RemoveAllListeners();
            pauseResumeButton.onClick.AddListener(OnPauseResumePressed);

            pauseRestartButton = EnsureButton("PauseRestartButton", pausePanel.transform, "RESTART");
            SetAnchoredRect(pauseRestartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(560f, 90f), new Vector2(0f, -270f));
            ConfigureButtonColors(
                pauseRestartButton,
                new Color(0.5f, 0.2f, 0.2f, 1f),
                new Color(0.62f, 0.28f, 0.28f, 1f),
                new Color(0.38f, 0.12f, 0.12f, 1f),
                Color.white);
            pauseRestartButton.onClick.RemoveAllListeners();
            pauseRestartButton.onClick.AddListener(OnPauseRestartPressed);

            pauseSoundButton = EnsureButton("PauseSoundButton", pausePanel.transform, "SOUND: ON");
            SetAnchoredRect(pauseSoundButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(560f, 90f), new Vector2(0f, -385f));
            ConfigureButtonColors(
                pauseSoundButton,
                new Color(0.25f, 0.25f, 0.3f, 1f),
                new Color(0.34f, 0.34f, 0.4f, 1f),
                new Color(0.17f, 0.17f, 0.22f, 1f),
                Color.white);
            pauseSoundButton.onClick.RemoveAllListeners();
            pauseSoundButton.onClick.AddListener(OnPauseSoundPressed);
            pauseSoundLabel = pauseSoundButton.transform.Find("Label")?.GetComponent<Text>();

            pauseVibrationButton = EnsureButton("PauseVibrationButton", pausePanel.transform, "VIBRATION: ON");
            SetAnchoredRect(pauseVibrationButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(560f, 90f), new Vector2(0f, -500f));
            ConfigureButtonColors(
                pauseVibrationButton,
                new Color(0.25f, 0.25f, 0.3f, 1f),
                new Color(0.34f, 0.34f, 0.4f, 1f),
                new Color(0.17f, 0.17f, 0.22f, 1f),
                Color.white);
            pauseVibrationButton.onClick.RemoveAllListeners();
            pauseVibrationButton.onClick.AddListener(OnPauseVibrationPressed);
            pauseVibrationLabel = pauseVibrationButton.transform.Find("Label")?.GetComponent<Text>();

            pauseMenuButton = EnsureButton("PauseMenuButton", pausePanel.transform, "MENU");
            SetAnchoredRect(pauseMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(400f, 70f), new Vector2(0f, -620f));
            ConfigureButtonColors(
                pauseMenuButton,
                new Color(0.3f, 0.3f, 0.3f, 1f),
                new Color(0.38f, 0.38f, 0.38f, 1f),
                new Color(0.22f, 0.22f, 0.22f, 1f),
                Color.white);
            pauseMenuButton.onClick.RemoveAllListeners();
            pauseMenuButton.onClick.AddListener(OnPauseMenuPressed);

            UpdatePauseToggleLabels();
            pauseOverlay.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(canvas.transform.parent, false);
        }

        private void CreateLaneGuides()
        {
            var visibleHalfWidth = GetVisibleHalfWidth();
            laneRenderers = new Renderer[3];
            laneGlowRenderers = new Renderer[3];
            laneDividerRenderers = new Renderer[2];

            for (var laneIndex = 0; laneIndex < 3; laneIndex += 1)
            {
                var lane = CreatePrimitive($"Lane_{laneIndex}", PrimitiveType.Cube, laneRoot);
                lane.transform.localPosition = new Vector3(GetLaneXPosition(visibleHalfWidth, laneIndex), -0.45f, 0f);
                lane.transform.localScale = new Vector3(visibleHalfWidth * 0.345f, 7.35f, 0.15f);
                laneRenderers[laneIndex] = lane.GetComponent<Renderer>();

                var glow = CreatePrimitive($"LaneGlow_{laneIndex}", PrimitiveType.Quad, lane.transform);
                glow.transform.localPosition = new Vector3(0f, 0f, 0.42f);
                glow.transform.localScale = new Vector3(1.25f, 1.05f, 1f);
                laneGlowRenderers[laneIndex] = glow.GetComponent<Renderer>();

                var rail = CreatePrimitive($"LaneRail_{laneIndex}", PrimitiveType.Cube, lane.transform);
                rail.transform.localPosition = new Vector3(0f, 0f, -0.36f);
                rail.transform.localScale = new Vector3(1.08f, 0.07f, 0.45f);
                SetRendererColor(rail, new Color(0.72f, 0.58f, 0.18f, 1f));
            }

            for (var dividerIndex = 0; dividerIndex < laneDividerRenderers.Length; dividerIndex += 1)
            {
                var divider = CreatePrimitive($"LaneDivider_{dividerIndex}", PrimitiveType.Quad, laneRoot);
                divider.transform.localPosition = new Vector3((dividerIndex == 0 ? -1f : 1f) * visibleHalfWidth * 0.23f, -0.45f, 0.55f);
                divider.transform.localScale = new Vector3(0.12f, 0.94f, 1f);
                laneDividerRenderers[dividerIndex] = divider.GetComponent<Renderer>();
                SetRendererColor(divider, new Color(0.82f, 0.88f, 1f, 0.16f));
            }
        }

        private void CreateSegmentMarkers()
        {
            var visibleHalfWidth = GetVisibleHalfWidth();
            markerRenderers = new Renderer[3];
            rewardRenderers = new Renderer[3];

            // marker Y = playerY - 2.0f, adjusted for markerRoot parent offset
            var markerY = playerVisualRoot != null
                ? playerVisualRoot.position.y - 2.0f
                : -1.95f - 2.0f;
            var localMarkerY = markerY - markerRoot.position.y;
            Debug.Log("[MARKER] markerY=" + markerY + " playerY=" + (playerVisualRoot != null ? playerVisualRoot.position.y : -1.95f));

            for (var laneIndex = 0; laneIndex < 3; laneIndex += 1)
            {
                var marker = CreatePrimitive($"SegmentMarker_{laneIndex}", PrimitiveType.Cube, markerRoot);
                marker.transform.localPosition = new Vector3(GetLaneXPosition(visibleHalfWidth, laneIndex), localMarkerY, -0.2f);
                marker.transform.localScale = new Vector3(1.02f, 1.18f, 0.65f);
                markerRenderers[laneIndex] = marker.GetComponent<Renderer>();

                var reward = CreatePrimitive($"RewardMarker_{laneIndex}", PrimitiveType.Sphere, marker.transform);
                reward.transform.localPosition = new Vector3(0f, 1.08f, 0f);
                reward.transform.localScale = new Vector3(0.52f, 0.52f, 0.52f);
                rewardRenderers[laneIndex] = reward.GetComponent<Renderer>();
            }
        }

        private void CreateCollapseCeiling()
        {
            var visibleHalfWidth = GetVisibleHalfWidth();
            collapseRenderers = new Renderer[2];

            var band = CreatePrimitive("CollapseBand", PrimitiveType.Cube, collapseRoot);
            band.transform.localPosition = new Vector3(0f, 3.35f, 0.2f);
            band.transform.localScale = new Vector3(visibleHalfWidth * 2.02f, 0.62f, 0.4f);
            collapseRenderers[0] = band.GetComponent<Renderer>();

            var glow = CreatePrimitive("CollapseGlow", PrimitiveType.Cube, collapseRoot);
            glow.transform.localPosition = new Vector3(0f, 3.02f, 0.4f);
            glow.transform.localScale = new Vector3(visibleHalfWidth * 1.89f, 0.2f, 0.15f);
            collapseRenderers[1] = glow.GetComponent<Renderer>();
        }

        private void CreatePlayerVisual()
        {
            playerVisualRoot = FindOrCreate("PlayerVisualRoot", transform).transform;
            playerVisualRoot.localPosition = new Vector3(0f, -1.95f, -0.3f);

            var shadow = CreatePrimitive("PlayerShadow", PrimitiveType.Quad, playerVisualRoot);
            shadow.transform.localPosition = new Vector3(0f, 0.05f, 0.34f);
            shadow.transform.localScale = new Vector3(0.9f, 0.34f, 1f);
            playerShadowRenderer = shadow.GetComponent<Renderer>();

            var body = CreatePrimitive("PlayerBody", PrimitiveType.Capsule, playerVisualRoot);
            body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            body.transform.localScale = new Vector3(0.74f, 1.02f, 0.58f);
            playerBodyRenderer = body.GetComponent<Renderer>();

            var helmet = CreatePrimitive("PlayerHelmet", PrimitiveType.Sphere, playerVisualRoot);
            helmet.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            helmet.transform.localScale = new Vector3(0.72f, 0.54f, 0.72f);
            playerHelmetRenderer = helmet.GetComponent<Renderer>();

            var lamp = CreatePrimitive("PlayerLamp", PrimitiveType.Cube, helmet.transform);
            lamp.transform.localPosition = new Vector3(0f, 0.04f, -0.46f);
            lamp.transform.localScale = new Vector3(0.22f, 0.14f, 0.08f);
            playerLampRenderer = lamp.GetComponent<Renderer>();
            SetRendererColor(lamp, new Color(1f, 0.93f, 0.62f, 1f));
        }

        private void UpdateWorldPresentation()
        {
            var visibleHalfWidth = GetVisibleHalfWidth();
            var state = bootstrap.CurrentRunState;
            UpdateScrollPresentation(state);
            if (state == RunState.RunActive && lastLaneColorState != RunState.RunActive)
            {
                GenerateRunLaneColors();
                // Reset gate only on RunActive entry — NOT every non-Active frame,
                // which was causing spurious re-colors on tap/segment-boundary frames.
                lastMarkerSegmentIndex = int.MinValue;
            }

            lastLaneColorState = state;
            var segment = bootstrap.GetCurrentSegmentDescriptor();
            UpdateMarkerColorsIfNeeded(segment);
            // TM-CORE-01: collapse progress is time-driven from AppBootstrap
            var collapseProgress = bootstrap.CollapseProximity;
            var pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;
            var lanePunch = laneShiftImpulse * 0.18f;

            for (var laneIndex = 0; laneIndex < laneRenderers.Length; laneIndex += 1)
            {
                var laneColor = runLaneColors[laneIndex].a > 0f ? runLaneColors[laneIndex] : LaneBaseColor;
                var laneGlowColor = new Color(laneColor.r, laneColor.g, laneColor.b, 0.22f);

                SetRendererColor(laneRenderers[laneIndex], laneColor);
                SetRendererColor(laneGlowRenderers[laneIndex], laneGlowColor);
                laneGlowRenderers[laneIndex].transform.localScale = new Vector3(
                    1.25f + (laneIndex == bootstrap.CurrentCommittedLaneIndex ? 0.06f + lanePunch : 0f),
                    1.05f + (pulse * 0.03f) + (laneIndex == bootstrap.CurrentCommittedLaneIndex ? lanePunch * 0.6f : 0f),
                    1f);
                markerRenderers[laneIndex].transform.localScale = new Vector3(
                    laneIndex == bootstrap.CurrentCommittedLaneIndex ? 1.12f + lanePunch : 1.02f,
                    1.18f + Mathf.Sin(Time.time * 2.2f + laneIndex) * 0.1f + (segment != null && segment.BreakableLaneMask[laneIndex] ? breakImpulse * 0.16f : 0f) + (segment != null && segment.HazardLaneMask[laneIndex] ? hitImpulse * 0.12f : 0f),
                    0.65f);

                var hasReward = segment != null && segment.HasRewardPath && laneIndex == segment.RewardLaneIndex;
                rewardRenderers[laneIndex].gameObject.SetActive(hasReward);
                if (hasReward)
                {
                    SetRendererColor(rewardRenderers[laneIndex], new Color(1f, 0.82f, 0.22f, 1f));
                    rewardRenderers[laneIndex].transform.localScale = Vector3.one * (0.52f + (pulse * 0.07f) + (lootImpulse * 0.16f));
                }
            }

            for (var dividerIndex = 0; dividerIndex < laneDividerRenderers.Length; dividerIndex += 1)
            {
                var dividerAlpha = 0.18f + (collapseProgress * 0.08f);
                SetRendererColor(laneDividerRenderers[dividerIndex], new Color(0.87f, 0.91f, 1f, dividerAlpha));
            }

            playerVisualRoot.localScale = new Vector3(
                1f + (laneShiftImpulse * 0.06f) + (breakImpulse * 0.08f),
                1f - (laneShiftImpulse * 0.05f) - (hitImpulse * 0.06f),
                1f);

            if (playerShadowRenderer != null)
            {
                SetRendererColor(playerShadowRenderer, new Color(0f, 0f, 0f, 0.35f));
                playerShadowRenderer.transform.localScale = new Vector3(
                    0.9f + (laneShiftImpulse * 0.18f) + (breakImpulse * 0.14f),
                    0.34f + (laneShiftImpulse * 0.04f),
                    1f);
            }

            if (playerBodyRenderer != null)
            {
                SetRendererColor(playerBodyRenderer, state == RunState.RunDeathResolved
                    ? new Color(0.7f, 0.24f, 0.22f, 1f)
                    : new Color(0.13f, 0.3f, 0.5f, 1f));
            }

            if (playerHelmetRenderer != null)
            {
                SetRendererColor(playerHelmetRenderer, state == RunState.RunDeathResolved
                    ? new Color(0.92f, 0.4f, 0.3f, 1f)
                    : new Color(1f, 0.83f, 0.18f, 1f));
            }

            if (playerLampRenderer != null)
            {
                SetRendererColor(playerLampRenderer, Color.Lerp(
                    new Color(1f, 0.92f, 0.66f, 1f),
                    new Color(1f, 0.98f, 0.84f, 1f),
                    pulse));
                playerLampRenderer.transform.localScale = new Vector3(
                    0.22f + (lootImpulse * 0.12f) + (hitImpulse * 0.08f),
                    0.14f + (lootImpulse * 0.06f),
                    0.08f);
            }

            if (collapseRenderers != null && collapseRenderers.Length == 2)
            {
                collapseRoot.localPosition = new Vector3(0f, -collapseProgress * 1.1f - (collapseSurgeImpulse * 0.08f), 0f);
                SetRendererColor(collapseRenderers[0], Color.Lerp(
                    new Color(0.42f, 0.3f, 0.12f, 1f),
                    new Color(0.88f, 0.25f, 0.22f, 1f),
                    collapseProgress));
                SetRendererColor(collapseRenderers[1], Color.Lerp(
                    new Color(0.92f, 0.72f, 0.2f, 1f),
                    new Color(1f, 0.34f, 0.26f, 1f),
                    collapseProgress));
                collapseRenderers[1].transform.localScale = new Vector3(
                    visibleHalfWidth * 1.89f,
                    0.2f + (collapseProgress * 0.12f) + (pulse * 0.03f) + (collapseSurgeImpulse * 0.12f),
                    0.15f);
            }

            if (mainCamera != null)
            {
                var cameraOffset = new Vector3(
                    0f,
                    (breakImpulse * 0.05f) - (hitImpulse * 0.09f) + (collapseSurgeImpulse * 0.04f),
                    0f);
                mainCamera.transform.position = Vector3.Lerp(
                    mainCamera.transform.position,
                    baseCameraPosition + cameraOffset,
                    14f * Time.deltaTime);
                var p = mainCamera.transform.position;
                mainCamera.transform.position = new Vector3(0f, p.y, p.z);
            }
        }

        private void UpdateScrollPresentation(RunState state)
        {
            EnsureScrollTilePool();

            if (!scrollStateInitialized || state != lastScrollState)
            {
                if (state == RunState.RunReady)
                {
                    ResetScrollWorld();
                    Debug.Log("[SCROLL] Pool reset on RunReady");
                }
                else if (state == RunState.RunRestarting)
                {
                    ResetScrollWorld();
                }
                else if (state == RunState.RunActive)
                {
                    // TM-CORE-01: reset scroll distance counter on run start
                    _totalScrolledUnits = 0f;
                    Debug.Log("[SCROLL] RunActive entry — scrollSpeed=" + GetScrollSpeed() + " tileCount=" + scrollTiles.Count);
                }

                lastScrollState = state;
                scrollStateInitialized = true;
            }

            // TM-CORE-01: stop scroll when a block is in the player's lane
            var isScrollBlocked = bootstrap != null && bootstrap.IsScrollBlocked;
            if (state != RunState.RunActive || (bootstrap != null && bootstrap.IsPaused) || isScrollBlocked || scrollTiles.Count == 0)
            {
                return;
            }

            var scrollSpeed = GetScrollSpeed();
            var scrollDelta = Vector3.up * (scrollSpeed * Time.deltaTime);
            // TM-CORE-01: accumulate scroll for depth tracking
            _totalScrolledUnits += scrollSpeed * Time.deltaTime;
            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                var tile = scrollTiles[tileIndex];
                if (!tile.gameObject.activeSelf)
                {
                    continue;
                }

                tile.position += scrollDelta;
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log("[SCROLL-MOVE] moving " + tile.name + " y=" + tile.position.y);
                }
            }

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log("[SCROLL] tile[0].position.y = " + (scrollTiles.Count > 0 ? scrollTiles[0].position.y : 0f));
            }
            RecycleScrollTiles();
        }

        private void EnsureScrollTilePool()
        {
            if (runtimeRoot == null)
            {
                return;
            }

            var targetTileCount = GetScrollPoolSize();
            var countBefore = scrollTiles.Count;
            while (scrollTiles.Count < targetTileCount)
            {
                var tileIndex = scrollTiles.Count;
                var tileRoot = FindOrCreate($"ScrollTile_{tileIndex}", runtimeRoot.transform).transform;
                if (tileRoot.parent != runtimeRoot.transform)
                {
                    tileRoot.SetParent(runtimeRoot.transform, true);
                }
                BuildScrollTile(tileRoot);
                scrollTiles.Add(tileRoot);
            }

            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                if (scrollTiles[tileIndex].parent != runtimeRoot.transform)
                {
                    scrollTiles[tileIndex].SetParent(runtimeRoot.transform, true);
                }

                scrollTiles[tileIndex].gameObject.SetActive(tileIndex < targetTileCount);
            }

            if (scrollTiles.Count == 0)
            {
                return;
            }

            // Only realign when new tiles were just added — do NOT reset positions every frame
            // or accumulated scroll movement gets wiped out each Update.
            if (scrollTiles.Count > countBefore)
            {
                AlignScrollTiles();
            }

            if (!scrollTilesLogged)
            {
                for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
                {
                    if (!scrollTiles[tileIndex].gameObject.activeSelf)
                    {
                        continue;
                    }

                    Debug.Log("[SCROLL] Tile registered: " + scrollTiles[tileIndex].name);
                }

                scrollTilesLogged = true;
            }
        }

        private void ResetScrollWorld()
        {
            if (worldScrollRoot == null)
            {
                return;
            }

            worldScrollRoot.localPosition = Vector3.zero;
            AlignScrollTiles();
        }

        private void AlignScrollTiles()
        {
            var tileHeight = GetTileHeight();
            var targetTileCount = GetScrollPoolSize();
            var spawnOriginY = GetSpawnOriginY(tileHeight);
            var originX = runtimeRoot != null ? runtimeRoot.transform.position.x : 0f;
            var originZ = runtimeRoot != null ? runtimeRoot.transform.position.z : 0f;

            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                if (tileIndex >= targetTileCount)
                {
                    continue;
                }

                scrollTiles[tileIndex].position = new Vector3(
                    originX,
                    spawnOriginY + (tileIndex * tileHeight),
                    originZ);
            }
        }

        private void RecycleScrollTiles()
        {
            var tileHeight = GetTileHeight();
            var topEdge = (mainCamera != null ? mainCamera.transform.position.y + mainCamera.orthographicSize : 0f);
            var bottomEdge = (mainCamera != null ? mainCamera.transform.position.y - mainCamera.orthographicSize : 0f);

            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                var tile = scrollTiles[tileIndex];
                if (!tile.gameObject.activeSelf)
                {
                    continue;
                }

                if (tile.position.y > topEdge + tileHeight)
                {
                    var lowestY = GetLowestTileWorldY();
                    tile.position = new Vector3(tile.position.x, lowestY - tileHeight, tile.position.z);
                    Debug.Log("[SCROLL] Tile recycled -> " + tile.position.y);
                }
                else if (tile.position.y < bottomEdge - (tileHeight * 2f))
                {
                    var highestY = GetHighestTileWorldY();
                    tile.position = new Vector3(tile.position.x, highestY + tileHeight, tile.position.z);
                    Debug.Log("[SCROLL] Tile recycled -> " + tile.position.y);
                }
            }
        }

        private int GetScrollPoolSize()
        {
            if (scrollPoolSize > 0)
            {
                return scrollPoolSize;
            }

            var tileHeight = GetTileHeight();
            var screenHeightUnits = mainCamera != null ? mainCamera.orthographicSize * 2f : tileHeight;
            scrollPoolSize = Mathf.CeilToInt(screenHeightUnits / tileHeight) + 1;
            Debug.Log("[SCROLL] Pool size: " + scrollPoolSize);
            return scrollPoolSize;
        }

        private void GenerateRunLaneColors()
        {
            var palette = new[]
            {
                new Color(0.16f, 0.48f, 0.34f, 1f),
                new Color(0.28f, 0.40f, 0.58f, 1f),
                new Color(0.50f, 0.20f, 0.20f, 1f),
                new Color(0.53f, 0.42f, 0.15f, 1f),
                new Color(0.35f, 0.20f, 0.50f, 1f)
            };

            for (var index = 0; index < palette.Length; index += 1)
            {
                var swapIndex = Random.Range(index, palette.Length);
                (palette[index], palette[swapIndex]) = (palette[swapIndex], palette[index]);
            }

            for (var laneIndex = 0; laneIndex < runLaneColors.Length; laneIndex += 1)
            {
                runLaneColors[laneIndex] = palette[laneIndex];
            }
        }

        private void UpdateMarkerColorsIfNeeded(SegmentDescriptor segment)
        {
            if (markerRenderers == null)
            {
                return;
            }

            var newSegmentIndex = segment != null ? segment.SegmentIndex : -1;
            if (newSegmentIndex == lastMarkerSegmentIndex)
            {
                return;
            }

            lastMarkerSegmentIndex = newSegmentIndex;
            for (var laneIndex = 0; laneIndex < markerRenderers.Length; laneIndex += 1)
            {
                var markerColor = MarkerNeutralColor;
                if (segment != null)
                {
                    markerColor = segment.HazardLaneMask[laneIndex]
                        ? MarkerHazardColor
                        : segment.BreakableLaneMask[laneIndex]
                            ? MarkerBreakColor
                            : laneIndex == segment.SafeLaneIndex
                                ? MarkerSafeColor
                                : MarkerNeutralColor;
                }

                SetRendererColor(markerRenderers[laneIndex], markerColor);
            }
        }

        private float GetSpawnOriginY(float tileHeight)
        {
            if (mainCamera == null)
            {
                return -tileHeight;
            }

            return mainCamera.transform.position.y - mainCamera.orthographicSize - tileHeight;
        }

        private float GetLowestTileWorldY()
        {
            var lowestY = float.PositiveInfinity;

            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                if (!scrollTiles[tileIndex].gameObject.activeSelf)
                {
                    continue;
                }

                lowestY = Mathf.Min(lowestY, scrollTiles[tileIndex].position.y);
            }

            return float.IsPositiveInfinity(lowestY) ? 0f : lowestY;
        }

        private float GetHighestTileWorldY()
        {
            var highestY = float.NegativeInfinity;

            for (var tileIndex = 0; tileIndex < scrollTiles.Count; tileIndex += 1)
            {
                if (!scrollTiles[tileIndex].gameObject.activeSelf)
                {
                    continue;
                }

                highestY = Mathf.Max(highestY, scrollTiles[tileIndex].position.y);
            }

            return float.IsNegativeInfinity(highestY) ? 0f : highestY;
        }

        private void BuildScrollTile(Transform tileRoot)
        {
            CreateScrollBackdrop(tileRoot);
            CreateScrollMineFrame(tileRoot);
            CreateScrollLaneDecor(tileRoot);
        }

        private void CreateScrollBackdrop(Transform tileRoot)
        {
            var visibleHalfWidth = GetVisibleHalfWidth();

            var backdrop = CreatePrimitive("Backdrop", PrimitiveType.Quad, tileRoot);
            backdrop.transform.localPosition = new Vector3(0f, 0.4f, 6f);
            backdrop.transform.localScale = new Vector3(visibleHalfWidth * 1.98f, 10.8f, 1f);
            SetRendererColor(backdrop, new Color(0.08f, 0.1f, 0.13f, 1f));

            var tunnelGlow = CreatePrimitive("TunnelGlow", PrimitiveType.Quad, tileRoot);
            tunnelGlow.transform.localPosition = new Vector3(0f, -0.3f, 5.5f);
            tunnelGlow.transform.localScale = new Vector3(visibleHalfWidth * 1.75f, 8.9f, 1f);
            SetRendererColor(tunnelGlow, new Color(0.14f, 0.17f, 0.21f, 1f));

            var floor = CreatePrimitive("MineFloor", PrimitiveType.Cube, tileRoot);
            floor.transform.localPosition = new Vector3(0f, -4.3f, 1f);
            floor.transform.localScale = new Vector3(visibleHalfWidth * 2.30f, 1.25f, 1f);
            SetRendererColor(floor, new Color(0.17f, 0.14f, 0.12f, 1f));
        }

        private void CreateScrollMineFrame(Transform tileRoot)
        {
            var visibleHalfWidth = GetVisibleHalfWidth();

            var leftWall = CreatePrimitive("LeftWall", PrimitiveType.Cube, tileRoot);
            leftWall.transform.localPosition = new Vector3(-visibleHalfWidth, -0.1f, 0.8f);
            leftWall.transform.localScale = new Vector3(0.55f, 8.8f, 0.8f);
            SetRendererColor(leftWall, new Color(0.24f, 0.2f, 0.17f, 1f));

            var rightWall = CreatePrimitive("RightWall", PrimitiveType.Cube, tileRoot);
            rightWall.transform.localPosition = new Vector3(visibleHalfWidth, -0.1f, 0.8f);
            rightWall.transform.localScale = new Vector3(0.55f, 8.8f, 0.8f);
            SetRendererColor(rightWall, new Color(0.24f, 0.2f, 0.17f, 1f));

            var ceiling = CreatePrimitive("MineCeiling", PrimitiveType.Cube, tileRoot);
            ceiling.transform.localPosition = new Vector3(0f, 4.35f, 0.8f);
            ceiling.transform.localScale = new Vector3(visibleHalfWidth * 2.16f, 0.5f, 0.8f);
            SetRendererColor(ceiling, new Color(0.26f, 0.2f, 0.16f, 1f));
        }

        private void CreateScrollLaneDecor(Transform tileRoot)
        {
            var visibleHalfWidth = GetVisibleHalfWidth();
            var laneDecorRoot = FindOrCreate("LaneDecor", tileRoot).transform;

            for (var laneIndex = 0; laneIndex < 3; laneIndex += 1)
            {
                var lane = CreatePrimitive($"LaneDecor_{laneIndex}", PrimitiveType.Cube, laneDecorRoot);
                lane.transform.localPosition = new Vector3(GetLaneXPosition(visibleHalfWidth, laneIndex), -0.45f, -0.12f);
                lane.transform.localScale = new Vector3(visibleHalfWidth * 0.345f, 7.35f, 0.12f);
                SetRendererColor(lane, new Color(0.11f, 0.13f, 0.18f, 0.92f));
            }

            for (var dividerIndex = 0; dividerIndex < 2; dividerIndex += 1)
            {
                var divider = CreatePrimitive($"LaneDecorDivider_{dividerIndex}", PrimitiveType.Quad, laneDecorRoot);
                divider.transform.localPosition = new Vector3((dividerIndex == 0 ? -1f : 1f) * visibleHalfWidth * 0.23f, -0.45f, 0.45f);
                divider.transform.localScale = new Vector3(0.08f, 0.94f, 1f);
                SetRendererColor(divider, new Color(0.65f, 0.72f, 0.84f, 0.1f));
            }
        }

        private void UpdateHudPresentation()
        {
            // TM-CORE-01: depth driven by scroll distance, not segment count
            var depthValue = Mathf.FloorToInt(_totalScrolledUnits);
            var state = bootstrap.CurrentRunState;
            var isMenuVisible = state == RunState.RunReady || state == RunState.RunDeathResolved;

            if (state != RunState.RunDeathResolved)
            {
                lastVisibleDepth = depthValue;
                lastVisibleRunCoins = bootstrap.CurrentRunRewardResult.TotalRewardValue;
            }

            depthText.text = $"DEPTH {depthValue:000}";
            coinText.text = $"COINS {bootstrap.CurrentRunRewardResult.TotalRewardValue:000}";
            depthText.color = Color.Lerp(new Color(0.92f, 0.95f, 1f, 1f), new Color(1f, 0.93f, 0.7f, 1f), Mathf.Clamp01(depthValue / 60f));
            depthText.gameObject.SetActive(!isMenuVisible);
            coinText.gameObject.SetActive(!isMenuVisible);

            // TM-CORE-01: collapse bar driven by time, not segment count
            var collapseProgress = bootstrap.CollapseProximity;
            collapseFrameImage.gameObject.SetActive(!isMenuVisible);
            collapseFillImage.fillAmount = 0.08f + (collapseProgress * 0.92f);
            collapseFillImage.color = Color.Lerp(
                new Color(0.21f, 0.78f, 0.45f, 1f),
                new Color(0.95f, 0.29f, 0.23f, 1f),
                collapseProgress);
            collapseFillImage.rectTransform.localScale = Vector3.Lerp(
                collapseFillImage.rectTransform.localScale,
                new Vector3(1f + (collapseSurgeImpulse * 0.06f), 1f + (collapseSurgeImpulse * 0.1f), 1f),
                12f * Time.deltaTime);
            if (collapseFrameImage != null)
            {
                collapseFrameImage.color = Color.Lerp(
                    new Color(0.12f, 0.13f, 0.18f, 0.94f),
                    new Color(0.28f, 0.08f, 0.08f, 0.98f),
                    collapseProgress * 0.85f);
                collapseFrameImage.rectTransform.localScale = Vector3.Lerp(
                    collapseFrameImage.rectTransform.localScale,
                    new Vector3(1f + (collapseSurgeImpulse * 0.03f), 1f + (collapseSurgeImpulse * 0.05f), 1f),
                    12f * Time.deltaTime);
            }

            switch (state)
            {
                case RunState.RunReady:
                    promptText.gameObject.SetActive(false);
                    break;
                case RunState.RunActive:
                    promptText.gameObject.SetActive(true);
                    // TM-CORE-01: tap drills blocks; world auto-scrolls
                    promptText.text = bootstrap.IsScrollBlocked
                        ? "BLOCK  •  TAP TO DRILL"
                        : "SWIPE TO SHIFT LANE";
                    promptText.color = Color.Lerp(
                        new Color(0.8f, 0.89f, 1f, 0.76f),
                        new Color(1f, 0.85f, 0.4f, 0.92f),
                        collapseProgress * 0.45f);
                    break;
                default:
                    promptText.gameObject.SetActive(false);
                    break;
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(state == RunState.RunActive && (bootstrap == null || !bootstrap.IsPaused));
            }

            UpdateFeedbackReadability();
            UpdateHudPunch();

            feedbackText.gameObject.SetActive(!isMenuVisible);

            if (feedbackText != null && state == RunState.RunReady && !bootstrap.IsFeedbackActive)
            {
                feedbackText.text = "READY";
                feedbackText.color = new Color(0.9f, 0.95f, 1f, 1f);
            }

            if (hitFlashImage != null)
            {
                hitFlashImage.color = new Color(1f, 0.22f, 0.18f, hitFlashAlpha);
            }
        }

        private void UpdatePausePresentation()
        {
            if (bootstrap == null)
            {
                return;
            }

            var shouldShowOverlay = bootstrap.CurrentRunState == RunState.RunActive && bootstrap.IsPaused;
            if (pauseOverlay != null && pauseOverlay.activeSelf != shouldShowOverlay)
            {
                pauseOverlay.SetActive(shouldShowOverlay);
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(bootstrap.CurrentRunState == RunState.RunActive && !bootstrap.IsPaused);
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = "II";
            }

            UpdatePauseToggleLabels();
        }

        private void UpdatePauseToggleLabels()
        {
            if (pauseConfig == null)
            {
                return;
            }

            if (pauseSoundLabel != null)
            {
                pauseSoundLabel.text = pauseConfig.soundEnabled ? "SOUND: ON" : "SOUND: OFF";
            }

            if (pauseVibrationLabel != null)
            {
                pauseVibrationLabel.text = pauseConfig.vibrationEnabled ? "VIBRATION: ON" : "VIBRATION: OFF";
            }
        }

        private void UpdateFeedbackReadability()
        {
            if (feedbackText == null)
            {
                return;
            }

            var currentMessage = bootstrap.CurrentFeedbackText;
            if (currentMessage != lastFeedbackMessage)
            {
                lastFeedbackMessage = currentMessage;
                feedbackEmphasisTimer = 0.18f;
            }

            feedbackEmphasisTimer = Mathf.Max(0f, feedbackEmphasisTimer - Time.deltaTime);

            var targetScale = Vector3.one;
            var targetColor = feedbackText.color;

            if (currentMessage.StartsWith("SHIFT"))
            {
                targetScale = Vector3.one * 1.14f;
                targetColor = new Color(0.56f, 0.88f, 1f, 1f);
            }
            else if (currentMessage.StartsWith("BREAK"))
            {
                targetScale = Vector3.one * 1.18f;
                targetColor = new Color(1f, 0.87f, 0.32f, 1f);
            }
            else if (currentMessage.StartsWith("HIT"))
            {
                targetScale = Vector3.one * 1.22f;
                targetColor = new Color(1f, 0.4f, 0.35f, 1f);
            }
            else if (currentMessage == "READY")
            {
                targetScale = Vector3.one;
                targetColor = new Color(0.88f, 0.95f, 1f, 1f);
            }

            if (feedbackEmphasisTimer > 0f)
            {
                var punch = 1f + (feedbackEmphasisTimer * 0.35f);
                feedbackText.rectTransform.localScale = Vector3.one * punch;
            }
            else
            {
                feedbackText.rectTransform.localScale = Vector3.Lerp(
                    feedbackText.rectTransform.localScale,
                    targetScale,
                    12f * Time.deltaTime);
            }

            feedbackText.color = Color.Lerp(feedbackText.color, targetColor, 12f * Time.deltaTime);
        }

        private void UpdateHudPunch()
        {
            depthText.rectTransform.localScale = Vector3.Lerp(
                depthText.rectTransform.localScale,
                Vector3.one * (1f + (breakImpulse * 0.02f)),
                12f * Time.deltaTime);

            coinText.rectTransform.localScale = Vector3.Lerp(
                coinText.rectTransform.localScale,
                Vector3.one * (1f + (lootImpulse * 0.18f)),
                12f * Time.deltaTime);

            promptText.rectTransform.localScale = Vector3.Lerp(
                promptText.rectTransform.localScale,
                Vector3.one * (1f + (collapseSurgeImpulse * 0.03f)),
                10f * Time.deltaTime);
        }

        private void UpdateResultsPresentation()
        {
            var state = bootstrap.CurrentRunState;

            // BUG-FIX TM-HOTFIX-14: AppBootstrap.HandleStateChanged fires RequestStartRun()
            // inside TryCompleteRestart()'s StateChanged.Invoke(), where isTransitionInProgress
            // is still true — so TryStartRun() returns false and the run never auto-starts.
            // We catch the RunRestarting→RunReady transition here (Update, lock already clear)
            // and re-fire RequestMenuPlay() ourselves so the player skips the menu after death.
            if (_prevResultsPresentationState == RunState.RunRestarting && state == RunState.RunReady)
            {
                _prevResultsPresentationState = state;
                bootstrap.RequestMenuPlay();
                return;
            }
            _prevResultsPresentationState = state;

            // showMenu and showResults are MUTUALLY EXCLUSIVE — they can never both be true.
            var showMenu = debugPreviewResultsOverlay || state == RunState.RunReady;
            var showResults = debugPreviewResultsOverlay || state == RunState.RunDeathResolved;
            var isVisible = showMenu || showResults;
            if (state == RunState.RunActive)
            {
                isUpgradePanelOpen = false;
            }

            if (showResults && lastVisibleDepth > bestVisibleDepth)
            {
                bestVisibleDepth = lastVisibleDepth;
                PlayerPrefs.SetInt(BestDepthPlayerPrefsKey, bestVisibleDepth);
                PlayerPrefs.Save();
            }

            resultsTransition = Mathf.MoveTowards(resultsTransition, isVisible ? 1f : 0f, Time.deltaTime * 5f);

            if (resultsOverlay.activeSelf != isVisible)
            {
                resultsOverlay.SetActive(isVisible);
                if (isVisible && showMenu)
                {
                    Debug.Log("[MENU] SetActive(true) from: RunReady visibility gate");
                }
                if (showMenu || !isVisible)
                {
                    Debug.Log($"[MENU] SetActive({isVisible}) | state={state}"); // TM-BUILD-13-TEMP
                }
            }

            if (!isVisible)
            {
                if (resultsOverlayImage != null)
                {
                    resultsOverlayImage.color = new Color(0.04f, 0.05f, 0.08f, 0f);
                }

                return;
            }

            var pulse = (Mathf.Sin(Time.time * 4.2f) + 1f) * 0.5f;
            var shouldShowUpgradePanel = isUpgradePanelOpen;
            menuTitleText.gameObject.SetActive(showMenu && !shouldShowUpgradePanel);
            menuSubtitleText.gameObject.SetActive(showMenu && !shouldShowUpgradePanel);
            menuBestDepthText.gameObject.SetActive(showMenu && !shouldShowUpgradePanel);
            menuBestDepthText.text = $"BEST: {bestVisibleDepth:000}";
            resultsDepthText.text = $"DEPTH REACHED {lastVisibleDepth:000}";
            resultsCoinsText.text = $"RUN COINS +{lastVisibleRunCoins}";
            resultsSubtitleText.text = lastVisibleRunCoins > 0 ? "BANKED THIS RUN" : "NO BANKED LOOT";
            resultsHintText.text = showResults ? "HIT PLAY TO DROP BACK IN" : "TAP PLAY TO START";
            resultsSubtitleText.gameObject.SetActive(showResults && !shouldShowUpgradePanel);
            resultsDepthText.gameObject.SetActive(showResults && !shouldShowUpgradePanel);
            resultsCoinsText.gameObject.SetActive(showResults && !shouldShowUpgradePanel);
            resultsHintText.gameObject.SetActive(showResults && !shouldShowUpgradePanel);
            upgradesButton.gameObject.SetActive(showMenu && !shouldShowUpgradePanel);
            restartButton.gameObject.SetActive(!shouldShowUpgradePanel);
            upgradePanel.SetActive(shouldShowUpgradePanel);

            if (resultsOverlayImage != null)
            {
                resultsOverlayImage.color = new Color(0.03f, 0.04f, 0.07f, 0.82f * resultsTransition);
            }

            if (resultsPanelImage != null)
            {
                resultsPanelImage.color = Color.Lerp(
                    new Color(0.1f, 0.11f, 0.16f, 0.96f),
                    new Color(0.16f, 0.12f, 0.08f, 0.98f),
                    0.18f + (pulse * 0.08f));
                resultsPanelImage.rectTransform.localScale = Vector3.Lerp(
                    resultsPanelImage.rectTransform.localScale,
                    Vector3.one * (0.96f + (resultsTransition * 0.04f)),
                    14f * Time.deltaTime);
            }

            resultsDepthText.color = Color.Lerp(
                new Color(0.92f, 0.95f, 1f, 1f),
                new Color(1f, 0.95f, 0.78f, 1f),
                Mathf.Clamp01(lastVisibleDepth / 60f));
            resultsCoinsText.color = lastVisibleRunCoins > 0
                ? new Color(1f, 0.85f, 0.26f, 1f)
                : new Color(0.74f, 0.78f, 0.86f, 1f);

            if (resultsRestartLabelText != null)
            {
                resultsRestartLabelText.text = "PLAY";
                resultsRestartLabelText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            }

            var restartImage = restartButton.GetComponent<Image>();
            if (restartImage != null)
            {
                restartImage.color = Color.Lerp(
                    new Color(0.96f, 0.74f, 0.14f, 1f),
                    new Color(1f, 0.88f, 0.28f, 1f),
                    pulse);
            }

            restartButton.transform.localScale = Vector3.Lerp(
                restartButton.transform.localScale,
                Vector3.one * (1f + (pulse * 0.035f)),
                10f * Time.deltaTime);

            if (menuTitleText != null)
            {
                menuTitleText.color = Color.Lerp(
                    new Color(1f, 0.88f, 0.42f, 1f),
                    new Color(1f, 0.95f, 0.66f, 1f),
                    pulse * 0.45f);
            }
        }

        private void DetectPresentationEvents()
        {
            if (bootstrap.CurrentCommittedLaneIndex != lastCommittedLaneIndex)
            {
                laneShiftImpulse = 1f;
                lastCommittedLaneIndex = bootstrap.CurrentCommittedLaneIndex;
            }

            RunRewardResult runRewardResult;
            int currentRunHealth;
            try
            {
                runRewardResult = bootstrap.CurrentRunRewardResult;
                currentRunHealth = bootstrap.CurrentRunHealth;
            }
            catch (System.NullReferenceException)
            {
                return;
            }

            if (runRewardResult == null)
            {
                return;
            }

            if (runRewardResult.TotalRewardValue > lastRunRewardValue)
            {
                var rewardGain = runRewardResult.TotalRewardValue - lastRunRewardValue;
                breakImpulse = Mathf.Max(breakImpulse, 0.8f);
                lootImpulse = Mathf.Max(lootImpulse, Mathf.Clamp01(0.55f + (rewardGain * 0.05f)));
                lastRunRewardValue = runRewardResult.TotalRewardValue;
            }
            else if (runRewardResult.TotalRewardValue < lastRunRewardValue)
            {
                lastRunRewardValue = runRewardResult.TotalRewardValue;
            }

            if (currentRunHealth < lastRunHealth)
            {
                hitImpulse = 1f;
                hitFlashAlpha = 0.38f;
            }

            lastRunHealth = currentRunHealth;

            // TM-CORE-01: surge impulse from time-driven collapse proximity
            var collapseProgress = bootstrap.CollapseProximity;
            if (collapseProgress > 0.55f)
            {
                collapseSurgeImpulse = Mathf.Max(collapseSurgeImpulse, (collapseProgress - 0.55f) / 0.45f);
            }
        }

        private void TickPresentationImpulses()
        {
            laneShiftImpulse = Mathf.MoveTowards(laneShiftImpulse, 0f, Time.deltaTime * 6f);
            breakImpulse = Mathf.MoveTowards(breakImpulse, 0f, Time.deltaTime * 4.5f);
            lootImpulse = Mathf.MoveTowards(lootImpulse, 0f, Time.deltaTime * 3.5f);
            hitImpulse = Mathf.MoveTowards(hitImpulse, 0f, Time.deltaTime * 5.5f);
            collapseSurgeImpulse = Mathf.MoveTowards(collapseSurgeImpulse, 0f, Time.deltaTime * 1.7f);
            hitFlashAlpha = Mathf.MoveTowards(hitFlashAlpha, 0f, Time.deltaTime * 2.8f);
        }

        private static GameObject FindOrCreate(string objectName, Transform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(objectName);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static GameObject FindOrCreateUiObject(string objectName, Transform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = new GameObject(objectName, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return created;
        }

        private GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = GameObject.CreatePrimitive(primitiveType);
            var col = created.GetComponent<Collider>();
            if (col != null)
            {
                Object.DestroyImmediate(col);
            }
            var renderer = created.GetComponent<Renderer>();
            if (renderer != null && _urpLitBaseMaterial != null)
            {
                renderer.material = new Material(_urpLitBaseMaterial);
            }
            created.name = objectName;
            created.transform.SetParent(parent, false);
            return created;
        }

        private static void SetRendererColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                SetRendererColor(renderer, color);
            }
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            var material = renderer.material;
            if (material == null)
            {
                return;
            }

            if (material.shader == null || !material.shader.name.Contains("Unlit"))
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    material.shader = shader;
                }
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }
        }

        private static Text EnsureText(string objectName, Transform parent, string textValue, int fontSize, TextAnchor anchor, Color color)
        {
            var textObject = FindOrCreate(objectName, parent);
            var rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            var text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = textValue;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Image EnsureImage(string objectName, Transform parent, Color color)
        {
            var imageObject = FindOrCreate(objectName, parent);
            var rect = imageObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = imageObject.AddComponent<RectTransform>();
            }

            var image = imageObject.GetComponent<Image>();
            if (image == null)
            {
                image = imageObject.AddComponent<Image>();
            }

            image.color = color;
            return image;
        }

        private static Image EnsureImageComponent(GameObject gameObject, Color color)
        {
            var rect = gameObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = gameObject.AddComponent<RectTransform>();
            }

            var image = gameObject.GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
            }

            image.color = color;
            return image;
        }

        private static Button EnsureButton(string objectName, Transform parent, string label)
        {
            var buttonObject = FindOrCreate(objectName, parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            var image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            image.color = new Color(0.97f, 0.8f, 0.18f, 1f);

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(1f, 0.88f, 0.38f, 1f);
            colors.pressedColor = new Color(0.86f, 0.63f, 0.08f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var labelText = EnsureText("Label", buttonObject.transform, label, 44, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.12f, 1f));
            StretchRect(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static TextMeshProUGUI EnsureTmpText(string objectName, Transform parent, string textValue, float fontSize, TextAlignmentOptions alignment, FontStyles fontStyle, Color color)
        {
            var textObject = FindOrCreate(objectName, parent);
            var rect = textObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = textObject.AddComponent<RectTransform>();
            }

            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = textObject.AddComponent<TextMeshProUGUI>();
            }

            text.text = textValue;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = fontStyle;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static Button EnsureTmpButton(string objectName, Transform parent, string label, float fontSize, Color normalColor, Color labelColor)
        {
            var buttonObject = FindOrCreate(objectName, parent);
            var rect = buttonObject.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = buttonObject.AddComponent<RectTransform>();
            }

            var image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            image.color = normalColor;
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            var labelText = EnsureTmpText("Label", buttonObject.transform, label, fontSize, TextAlignmentOptions.Center, FontStyles.Bold, labelColor);
            StretchRect(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void ConfigureButtonColors(Button button, Color normalColor, Color highlightedColor, Color pressedColor, Color labelColor)
        {
            if (button == null)
            {
                return;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = normalColor;
            }

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = highlightedColor;
            button.colors = colors;

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.color = labelColor;
            }
        }

        private static void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
        }

        private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
        }

        private float GetVisibleHalfWidth()
        {
            return mainCamera != null
                ? mainCamera.orthographicSize * mainCamera.aspect
                : 4.35f;
        }

        private float GetScrollSpeed()
        {
            return scrollConfig != null
                ? Mathf.Max(0f, scrollConfig.scrollSpeedUnitsPerSec)
                : 3f;
        }

        private float GetTileHeight()
        {
            return scrollConfig != null
                ? Mathf.Max(1f, scrollConfig.tileHeightUnits)
                : 4f;
        }

        private int GetVisibleBuffer()
        {
            return scrollConfig != null
                ? Mathf.Max(0, scrollConfig.visibleTileBuffer)
                : 2;
        }

        private int GetVisibleTileCount()
        {
            var tileHeight = GetTileHeight();
            var screenHeight = mainCamera != null
                ? mainCamera.orthographicSize * 2f
                : 9.7f;
            return Mathf.Max(3, Mathf.CeilToInt(screenHeight / tileHeight) + (GetVisibleBuffer() * 2));
        }

        private static float GetLaneXPosition(float visibleHalfWidth, int laneIndex)
        {
            return laneIndex switch
            {
                0 => -visibleHalfWidth * 0.46f,
                1 => 0f,
                2 => visibleHalfWidth * 0.46f,
                _ => 0f
            };
        }
    }
}
