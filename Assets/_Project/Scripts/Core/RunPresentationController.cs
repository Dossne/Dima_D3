using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TapMiner.Core
{
    /// <summary>
    /// Builds a minimal runtime presentation layer over the existing vertical-slice systems.
    /// </summary>
    public sealed class RunPresentationController : MonoBehaviour
    {
        private const float LaneSpacing = 2f;
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

        private AppBootstrap bootstrap;
        private Camera mainCamera;

        private GameObject runtimeRoot;
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
        private GameObject resultsOverlay;
        private Text resultsDepthText;
        private Text resultsCoinsText;
        private Button restartButton;
        private Text feedbackText;

        private int lastVisibleDepth;
        private int lastVisibleRunCoins;
        private string lastFeedbackMessage = string.Empty;
        private float feedbackEmphasisTimer;

        private void Awake()
        {
            bootstrap = GetComponent<AppBootstrap>();
            mainCamera = Camera.main;
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

            UpdateWorldPresentation();
            UpdateHudPresentation();
            UpdateResultsPresentation();
        }

        public void OnRestartPressed()
        {
            if (bootstrap == null)
            {
                return;
            }

            bootstrap.RequestRestartRun();
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
        }

        private void EnsureWorldPresentation()
        {
            runtimeRoot = FindOrCreate("PresentationRuntime", transform);
            laneRoot = FindOrCreate("LanePresentation", runtimeRoot.transform).transform;
            markerRoot = FindOrCreate("CurrentSegmentMarkers", runtimeRoot.transform).transform;
            collapseRoot = FindOrCreate("CollapsePresentation", runtimeRoot.transform).transform;

            CreateBackdrop();
            CreateMineFrame();
            CreateLaneGuides();
            CreateSegmentMarkers();
            CreateCollapseCeiling();
            CreatePlayerVisual();
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
            StretchRect(collapseBar.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.895f), Vector2.zero, Vector2.zero);
            collapseFrameImage = collapseBar;

            collapseFillImage = EnsureImage(
                "CollapseBarFill",
                collapseBar.transform,
                new Color(0.25f, 0.82f, 0.46f, 0.95f));
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

            resultsOverlay = FindOrCreate("ResultsOverlay", canvasObject.transform);
            var resultsImage = EnsureImageComponent(resultsOverlay, new Color(0.04f, 0.05f, 0.08f, 0.9f));
            StretchRect(resultsImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var resultsPanel = EnsureImage(
                "ResultsPanel",
                resultsOverlay.transform,
                new Color(0.12f, 0.13f, 0.18f, 0.96f));
            StretchRect(resultsPanel.rectTransform, new Vector2(0.14f, 0.23f), new Vector2(0.86f, 0.74f), Vector2.zero, Vector2.zero);

            var resultsTitle = EnsureText(
                "ResultsTitle",
                resultsPanel.transform,
                "RUN OVER",
                62,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.9f, 0.48f, 1f));
            StretchRect(resultsTitle.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.88f), Vector2.zero, Vector2.zero);

            resultsDepthText = EnsureText(
                "ResultsDepthText",
                resultsPanel.transform,
                "DEPTH REACHED 000",
                46,
                TextAnchor.MiddleCenter,
                Color.white);
            StretchRect(resultsDepthText.rectTransform, new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.66f), Vector2.zero, Vector2.zero);

            resultsCoinsText = EnsureText(
                "ResultsCoinsText",
                resultsPanel.transform,
                "RUN COINS +0",
                42,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.26f, 1f));
            StretchRect(resultsCoinsText.rectTransform, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.5f), Vector2.zero, Vector2.zero);

            restartButton = EnsureButton("RestartButton", resultsPanel.transform, "RESTART");
            StretchRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.28f), Vector2.zero, Vector2.zero);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartPressed);

            resultsOverlay.SetActive(false);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(canvas.transform.parent, false);
        }

        private void CreateBackdrop()
        {
            var backdrop = CreatePrimitive("Backdrop", PrimitiveType.Quad, runtimeRoot.transform);
            backdrop.transform.localPosition = new Vector3(0f, 0.4f, 6f);
            backdrop.transform.localScale = new Vector3(8.6f, 10.8f, 1f);
            SetRendererColor(backdrop, new Color(0.08f, 0.1f, 0.13f, 1f));

            var tunnelGlow = CreatePrimitive("TunnelGlow", PrimitiveType.Quad, runtimeRoot.transform);
            tunnelGlow.transform.localPosition = new Vector3(0f, -0.3f, 5.5f);
            tunnelGlow.transform.localScale = new Vector3(7.6f, 8.9f, 1f);
            SetRendererColor(tunnelGlow, new Color(0.14f, 0.17f, 0.21f, 1f));

            var floor = CreatePrimitive("MineFloor", PrimitiveType.Cube, runtimeRoot.transform);
            floor.transform.localPosition = new Vector3(0f, -4.3f, 1f);
            floor.transform.localScale = new Vector3(10f, 1.25f, 1f);
            SetRendererColor(floor, new Color(0.17f, 0.14f, 0.12f, 1f));
        }

        private void CreateMineFrame()
        {
            var leftWall = CreatePrimitive("LeftWall", PrimitiveType.Cube, runtimeRoot.transform);
            leftWall.transform.localPosition = new Vector3(-4.35f, -0.1f, 0.8f);
            leftWall.transform.localScale = new Vector3(0.55f, 8.8f, 0.8f);
            SetRendererColor(leftWall, new Color(0.24f, 0.2f, 0.17f, 1f));

            var rightWall = CreatePrimitive("RightWall", PrimitiveType.Cube, runtimeRoot.transform);
            rightWall.transform.localPosition = new Vector3(4.35f, -0.1f, 0.8f);
            rightWall.transform.localScale = new Vector3(0.55f, 8.8f, 0.8f);
            SetRendererColor(rightWall, new Color(0.24f, 0.2f, 0.17f, 1f));

            var ceiling = CreatePrimitive("MineCeiling", PrimitiveType.Cube, runtimeRoot.transform);
            ceiling.transform.localPosition = new Vector3(0f, 4.35f, 0.8f);
            ceiling.transform.localScale = new Vector3(9.4f, 0.5f, 0.8f);
            SetRendererColor(ceiling, new Color(0.26f, 0.2f, 0.16f, 1f));
        }

        private void CreateLaneGuides()
        {
            laneRenderers = new Renderer[3];
            laneGlowRenderers = new Renderer[3];
            laneDividerRenderers = new Renderer[2];

            for (var laneIndex = 0; laneIndex < 3; laneIndex += 1)
            {
                var lane = CreatePrimitive($"Lane_{laneIndex}", PrimitiveType.Cube, laneRoot);
                lane.transform.localPosition = new Vector3((laneIndex - 1) * LaneSpacing, -0.45f, 0f);
                lane.transform.localScale = new Vector3(1.5f, 7.35f, 0.15f);
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
                divider.transform.localPosition = new Vector3((dividerIndex == 0 ? -1f : 1f) * LaneSpacing * 0.5f, -0.45f, 0.55f);
                divider.transform.localScale = new Vector3(0.12f, 0.94f, 1f);
                laneDividerRenderers[dividerIndex] = divider.GetComponent<Renderer>();
                SetRendererColor(divider, new Color(0.82f, 0.88f, 1f, 0.16f));
            }
        }

        private void CreateSegmentMarkers()
        {
            markerRenderers = new Renderer[3];
            rewardRenderers = new Renderer[3];

            for (var laneIndex = 0; laneIndex < 3; laneIndex += 1)
            {
                var marker = CreatePrimitive($"SegmentMarker_{laneIndex}", PrimitiveType.Cube, markerRoot);
                marker.transform.localPosition = new Vector3((laneIndex - 1) * LaneSpacing, 1.65f, -0.2f);
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
            collapseRenderers = new Renderer[2];

            var band = CreatePrimitive("CollapseBand", PrimitiveType.Cube, collapseRoot);
            band.transform.localPosition = new Vector3(0f, 3.35f, 0.2f);
            band.transform.localScale = new Vector3(8.8f, 0.62f, 0.4f);
            collapseRenderers[0] = band.GetComponent<Renderer>();

            var glow = CreatePrimitive("CollapseGlow", PrimitiveType.Cube, collapseRoot);
            glow.transform.localPosition = new Vector3(0f, 3.02f, 0.4f);
            glow.transform.localScale = new Vector3(8.2f, 0.2f, 0.15f);
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
            var state = bootstrap.CurrentRunState;
            var segment = bootstrap.GetCurrentSegmentDescriptor();
            var completedSegments = bootstrap.CurrentCompletedSegmentCount;
            var collapseProgress = segment == null
                ? 0f
                : Mathf.Clamp01((completedSegments + 1f) / Mathf.Max(1f, bootstrap.CurrentSpawnedSegmentCount));
            var pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f;

            for (var laneIndex = 0; laneIndex < laneRenderers.Length; laneIndex += 1)
            {
                var laneColor = LaneBaseColor;
                var laneGlowColor = new Color(0.12f, 0.15f, 0.2f, 0.2f);

                if (segment != null)
                {
                    if (segment.HazardLaneMask[laneIndex])
                    {
                        laneColor = Color.Lerp(LaneBaseColor, LaneDangerColor, 0.52f);
                        laneGlowColor = new Color(0.95f, 0.25f, 0.22f, 0.22f + (pulse * 0.18f));
                    }
                    else if (segment.HasRewardPath && laneIndex == segment.RewardLaneIndex)
                    {
                        laneColor = Color.Lerp(LaneBaseColor, LaneRewardColor, 0.72f);
                        laneGlowColor = new Color(1f, 0.86f, 0.28f, 0.18f + (pulse * 0.14f));
                    }
                    else if (laneIndex == segment.SafeLaneIndex)
                    {
                        laneColor = LaneSafeColor;
                        laneGlowColor = new Color(0.27f, 0.95f, 0.62f, 0.16f + (pulse * 0.12f));
                    }
                }

                if (laneIndex == bootstrap.CurrentCommittedLaneIndex)
                {
                    laneColor = Color.Lerp(laneColor, LaneCurrentColor, 0.58f);
                    laneGlowColor.a = Mathf.Max(laneGlowColor.a, 0.24f + (pulse * 0.12f));
                }

                SetRendererColor(laneRenderers[laneIndex], laneColor);
                SetRendererColor(laneGlowRenderers[laneIndex], laneGlowColor);
                laneGlowRenderers[laneIndex].transform.localScale = new Vector3(
                    1.25f + (laneIndex == bootstrap.CurrentCommittedLaneIndex ? 0.06f : 0f),
                    1.05f + (pulse * 0.03f),
                    1f);

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
                markerRenderers[laneIndex].transform.localScale = new Vector3(
                    laneIndex == bootstrap.CurrentCommittedLaneIndex ? 1.12f : 1.02f,
                    1.18f + Mathf.Sin(Time.time * 2.2f + laneIndex) * 0.1f,
                    0.65f);

                var hasReward = segment != null && segment.HasRewardPath && laneIndex == segment.RewardLaneIndex;
                rewardRenderers[laneIndex].gameObject.SetActive(hasReward);
                if (hasReward)
                {
                    SetRendererColor(rewardRenderers[laneIndex], new Color(1f, 0.82f, 0.22f, 1f));
                    rewardRenderers[laneIndex].transform.localScale = Vector3.one * (0.52f + (pulse * 0.07f));
                }
            }

            for (var dividerIndex = 0; dividerIndex < laneDividerRenderers.Length; dividerIndex += 1)
            {
                var dividerAlpha = 0.18f + (collapseProgress * 0.08f);
                SetRendererColor(laneDividerRenderers[dividerIndex], new Color(0.87f, 0.91f, 1f, dividerAlpha));
            }

            playerVisualRoot.localPosition = new Vector3(
                (bootstrap.CurrentCommittedLaneIndex - 1) * LaneSpacing,
                -1.95f + Mathf.Sin(Time.time * 6.2f) * 0.03f,
                -0.3f);

            if (playerShadowRenderer != null)
            {
                SetRendererColor(playerShadowRenderer, new Color(0f, 0f, 0f, 0.35f));
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
            }

            if (collapseRenderers != null && collapseRenderers.Length == 2)
            {
                collapseRoot.localPosition = new Vector3(0f, -collapseProgress * 1.1f, 0f);
                SetRendererColor(collapseRenderers[0], Color.Lerp(
                    new Color(0.42f, 0.3f, 0.12f, 1f),
                    new Color(0.88f, 0.25f, 0.22f, 1f),
                    collapseProgress));
                SetRendererColor(collapseRenderers[1], Color.Lerp(
                    new Color(0.92f, 0.72f, 0.2f, 1f),
                    new Color(1f, 0.34f, 0.26f, 1f),
                    collapseProgress));
                collapseRenderers[1].transform.localScale = new Vector3(
                    8.2f,
                    0.2f + (collapseProgress * 0.12f) + (pulse * 0.03f),
                    0.15f);
            }
        }

        private void UpdateHudPresentation()
        {
            var depthValue = bootstrap.CurrentCompletedSegmentCount * 5;
            var state = bootstrap.CurrentRunState;

            if (state != RunState.RunDeathResolved)
            {
                lastVisibleDepth = depthValue;
                lastVisibleRunCoins = bootstrap.CurrentRunRewardResult.TotalRewardValue;
            }

            depthText.text = $"DEPTH {depthValue:000}";
            coinText.text = $"COINS {bootstrap.SoftCurrencyBalance:000}";
            depthText.color = Color.Lerp(new Color(0.92f, 0.95f, 1f, 1f), new Color(1f, 0.93f, 0.7f, 1f), Mathf.Clamp01(depthValue / 60f));

            var collapseProgress = Mathf.Clamp01((bootstrap.CurrentCompletedSegmentCount + 1f) / Mathf.Max(1f, bootstrap.CurrentSpawnedSegmentCount));
            collapseFillImage.fillAmount = 0.08f + (collapseProgress * 0.92f);
            collapseFillImage.color = Color.Lerp(
                new Color(0.21f, 0.78f, 0.45f, 1f),
                new Color(0.95f, 0.29f, 0.23f, 1f),
                collapseProgress);
            if (collapseFrameImage != null)
            {
                collapseFrameImage.color = Color.Lerp(
                    new Color(0.12f, 0.13f, 0.18f, 0.94f),
                    new Color(0.28f, 0.08f, 0.08f, 0.98f),
                    collapseProgress * 0.85f);
            }

            switch (state)
            {
                case RunState.RunReady:
                    promptText.gameObject.SetActive(true);
                    promptText.text = "TAP TO START";
                    promptText.color = new Color(0.82f, 0.91f, 1f, 0.84f);
                    break;
                case RunState.RunActive:
                    promptText.gameObject.SetActive(true);
                    promptText.text = "SWIPE TO SHIFT  •  TAP TO DESCEND";
                    promptText.color = Color.Lerp(
                        new Color(0.8f, 0.89f, 1f, 0.76f),
                        new Color(1f, 0.85f, 0.4f, 0.92f),
                        collapseProgress * 0.45f);
                    break;
                default:
                    promptText.gameObject.SetActive(false);
                    break;
            }

            UpdateFeedbackReadability();

            if (feedbackText != null && state == RunState.RunReady && !bootstrap.IsFeedbackActive)
            {
                feedbackText.text = "READY";
                feedbackText.color = new Color(0.9f, 0.95f, 1f, 1f);
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

        private void UpdateResultsPresentation()
        {
            var isVisible = debugPreviewResultsOverlay || bootstrap.CurrentRunState == RunState.RunDeathResolved;
            if (resultsOverlay.activeSelf != isVisible)
            {
                resultsOverlay.SetActive(isVisible);
            }

            if (!isVisible)
            {
                return;
            }

            resultsDepthText.text = $"DEPTH REACHED {lastVisibleDepth:000}";
            resultsCoinsText.text = $"RUN COINS +{lastVisibleRunCoins}";
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

        private static GameObject CreatePrimitive(string objectName, PrimitiveType primitiveType, Transform parent)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var created = GameObject.CreatePrimitive(primitiveType);
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

            var labelText = EnsureText("Label", buttonObject.transform, label, 42, TextAnchor.MiddleCenter, new Color(0.12f, 0.12f, 0.12f, 1f));
            StretchRect(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void StretchRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.localScale = Vector3.one;
        }
    }
}
