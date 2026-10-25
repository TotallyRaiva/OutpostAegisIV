using OutpostAegis.Abilities;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Map;
using OutpostAegis.Towers;
using OutpostAegis.Waves;
using UnityEngine;

namespace OutpostAegis.UI
{
    /// <summary>
    /// Self-contained mobile-scaled tactical HUD for Outpost Aegis-IV:
    /// - Top Tactical Bar: Plasma Reactor HP, Requisition, Command Points, Wave Status, Start Wave, 2x Speed
    /// - Node Build/Upgrade Drawer: Construct allowed towers, 3-Tier Upgrades, Target Priority Selector, Sell
    /// - Command Abilities Bar: Orbital Bombardment & Drop Pod Assault buttons with cooldown readouts
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private BuildNode _selectedNode;
        private string _latestVoxMessage = "Awaiting tactical orders, Commander.";

        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _voxStyle;
        private bool _stylesInitialized;

        private void OnEnable()
        {
            BuildNode.OnNodeSelected += HandleNodeSelected;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnVoxTransmission += HandleVoxMessage;
            }
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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnVoxTransmission -= HandleVoxMessage;
                GameManager.Instance.OnVoxTransmission += HandleVoxMessage;
            }
        }

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

        private void HandleVoxMessage(string msg)
        {
            _latestVoxMessage = msg;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.92f, 0.88f, 0.75f) }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.95f, 0.82f, 0.35f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            _voxStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.95f, 0.65f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (GameManager.Instance == null) return;
            InitStyles();

            // Responsive scaling for mobile & desktop resolutions
            float scale = Mathf.Clamp(Screen.height / 540f, 0.85f, 1.8f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float uiWidth = Screen.width / scale;
            float uiHeight = Screen.height / scale;

            DrawTopStatusBar(uiWidth);
            DrawBottomCommandBar(uiWidth, uiHeight);

            if (_selectedNode != null)
            {
                DrawNodeInspectorDrawer(uiWidth, uiHeight);
            }

            if (GameManager.Instance.CurrentState == GameState.Victory ||
                GameManager.Instance.CurrentState == GameState.Defeat)
            {
                DrawEndMatchOverlay(uiWidth, uiHeight);
            }
        }

        private void DrawTopStatusBar(float uiWidth)
        {
            GUI.Box(new Rect(8, 6, uiWidth - 16, 56), GUIContent.none, _boxStyle);

            GameManager gm = GameManager.Instance;
            WaveManager wm = WaveManager.Instance;

            string reactorText = $"PLASMA REACTOR: {gm.ReactorHealth}/{gm.MaxReactorHealth}";
            string reqText = $"REQUISITION: {gm.Requisition} RP";
            string cpText = $"COMMAND PTS: {gm.CommandPoints}/{gm.MaxCommandPoints} CP";
            string waveText = wm != null
                ? $"WAVE: {wm.DisplayWaveNumber}/{wm.TotalWaves}"
                : "WAVE: 1/20";

            GUI.Label(new Rect(18, 10, 210, 24), reactorText, _headerStyle);
            GUI.Label(new Rect(235, 10, 185, 24), reqText, _headerStyle);
            GUI.Label(new Rect(425, 10, 195, 24), cpText, _headerStyle);
            GUI.Label(new Rect(625, 10, 130, 24), waveText, _headerStyle);

            // Start Next Wave button
            bool canStartWave = wm != null && !wm.IsSpawning && gm.CurrentState == GameState.Preparing;
            GUI.enabled = canStartWave;
            string waveBtnLabel = canStartWave ? "LAUNCH NEXT WAVE" : "WAVE IN PROGRESS";
            if (GUI.Button(new Rect(uiWidth - 265, 11, 165, 36), waveBtnLabel, _buttonStyle))
            {
                wm.StartNextWave();
            }
            GUI.enabled = true;

            // Speed toggle button (1x / 2x)
            string speedLabel = Mathf.Approximately(Time.timeScale, 1f) ? "1x SPD" : "2x SPD";
            if (GUI.Button(new Rect(uiWidth - 92, 11, 76, 36), speedLabel, _buttonStyle))
            {
                gm.ToggleFastForward();
            }

            // Vox transmission ticker
            GUI.Label(new Rect(18, 36, uiWidth - 300, 22), $"[VOX]: {_latestVoxMessage}", _voxStyle);
        }

        private void DrawBottomCommandBar(float uiWidth, float uiHeight)
        {
            float barHeight = 58f;
            float y = uiHeight - barHeight - 6f;
            GUI.Box(new Rect(8, y, 470, barHeight), GUIContent.none, _boxStyle);

            GUI.Label(new Rect(18, y + 6, 150, 22), "COMMAND ABILITIES:", _headerStyle);

            GameManager gm = GameManager.Instance;
            CommandAbilityManager cam = CommandAbilityManager.Instance;
            if (cam == null) return;

            float btnX = 18f;
            for (int i = 0; i < gm.availableAbilities.Count; i++)
            {
                AbilityDataSO ability = gm.availableAbilities[i];
                if (ability == null) continue;

                float cd = cam.GetRemainingCooldown(ability.abilityType);
                bool ready = cam.CanActivateAbility(ability);
                GUI.enabled = ready;

                string label = cd > 0f
                    ? $"{ability.displayName} ({cd:F0}s)"
                    : $"{ability.displayName} [{ability.commandPointCost} CP]";

                if (GUI.Button(new Rect(btnX, y + 25f, 215f, 28f), label, _buttonStyle))
                {
                    cam.BeginTargetingAbility(ability);
                }

                GUI.enabled = true;
                btnX += 225f;
            }
        }

        private void DrawNodeInspectorDrawer(float uiWidth, float uiHeight)
        {
            float panelWidth = 440f;
            float panelHeight = 250f;
            float x = uiWidth - panelWidth - 10f;
            float y = uiHeight - panelHeight - 6f;

            GUI.Box(new Rect(x, y, panelWidth, panelHeight), GUIContent.none, _boxStyle);

            string nodeHeader = $"{_selectedNode.nodeName} [{_selectedNode.nodeType}]";
            GUI.Label(new Rect(x + 12, y + 8, panelWidth - 90, 24), nodeHeader, _headerStyle);

            if (GUI.Button(new Rect(x + panelWidth - 72, y + 6, 62, 26), "CLOSE", _buttonStyle))
            {
                _selectedNode.ShowRangeIndicator(false);
                _selectedNode = null;
                return;
            }

            if (!_selectedNode.IsOccupied)
            {
                DrawBuildOptions(x + 12, y + 38, panelWidth - 24);
            }
            else
            {
                DrawInstalledTowerControls(x + 12, y + 38, panelWidth - 24);
            }
        }

        private void DrawBuildOptions(float startX, float startY, float width)
        {
            GameManager gm = GameManager.Instance;
            float curY = startY;

            for (int i = 0; i < gm.availableTowers.Count; i++)
            {
                TowerDataSO towerData = gm.availableTowers[i];
                if (towerData == null) continue;

                bool allowedOnNode = _selectedNode.CanBuildTower(towerData);
                int cost = towerData.tiers[0].cost;
                bool canAfford = gm.CanAffordRequisition(cost);

                GUI.enabled = allowedOnNode && canAfford;

                string restrictionNote = allowedOnNode ? "" : $" [Requires {towerData.allowedNodeTypes}]";
                string btnText = $"{towerData.displayName} ({cost} RP){restrictionNote}\n{towerData.tiers[0].description}";

                if (GUI.Button(new Rect(startX, curY, width, 38f), btnText, _buttonStyle))
                {
                    _selectedNode.TryConstructTower(towerData);
                }

                GUI.enabled = true;
                curY += 41f;
            }
        }

        private void DrawInstalledTowerControls(float startX, float startY, float width)
        {
            TowerController tower = _selectedNode.InstalledTower;
            TowerTierData tier = tower.CurrentTierData;

            string statusWarn = tower.IsDisabled ? $" [EMP DISABLED: {tower.DisableRemainingSeconds:F1}s]" : "";
            GUI.Label(new Rect(startX, startY, width, 24),
                $"Tier {tower.CurrentTierIndex + 1}/3: {tier.tierName}{statusWarn}", _headerStyle);

            string statsLine = tower.Data.isSupportTower
                ? $"Aura Range: {tower.GetEffectiveRange():F1}m | +{tier.auraRangeBonusPercent * 100f:F0}% Rng / +{tier.auraFireRateBonusPercent * 100f:F0}% Rate | Reveals Kommandos"
                : $"DMG: {tier.damage:F0} ({tower.Data.damageType}) | Rate: {tower.GetEffectiveFireRate():F1}/s | Range: {tower.GetEffectiveRange():F1}m";

            GUI.Label(new Rect(startX, startY + 26, width, 22), statsLine);
            GUI.Label(new Rect(startX, startY + 46, width, 36), tier.description);

            // Upgrade Button
            if (tower.CanUpgrade)
            {
                int upgCost = tower.GetNextUpgradeCost();
                TowerTierData nextTier = tower.Data.tiers[tower.CurrentTierIndex + 1];
                GUI.enabled = GameManager.Instance.CanAffordRequisition(upgCost);

                string upgLabel = $"UPGRADE TO TIER {tower.CurrentTierIndex + 2}: {nextTier.tierName} ({upgCost} RP)";
                if (GUI.Button(new Rect(startX, startY + 86, width, 40), upgLabel, _buttonStyle))
                {
                    _selectedNode.TryUpgradeInstalledTower();
                }
                GUI.enabled = true;
            }
            else
            {
                GUI.Label(new Rect(startX, startY + 92, width, 28), "MAXIMUM IMPERIAL TIER REACHED (TIER 3)", _headerStyle);
            }

            // Target Priority Selector (for combat towers)
            if (!tower.Data.isSupportTower)
            {
                string prioLabel = $"TARGET PRIORITY: {tower.Priority.ToString().ToUpper()} (Tap to Cycle)";
                if (GUI.Button(new Rect(startX, startY + 132, width * 0.64f, 36), prioLabel, _buttonStyle))
                {
                    tower.CycleTargetPriority();
                }
            }

            // Sell / Salvage Button
            int totalInvested = tower.Data.GetTotalInvestedCost(tower.CurrentTierIndex);
            int refund = Mathf.RoundToInt(totalInvested * 0.70f);
            if (GUI.Button(new Rect(startX + width * 0.66f, startY + 132, width * 0.34f, 36), $"SALVAGE (+{refund} RP)", _buttonStyle))
            {
                _selectedNode.SellInstalledTower();
            }
        }

        private void DrawEndMatchOverlay(float uiWidth, float uiHeight)
        {
            float w = 420f;
            float h = 160f;
            Rect r = new Rect((uiWidth - w) * 0.5f, (uiHeight - h) * 0.5f, w, h);
            GUI.Box(r, GUIContent.none, _boxStyle);

            bool won = GameManager.Instance.CurrentState == GameState.Victory;
            string title = won
                ? "VICTORY — OUTPOST AEGIS-IV STANDS!"
                : "DEFEAT — THE PLASMA REACTOR HAS FALLEN";

            GUI.Label(new Rect(r.x + 24, r.y + 20, w - 48, 30), title, _headerStyle);
            GUI.Label(new Rect(r.x + 24, r.y + 55, w - 48, 40), _latestVoxMessage);

            if (GUI.Button(new Rect(r.x + 110, r.y + 102, 200, 40), "RESTART SIEGE", _buttonStyle))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
