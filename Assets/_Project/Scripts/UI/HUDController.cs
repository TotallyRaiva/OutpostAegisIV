using System.Collections.Generic;
using OutpostAegis.Abilities;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Map;
using OutpostAegis.Towers;
using OutpostAegis.Visuals;
using OutpostAegis.Waves;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OutpostAegis.UI
{
    /// <summary>
    /// Grimdark Warhammer 40k Servo-Skull / Cogitator CRT UGUI Canvas & Radial Touch Menu.
    /// Automatically builds its responsive 1920x1080 Canvas hierarchy and tracks tapped BuildNodes
    /// with a rotating Auspex Holo-Ring and octagonal radial action buttons.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private Font _uiFont;

        // Top Auspex Header UI references
        private Image _reactorFillBar;
        private Text _reactorText;
        private Text _requisitionText;
        private Text _commandPointsText;
        private RectTransform _servoSkullIconRect;
        private Text _waveHeaderText;
        private Text _voxTickerText;
        private Button _launchWaveButton;
        private Text _launchWaveButtonText;
        private Button _speedButton;
        private Text _speedButtonText;

        // Bottom Command Ability Deck
        private readonly List<Button> _abilityButtons = new List<Button>();
        private readonly List<Text> _abilityButtonLabels = new List<Text>();
        private GameObject _cancelTargetingBanner;
        private Text _cancelTargetingText;

        // World-anchored Radial Node Menu
        private BuildNode _selectedNode;
        private GameObject _radialMenuRoot;
        private RectTransform _radialMenuRect;
        private RectTransform _holoRingRect;
        private Text _radialNodeTitleText;
        private Text _radialCogitatorStatsText;

        // Empty Node: 5 Radial Build Buttons
        private GameObject _emptyNodeSlotsGroup;
        private readonly List<Button> _buildSlotButtons = new List<Button>();
        private readonly List<Image> _buildSlotIcons = new List<Image>();
        private readonly List<Text> _buildSlotLabels = new List<Text>();

        // Occupied Node: Upgrade / Priority / Salvage Radial Buttons
        private GameObject _occupiedNodeSlotsGroup;
        private Button _upgradeRadialBtn;
        private Image _upgradeRadialIcon;
        private Text _upgradeRadialLabel;
        private Button _priorityRadialBtn;
        private Text _priorityRadialLabel;
        private Button _salvageRadialBtn;
        private Text _salvageRadialLabel;

        // End-of-Match Modal
        private GameObject _endMatchModal;
        private Text _endMatchTitleText;
        private Text _endMatchBodyText;

        private string _latestVoxMessage = "OUTPOST AEGIS-IV COGITATOR ONLINE. TAP ANY TACTICAL NODE TO DEPLOY DEFENSES.";

        private void OnEnable()
        {
            BuildNode.OnNodeSelected += HandleNodeSelected;
        }

        private void OnDisable()
        {
            BuildNode.OnNodeSelected -= HandleNodeSelected;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnVoxTransmission -= HandleVoxMessage;
            }
        }

        private void Start()
        {
            EnsureEventSystem();
            BuildCogitatorCanvasUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnVoxTransmission -= HandleVoxMessage;
                GameManager.Instance.OnVoxTransmission += HandleVoxMessage;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || _canvas == null) return;

            AnimateServoSkullAndHoloRing();
            RefreshTopBarReadouts();
            RefreshCommandAbilityButtons();
            UpdateRadialMenuPositionAndState();
            RefreshEndMatchModal();
        }

        // =====================================================================
        // 1. UGUI CANVAS & COGITATOR HIERARCHY BUILDER
        // =====================================================================

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        private Font GetBuiltInFont()
        {
            if (_uiFont != null) return _uiFont;
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null)
                _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _uiFont;
        }

        private void BuildCogitatorCanvasUI()
        {
            GameObject canvasObj = new GameObject("Cogitator_HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            _canvasRect = canvasObj.GetComponent<RectTransform>();

            BuildTopAuspexHeaderBar(_canvasRect);
            BuildBottomCommandAbilityDeck(_canvasRect);
            BuildRadialNodeTouchMenu(_canvasRect);
            BuildEndMatchModal(_canvasRect);
        }

        private void BuildTopAuspexHeaderBar(Transform parent)
        {
            Sprite metalPanel = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: false);
            Sprite crtScreen = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: true);

            // Outer Header Frame (Top Stretch)
            RectTransform headerBar = CreatePanel(parent, "TopAuspexHeader", metalPanel,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -8f), new Vector2(-24f, 96f));

            // 1. Left: Reactor Integrity + Economy Telemetry
            RectTransform telemetryBox = CreatePanel(headerBar, "TelemetryBox", metalPanel,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(500f, 78f));

            // Plasma Reactor Health Bar background & cyan fill
            RectTransform reactorBg = CreatePanel(telemetryBox, "ReactorBarBg", PixelSpriteGenerator.GetWhiteSquareSprite(),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -12f), new Vector2(472f, 26f));
            reactorBg.GetComponent<Image>().color = new Color(0.05f, 0.10f, 0.16f, 0.95f);

            GameObject fillObj = new GameObject("ReactorFill");
            fillObj.transform.SetParent(reactorBg, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            _reactorFillBar = fillObj.AddComponent<Image>();
            _reactorFillBar.sprite = PixelSpriteGenerator.GetWhiteSquareSprite();
            _reactorFillBar.color = new Color(0.18f, 0.82f, 1.0f, 0.95f);
            _reactorFillBar.type = Image.Type.Filled;
            _reactorFillBar.fillMethod = Image.FillMethod.Horizontal;
            _reactorFillBar.fillAmount = 1f;

            _reactorText = CreateText(reactorBg, "ReactorText", "PLASMA REACTOR CORE: 20 / 20", 17,
                new Color(0.95f, 0.98f, 1f), TextAnchor.MiddleCenter);

            _requisitionText = CreateText(telemetryBox, "RequisitionText", "REQUISITION: 320 RP", 20,
                new Color(0.98f, 0.82f, 0.28f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.55f, 0.5f), new Vector2(16f, 6f), new Vector2(0f, -2f));

            _commandPointsText = CreateText(telemetryBox, "CommandPtsText", "COMMAND: 3/10 CP", 20,
                new Color(0.45f, 0.82f, 1.0f), TextAnchor.MiddleRight,
                new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 6f), new Vector2(-16f, -2f));

            // 2. Center: Servo-Skull + Green Phosphor CRT Vox-Caster Screen
            RectTransform crtBox = CreatePanel(headerBar, "ServoSkullCRT", crtScreen,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(10f, 0f), new Vector2(820f, 78f));

            // Floating Servo-Skull Avatar
            GameObject skullObj = new GameObject("ServoSkullAvatar");
            skullObj.transform.SetParent(crtBox, false);
            _servoSkullIconRect = skullObj.AddComponent<RectTransform>();
            _servoSkullIconRect.anchorMin = new Vector2(0f, 0.5f);
            _servoSkullIconRect.anchorMax = new Vector2(0f, 0.5f);
            _servoSkullIconRect.pivot = new Vector2(0f, 0.5f);
            _servoSkullIconRect.anchoredPosition = new Vector2(14f, 0f);
            _servoSkullIconRect.sizeDelta = new Vector2(58f, 58f);
            Image skullImg = skullObj.AddComponent<Image>();
            skullImg.sprite = PixelSpriteGenerator.GetServoSkullSprite();

            _waveHeaderText = CreateText(crtBox, "WaveHeader", "+++ AUSPEX WAVE 01/20 : GRETCHIN FORAGERS +++", 18,
                new Color(0.45f, 1.0f, 0.58f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(82f, -2f), new Vector2(-14f, -6f));

            _voxTickerText = CreateText(crtBox, "VoxTicker", _latestVoxMessage, 16,
                new Color(0.75f, 0.98f, 0.78f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(82f, 6f), new Vector2(-14f, 0f));

            // 3. Right: Launch Wave & Chrono Speed Controls
            _launchWaveButton = CreateCogitatorButton(headerBar, "LaunchWaveBtn",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-165f, 0f), new Vector2(265f, 72f),
                new Color(0.68f, 0.16f, 0.14f),
                () =>
                {
                    if (WaveManager.Instance != null)
                        WaveManager.Instance.StartNextWave();
                },
                out _launchWaveButtonText, "LAUNCH WAVE\n[WAAAGH!]", 18);

            _speedButton = CreateCogitatorButton(headerBar, "ChronoSpeedBtn",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(138f, 72f),
                new Color(0.22f, 0.28f, 0.36f),
                () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.ToggleFastForward();
                },
                out _speedButtonText, "CHRONO\n1x SPD", 18);
        }

        private void BuildBottomCommandAbilityDeck(Transform parent)
        {
            Sprite metalPanel = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: false);
            Sprite crtScreen = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: true);

            RectTransform deck = CreatePanel(parent, "BottomCommandDeck", metalPanel,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 12f), new Vector2(650f, 104f));

            CreateText(deck, "DeckHeader", "STRATEGIC COMMAND ABILITIES", 15,
                new Color(0.92f, 0.76f, 0.26f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -24f), new Vector2(-14f, -6f));

            _abilityButtons.Clear();
            _abilityButtonLabels.Clear();

            for (int i = 0; i < 2; i++)
            {
                int index = i;
                float xOffset = 14f + i * 314f;

                Button btn = CreateCogitatorButton(deck, $"AbilityBtn_{i}",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                    new Vector2(xOffset, 10f), new Vector2(304f, 66f),
                    new Color(0.18f, 0.24f, 0.34f),
                    () => OnCommandAbilityButtonClicked(index),
                    out Text label, "ABILITY", 16);

                // Pixel-Art Ability Icon on left of button
                GameObject iconObj = new GameObject("AbilityIcon");
                iconObj.transform.SetParent(btn.transform, false);
                RectTransform iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(10f, 0f);
                iconRect.sizeDelta = new Vector2(48f, 48f);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = PixelSpriteGenerator.GetAbilityIconSprite(
                    i == 0 ? CommandAbilityType.OrbitalBombardment : CommandAbilityType.DropPodAssault);

                // Shift label text to the right of the icon
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.offsetMin = new Vector2(64f, 4f);
                labelRect.offsetMax = new Vector2(-8f, -4f);
                label.alignment = TextAnchor.MiddleLeft;

                _abilityButtons.Add(btn);
                _abilityButtonLabels.Add(label);
            }

            // Targeting Active Banner (Bottom Center)
            RectTransform targetBanner = CreatePanel(parent, "TargetingBanner", crtScreen,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(120f, 18f), new Vector2(560f, 76f));
            _cancelTargetingBanner = targetBanner.gameObject;

            _cancelTargetingText = CreateText(targetBanner, "TargetPrompt",
                "AUSPEX LOCKING: TAP BATTLEFIELD TO STRIKE", 18,
                new Color(0.45f, 1.0f, 0.58f), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0.72f, 1f), new Vector2(16f, 4f), new Vector2(-8f, -4f));

            CreateCogitatorButton(targetBanner, "AbortTargetBtn",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-12f, 0f), new Vector2(130f, 52f),
                new Color(0.65f, 0.16f, 0.16f),
                () =>
                {
                    if (CommandAbilityManager.Instance != null)
                        CommandAbilityManager.Instance.CancelTargeting();
                },
                out _, "ABORT [ESC]", 16);

            _cancelTargetingBanner.SetActive(false);
        }

        // =====================================================================
        // 2. KINGDOM RUSH-STYLE RADIAL TOUCH MENU AROUND TAPPED BUILD NODES
        // =====================================================================

        private void BuildRadialNodeTouchMenu(Transform parent)
        {
            _radialMenuRoot = new GameObject("RadialNodeMenu");
            _radialMenuRoot.transform.SetParent(parent, false);
            _radialMenuRect = _radialMenuRoot.AddComponent<RectTransform>();
            _radialMenuRect.sizeDelta = new Vector2(360f, 360f);

            // Rotating Tactical Holo-Ring centered on the Build Node
            GameObject ringObj = new GameObject("AuspexHoloRing");
            ringObj.transform.SetParent(_radialMenuRect, false);
            _holoRingRect = ringObj.AddComponent<RectTransform>();
            _holoRingRect.sizeDelta = new Vector2(260f, 260f);
            Image ringImg = ringObj.AddComponent<Image>();
            ringImg.sprite = PixelSpriteGenerator.GetRadialHoloRingSprite();
            ringImg.raycastTarget = false;

            // Center Close Button [X] directly on the node
            CreateRadialActionSlot(_radialMenuRect, "CloseRadialBtn", Vector2.zero, new Vector2(56f, 56f),
                () => CloseSelectedNodeMenu(),
                out Image closeIcon, out Text closeLabel);
            closeIcon.enabled = false;
            closeLabel.text = "X";
            closeLabel.fontSize = 22;
            closeLabel.color = new Color(1f, 0.45f, 0.4f);

            // --- GROUP A: Empty Node (5 Tower Construction Slots around the ring) ---
            _emptyNodeSlotsGroup = new GameObject("EmptyNodeSlots");
            _emptyNodeSlotsGroup.transform.SetParent(_radialMenuRect, false);
            RectTransform emptyGroupRect = _emptyNodeSlotsGroup.AddComponent<RectTransform>();
            emptyGroupRect.sizeDelta = Vector2.zero;

            float[] buildAnglesDeg = { 90f, 18f, -54f, -126f, 162f };
            float radius = 132f;

            for (int i = 0; i < 5; i++)
            {
                int towerIdx = i;
                float rad = buildAnglesDeg[i] * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

                Button slotBtn = CreateRadialActionSlot(emptyGroupRect, $"BuildSlot_{i}", pos, new Vector2(96f, 96f),
                    () => OnRadialBuildTowerClicked(towerIdx),
                    out Image icon, out Text label);

                _buildSlotButtons.Add(slotBtn);
                _buildSlotIcons.Add(icon);
                _buildSlotLabels.Add(label);
            }

            // --- GROUP B: Occupied Node (Upgrade at 90°, Priority at 195°, Salvage at -15°) ---
            _occupiedNodeSlotsGroup = new GameObject("OccupiedNodeSlots");
            _occupiedNodeSlotsGroup.transform.SetParent(_radialMenuRect, false);
            RectTransform occGroupRect = _occupiedNodeSlotsGroup.AddComponent<RectTransform>();
            occGroupRect.sizeDelta = Vector2.zero;

            _upgradeRadialBtn = CreateRadialActionSlot(occGroupRect, "UpgradeSlot",
                new Vector2(0f, 134f), new Vector2(104f, 104f),
                OnRadialUpgradeClicked, out _upgradeRadialIcon, out _upgradeRadialLabel);

            _priorityRadialBtn = CreateRadialActionSlot(occGroupRect, "PrioritySlot",
                new Vector2(-132f, -20f), new Vector2(98f, 98f),
                OnRadialPriorityClicked, out Image prioIcon, out _priorityRadialLabel);
            prioIcon.enabled = false;

            _salvageRadialBtn = CreateRadialActionSlot(occGroupRect, "SalvageSlot",
                new Vector2(132f, -20f), new Vector2(98f, 98f),
                OnRadialSalvageClicked, out Image salvIcon, out _salvageRadialLabel);
            salvIcon.enabled = false;

            // --- Bottom Cogitator CRT Readout Card below the Radial Ring ---
            Sprite crtScreen = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: true);
            RectTransform infoCard = CreatePanel(_radialMenuRect, "CogitatorNodeInfoCard", crtScreen,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f),
                new Vector2(0f, -155f), new Vector2(430f, 96f));

            _radialNodeTitleText = CreateText(infoCard, "NodeTitle", "TACTICAL EMPLACEMENT", 17,
                new Color(0.98f, 0.84f, 0.32f), TextAnchor.UpperCenter,
                new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(10f, -2f), new Vector2(-10f, -6f));

            _radialCogitatorStatsText = CreateText(infoCard, "NodeStats",
                "TAP A RADIAL SLOT TO CONSTRUCT IMPERIAL DEFENSES", 15,
                new Color(0.55f, 1.0f, 0.68f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 0.62f), new Vector2(12f, 6f), new Vector2(-12f, 0f));

            _radialMenuRoot.SetActive(false);
        }

        private Button CreateRadialActionSlot(
            Transform parent,
            string name,
            Vector2 anchoredPos,
            Vector2 size,
            UnityEngine.Events.UnityAction onClick,
            out Image turretIcon,
            out Text badgeText)
        {
            GameObject slotObj = new GameObject(name);
            slotObj.transform.SetParent(parent, false);
            RectTransform rt = slotObj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image bgImg = slotObj.AddComponent<Image>();
            bgImg.sprite = PixelSpriteGenerator.GetRadialSlotButtonSprite();

            Button btn = slotObj.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.onClick.AddListener(onClick);

            // Turret / Action Sprite Icon
            GameObject iconObj = new GameObject("SlotIcon");
            iconObj.transform.SetParent(rt, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0f, 10f);
            iconRt.sizeDelta = new Vector2(size.x * 0.56f, size.y * 0.56f);
            turretIcon = iconObj.AddComponent<Image>();
            turretIcon.raycastTarget = false;

            // Bottom Cost / Action Badge
            GameObject badgeObj = new GameObject("BadgeLabel");
            badgeObj.transform.SetParent(rt, false);
            RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0.44f);
            badgeRt.offsetMin = new Vector2(2f, 3f);
            badgeRt.offsetMax = new Vector2(-2f, 0f);

            badgeText = badgeObj.AddComponent<Text>();
            badgeText.font = GetBuiltInFont();
            badgeText.fontSize = 14;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(0.98f, 0.88f, 0.42f);
            badgeText.raycastTarget = false;

            Outline outline = badgeObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            return btn;
        }

        private void BuildEndMatchModal(Transform parent)
        {
            Sprite metalPanel = PixelSpriteGenerator.GetCogitatorPanelSprite(crtGreenScreen: false);
            RectTransform modal = CreatePanel(parent, "EndMatchModal", metalPanel,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(680f, 290f));
            _endMatchModal = modal.gameObject;

            _endMatchTitleText = CreateText(modal, "EndTitle", "VICTORY FOR THE IMPERIUM!", 28,
                new Color(0.98f, 0.82f, 0.25f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.65f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-20f, -14f));

            _endMatchBodyText = CreateText(modal, "EndBody", "", 19,
                new Color(0.82f, 0.96f, 0.85f), TextAnchor.MiddleCenter,
                new Vector2(0f, 0.32f), new Vector2(1f, 0.68f), new Vector2(28f, 0f), new Vector2(-28f, 0f));

            CreateCogitatorButton(modal, "RestartSiegeBtn",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 22f), new Vector2(280f, 64f),
                new Color(0.68f, 0.18f, 0.14f),
                () =>
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                },
                out _, "REDEPLOY SIEGE", 20);

            _endMatchModal.SetActive(false);
        }

        // =====================================================================
        // 3. LIVE TELEMETRY & RADIAL POSITION UPDATES
        // =====================================================================

        private void AnimateServoSkullAndHoloRing()
        {
            if (_servoSkullIconRect != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 3.2f) * 4.5f;
                _servoSkullIconRect.anchoredPosition = new Vector2(14f, bob);
            }

            if (_holoRingRect != null && _radialMenuRoot != null && _radialMenuRoot.activeSelf)
            {
                _holoRingRect.Rotate(0f, 0f, -28f * Time.unscaledDeltaTime);
            }
        }

        private void RefreshTopBarReadouts()
        {
            GameManager gm = GameManager.Instance;
            WaveManager wm = WaveManager.Instance;

            float hpRatio = Mathf.Clamp01((float)gm.ReactorHealth / Mathf.Max(1, gm.MaxReactorHealth));
            _reactorFillBar.fillAmount = hpRatio;
            _reactorFillBar.color = Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.18f, 0.82f, 1.0f), hpRatio);
            _reactorText.text = $"CENTRAL PLASMA REACTOR: {gm.ReactorHealth} / {gm.MaxReactorHealth}";

            _requisitionText.text = $"REQUISITION: {gm.Requisition} RP";
            _commandPointsText.text = $"COMMAND: {gm.CommandPoints} / {gm.MaxCommandPoints} CP";

            if (wm != null)
            {
                string waveTitle = (wm.CurrentWaveIndex >= 0 && wm.CurrentWaveIndex < wm.waves.Count)
                    ? wm.waves[wm.CurrentWaveIndex].waveTitle.ToUpper()
                    : "AWAITING DEPLOYMENT";
                _waveHeaderText.text = $"+++ AUSPEX WAVE {wm.DisplayWaveNumber:D2}/{wm.TotalWaves:D2} : {waveTitle} +++";

                bool canLaunch = !wm.IsSpawning && gm.CurrentState == GameState.Preparing;
                _launchWaveButton.interactable = canLaunch;
                _launchWaveButtonText.text = canLaunch ? "LAUNCH WAVE\n[WAAAGH!]" : "WAVE ACTIVE\n[DEFEND GATE]";
            }

            _voxTickerText.text = $"[VOX]: {_latestVoxMessage}";
            _speedButtonText.text = Mathf.Approximately(Time.timeScale, 1f) ? "CHRONO\n1x SPD" : "CHRONO\n2x SPD";
        }

        private void RefreshCommandAbilityButtons()
        {
            GameManager gm = GameManager.Instance;
            CommandAbilityManager cam = CommandAbilityManager.Instance;
            if (cam == null) return;

            for (int i = 0; i < _abilityButtons.Count && i < gm.availableAbilities.Count; i++)
            {
                AbilityDataSO ability = gm.availableAbilities[i];
                if (ability == null) continue;

                float cd = cam.GetRemainingCooldown(ability.abilityType);
                bool ready = cam.CanActivateAbility(ability);
                _abilityButtons[i].interactable = ready;

                _abilityButtonLabels[i].text = cd > 0f
                    ? $"{ability.displayName.ToUpper()}\n[COOLDOWN: {cd:F0}s]"
                    : $"{ability.displayName.ToUpper()}\n[{ability.commandPointCost} CP] READY";
            }

            bool isTargeting = cam.ActiveTargetingAbility != null;
            if (_cancelTargetingBanner.activeSelf != isTargeting)
                _cancelTargetingBanner.SetActive(isTargeting);

            if (isTargeting)
            {
                _cancelTargetingText.text =
                    $"TARGETING {cam.ActiveTargetingAbility.displayName.ToUpper()}: TAP BATTLEFIELD";
            }
        }

        private void UpdateRadialMenuPositionAndState()
        {
            if (_selectedNode == null || Camera.main == null)
            {
                if (_radialMenuRoot.activeSelf)
                    _radialMenuRoot.SetActive(false);
                return;
            }

            if (!_radialMenuRoot.activeSelf)
                _radialMenuRoot.SetActive(true);

            // Convert world node position to Canvas local position and clamp inside safe screen margins
            Vector3 screenPos = Camera.main.WorldToScreenPoint(_selectedNode.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, null, out Vector2 localPoint);

            Vector2 halfCanvas = _canvasRect.sizeDelta * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + 230f, halfCanvas.x - 230f);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y + 270f, halfCanvas.y - 245f);
            _radialMenuRect.anchoredPosition = localPoint;

            string highGroundNote = _selectedNode.nodeType == BuildNodeType.CliffLedge ? " (+15% RANGE)" : "";
            _radialNodeTitleText.text = $"{_selectedNode.nodeName.ToUpper()} [{_selectedNode.nodeType}{highGroundNote}]";

            if (!_selectedNode.IsOccupied)
            {
                _emptyNodeSlotsGroup.SetActive(true);
                _occupiedNodeSlotsGroup.SetActive(false);
                RefreshEmptyNodeRadialSlots();
            }
            else
            {
                _emptyNodeSlotsGroup.SetActive(false);
                _occupiedNodeSlotsGroup.SetActive(true);
                RefreshOccupiedNodeRadialSlots();
            }
        }

        private void RefreshEmptyNodeRadialSlots()
        {
            GameManager gm = GameManager.Instance;
            _radialCogitatorStatsText.text =
                "SELECT TOWER SLOT: BOLTER (Kinetic) | LASCANNON (Anti-Armor)\nHELLHOUND (Flame AoE) | BASILISK (Global Splash) | RELAY (Support)";

            for (int i = 0; i < _buildSlotButtons.Count; i++)
            {
                if (i >= gm.availableTowers.Count || gm.availableTowers[i] == null)
                {
                    _buildSlotButtons[i].gameObject.SetActive(false);
                    continue;
                }

                _buildSlotButtons[i].gameObject.SetActive(true);
                TowerDataSO towerData = gm.availableTowers[i];
                bool allowed = _selectedNode.CanBuildTower(towerData);
                int cost = towerData.tiers[0].cost;
                bool affordable = gm.CanAffordRequisition(cost);

                _buildSlotIcons[i].sprite = PixelSpriteGenerator.GetTowerTurretSprite(towerData.category, 0);
                _buildSlotIcons[i].color = allowed ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);

                _buildSlotButtons[i].interactable = allowed && affordable;

                string shortName = GetShortTowerLabel(towerData.category);
                _buildSlotLabels[i].text = allowed ? $"{shortName}\n{cost} RP" : $"{shortName}\n[LOCKED]";
                _buildSlotLabels[i].color = !allowed
                    ? new Color(0.75f, 0.35f, 0.35f)
                    : (affordable ? new Color(0.98f, 0.88f, 0.42f) : new Color(0.95f, 0.45f, 0.35f));
            }
        }

        private void RefreshOccupiedNodeRadialSlots()
        {
            TowerController tower = _selectedNode.InstalledTower;
            TowerTierData tier = tower.CurrentTierData;

            string empWarn = tower.IsDisabled ? $" [EMP STUNNED {tower.DisableRemainingSeconds:F1}s]" : "";
            string statsLine = tower.Data.isSupportTower
                ? $"TIER {tower.CurrentTierIndex + 1}/3 {tier.tierName}{empWarn}\nAURA: {tower.GetEffectiveRange():F1}m | +{tier.auraRangeBonusPercent * 100f:F0}% RNG & RATE | REVEALS KOMMANDOS"
                : $"TIER {tower.CurrentTierIndex + 1}/3 {tier.tierName}{empWarn}\nDMG: {tier.damage:F0} ({tower.Data.damageType}) | RATE: {tower.GetEffectiveFireRate():F1}/s | RNG: {tower.GetEffectiveRange():F1}m";

            _radialCogitatorStatsText.text = statsLine;

            // 1. Upgrade Slot
            if (tower.CanUpgrade)
            {
                int upgCost = tower.GetNextUpgradeCost();
                bool canAfford = GameManager.Instance.CanAffordRequisition(upgCost);
                _upgradeRadialBtn.interactable = canAfford;
                _upgradeRadialIcon.enabled = true;
                _upgradeRadialIcon.sprite = PixelSpriteGenerator.GetTowerTurretSprite(
                    tower.Data.category, tower.CurrentTierIndex + 1);
                _upgradeRadialLabel.text = $"TIER {tower.CurrentTierIndex + 2}\n{upgCost} RP";
                _upgradeRadialLabel.color = canAfford
                    ? new Color(0.45f, 1.0f, 0.58f)
                    : new Color(0.95f, 0.45f, 0.35f);
            }
            else
            {
                _upgradeRadialBtn.interactable = false;
                _upgradeRadialIcon.enabled = true;
                _upgradeRadialIcon.sprite = PixelSpriteGenerator.GetTowerTurretSprite(
                    tower.Data.category, tower.CurrentTierIndex);
                _upgradeRadialLabel.text = "MAX\nTIER 3";
                _upgradeRadialLabel.color = new Color(0.98f, 0.82f, 0.25f);
            }

            // 2. Priority Slot
            _priorityRadialBtn.interactable = !tower.Data.isSupportTower;
            _priorityRadialLabel.text = tower.Data.isSupportTower
                ? "AURA\nPASSIVE"
                : $"TARGET:\n{tower.Priority.ToString().ToUpper()}";

            // 3. Salvage Slot
            int invested = tower.Data.GetTotalInvestedCost(tower.CurrentTierIndex);
            int refund = Mathf.RoundToInt(invested * 0.70f);
            _salvageRadialLabel.text = $"SALVAGE\n+{refund} RP";
            _salvageRadialLabel.color = new Color(1.0f, 0.65f, 0.32f);
        }

        private void RefreshEndMatchModal()
        {
            GameState state = GameManager.Instance.CurrentState;
            bool showEnd = state == GameState.Victory || state == GameState.Defeat;
            if (_endMatchModal.activeSelf != showEnd)
                _endMatchModal.SetActive(showEnd);

            if (showEnd)
            {
                _endMatchTitleText.text = state == GameState.Victory
                    ? "+++ VICTORY FOR THE IMPERIUM +++"
                    : "+++ DEFEAT : REACTOR DESTROYED +++";
                _endMatchBodyText.text = _latestVoxMessage;
            }
        }

        // =====================================================================
        // 4. EVENT HANDLERS & UI HELPERS
        // =====================================================================

        private void HandleNodeSelected(BuildNode node)
        {
            if (_selectedNode != null && _selectedNode != node)
            {
                _selectedNode.ShowRangeIndicator(false);
            }

            _selectedNode = node;
            if (_selectedNode != null)
            {
                _selectedNode.ShowRangeIndicator(true);
            }
        }

        private void CloseSelectedNodeMenu()
        {
            if (_selectedNode != null)
            {
                _selectedNode.ShowRangeIndicator(false);
                _selectedNode = null;
            }
            if (_radialMenuRoot != null)
            {
                _radialMenuRoot.SetActive(false);
            }
        }

        private void OnRadialBuildTowerClicked(int towerIndex)
        {
            if (_selectedNode == null || GameManager.Instance == null) return;
            if (towerIndex < 0 || towerIndex >= GameManager.Instance.availableTowers.Count) return;

            TowerDataSO data = GameManager.Instance.availableTowers[towerIndex];
            if (_selectedNode.TryConstructTower(data))
            {
                GameManager.Instance.BroadcastVox($"{data.displayName} constructed at {_selectedNode.nodeName}.");
            }
        }

        private void OnRadialUpgradeClicked()
        {
            if (_selectedNode == null) return;
            if (_selectedNode.TryUpgradeInstalledTower())
            {
                TowerController t = _selectedNode.InstalledTower;
                GameManager.Instance.BroadcastVox(
                    $"Upgraded {t.Data.displayName} to Tier {t.CurrentTierIndex + 1}: {t.CurrentTierData.tierName}!");
            }
        }

        private void OnRadialPriorityClicked()
        {
            if (_selectedNode == null || !_selectedNode.IsOccupied) return;
            _selectedNode.InstalledTower.CycleTargetPriority();
        }

        private void OnRadialSalvageClicked()
        {
            if (_selectedNode == null || !_selectedNode.IsOccupied) return;
            _selectedNode.SellInstalledTower();
            CloseSelectedNodeMenu();
        }

        private void OnCommandAbilityButtonClicked(int index)
        {
            if (GameManager.Instance == null || CommandAbilityManager.Instance == null) return;
            if (index < 0 || index >= GameManager.Instance.availableAbilities.Count) return;

            CloseSelectedNodeMenu();
            AbilityDataSO ability = GameManager.Instance.availableAbilities[index];
            CommandAbilityManager.Instance.BeginTargetingAbility(ability);
        }

        private void HandleVoxMessage(string msg)
        {
            _latestVoxMessage = msg;
        }

        private static string GetShortTowerLabel(TowerCategory cat)
        {
            return cat switch
            {
                TowerCategory.HeavyBolterBunker => "BOLTER",
                TowerCategory.LascannonTurret => "LASCANNON",
                TowerCategory.HellhoundFlameBunker => "HELLHOUND",
                TowerCategory.BasiliskArtilleryPit => "BASILISK",
                TowerCategory.AstropathicRelay => "RELAY",
                _ => "TOWER"
            };
        }

        private static RectTransform CreatePanel(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            Image img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            return rt;
        }

        private Text CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment,
            Vector2? anchorMin = null,
            Vector2? anchorMax = null,
            Vector2? offsetMin = null,
            Vector2? offsetMax = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin ?? Vector2.zero;
            rt.anchorMax = anchorMax ?? Vector2.one;
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;

            Text txt = obj.AddComponent<Text>();
            txt.font = GetBuiltInFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            return txt;
        }

        private Button CreateCogitatorButton(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            Color tintColor,
            UnityEngine.Events.UnityAction onClick,
            out Text labelText,
            string textContent,
            int fontSize)
        {
            RectTransform rt = CreatePanel(
                parent, name, PixelSpriteGenerator.GetCogitatorPanelSprite(false),
                anchorMin, anchorMax, pivot, anchoredPos, sizeDelta);

            Image img = rt.GetComponent<Image>();
            img.color = tintColor;

            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            labelText = CreateText(rt, "Label", textContent, fontSize,
                new Color(0.98f, 0.92f, 0.76f), TextAnchor.MiddleCenter);
            return btn;
        }
    }
}
