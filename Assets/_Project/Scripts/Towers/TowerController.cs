using System.Collections.Generic;
using OutpostAegis.Data;
using OutpostAegis.Enemies;
using OutpostAegis.Map;
using OutpostAegis.Visuals;
using UnityEngine;

namespace OutpostAegis.Towers
{
    /// <summary>
    /// Controls Imperial Guard & Space Marine defensive emplacements:
    /// - Heavy Bolter Bunker (Fast single-target kinetic)
    /// - Lascannon Turret (Slow sniper armor-piercing)
    /// - Hellhound Flame-Bunker (Continuous AoE flame cone ignoring cover)
    /// - Basilisk Artillery Pit (Global/long-range explosive splash + slow)
    /// - Astropathic Relay (Buffs adjacent tower range & fire rate + reveals stealth Kommandos)
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        public static readonly List<TowerController> ActiveTowers = new List<TowerController>();

        public TowerDataSO Data { get; private set; }
        public BuildNode ParentNode { get; private set; }
        public int CurrentTierIndex { get; private set; }
        public TargetPriority Priority { get; set; }

        public bool IsDisabled => _disableTimer > 0f;
        public float DisableRemainingSeconds => Mathf.Max(0f, _disableTimer);

        // Buff multipliers from nearby Astropathic Relays and Cliff Ledge high ground
        private float _relayRangeBonus;
        private float _relayFireRateBonus;
        private float _highGroundRangeBonus;

        private float _attackCooldown;
        private float _disableTimer;

        // Visual sub-objects
        private SpriteRenderer _baseRenderer;
        private Transform _turretTransform;
        private SpriteRenderer _turretRenderer;
        private PixelSpriteAnimator _turretAnimator;
        private Sprite[] _turretFrames;
        private LineRenderer _laserOrConeRenderer;
        private float _beamDisplayTimer;

        private void OnEnable()
        {
            if (!ActiveTowers.Contains(this))
                ActiveTowers.Add(this);
        }

        private void OnDisable()
        {
            ActiveTowers.Remove(this);
        }

        public void Initialize(TowerDataSO data, BuildNode parentNode)
        {
            Data = data;
            ParentNode = parentNode;
            CurrentTierIndex = 0;
            Priority = data.defaultPriority;

            // High Cliff Ledges grant +15% bonus range to emplaced towers
            _highGroundRangeBonus = (parentNode != null && parentNode.nodeType == BuildNodeType.CliffLedge) ? 0.15f : 0f;

            SetupVisualHierarchy();
            RefreshTierVisuals();
        }

        public TowerTierData CurrentTierData =>
            Data.tiers != null && Data.tiers.Length > CurrentTierIndex
                ? Data.tiers[CurrentTierIndex]
                : default;

        public bool CanUpgrade => Data != null && Data.tiers != null && CurrentTierIndex < Data.tiers.Length - 1;

        public int GetNextUpgradeCost()
        {
            if (!CanUpgrade) return 0;
            return Data.tiers[CurrentTierIndex + 1].cost;
        }

        public void UpgradeToNextTier()
        {
            if (!CanUpgrade) return;
            CurrentTierIndex++;
            RefreshTierVisuals();
        }

        public float GetEffectiveRange()
        {
            return CurrentTierData.range * (1f + _highGroundRangeBonus + _relayRangeBonus);
        }

        public float GetEffectiveFireRate()
        {
            return CurrentTierData.fireRate * (1f + _relayFireRateBonus);
        }

        public void CycleTargetPriority()
        {
            int next = ((int)Priority + 1) % 4;
            Priority = (TargetPriority)next;
        }

        public void DisableTower(float duration)
        {
            _disableTimer = Mathf.Max(_disableTimer, duration);
        }

        private void SetupVisualHierarchy()
        {
            _baseRenderer = GetComponent<SpriteRenderer>();
            if (_baseRenderer == null)
                _baseRenderer = gameObject.AddComponent<SpriteRenderer>();
            _baseRenderer.sortingOrder = 5;

            Transform existingTurret = transform.Find("TurretHead");
            if (existingTurret == null)
            {
                GameObject turretObj = new GameObject("TurretHead");
                turretObj.transform.SetParent(transform, false);
                _turretTransform = turretObj.transform;
                _turretRenderer = turretObj.AddComponent<SpriteRenderer>();
                _turretRenderer.sortingOrder = 6;
                _turretAnimator = turretObj.AddComponent<PixelSpriteAnimator>();
            }
            else
            {
                _turretTransform = existingTurret;
                _turretRenderer = existingTurret.GetComponent<SpriteRenderer>();
                _turretAnimator = existingTurret.GetComponent<PixelSpriteAnimator>();
                if (_turretAnimator == null)
                    _turretAnimator = existingTurret.gameObject.AddComponent<PixelSpriteAnimator>();
            }

            if (_laserOrConeRenderer == null)
            {
                _laserOrConeRenderer = gameObject.AddComponent<LineRenderer>();
                _laserOrConeRenderer.useWorldSpace = true;
                _laserOrConeRenderer.sortingOrder = 15;
                _laserOrConeRenderer.material = new Material(Shader.Find("Sprites/Default"));
                _laserOrConeRenderer.enabled = false;
            }
        }

        private void RefreshTierVisuals()
        {
            if (Data == null) return;
            _baseRenderer.sprite = PixelSpriteGenerator.GetTowerBaseSprite(Data.category, CurrentTierIndex);
            _turretFrames = PixelSpriteGenerator.GetTowerTurretFrames(Data.category, CurrentTierIndex);
            _turretRenderer.sprite = _turretFrames[0];

            if (_turretAnimator != null)
            {
                if (Data.isSupportTower)
                {
                    _turretAnimator.SetLoopAnimation(_turretFrames, fps: 5f, randomStartFrame: false);
                }
                else
                {
                    _turretAnimator.SetLoopAnimation(new[] { _turretFrames[0] }, fps: 1f, randomStartFrame: false);
                }
            }
        }

        private void Update()
        {
            if (Data == null) return;

            if (_beamDisplayTimer > 0f)
            {
                _beamDisplayTimer -= Time.deltaTime;
                if (_beamDisplayTimer <= 0f && _laserOrConeRenderer != null)
                {
                    _laserOrConeRenderer.enabled = false;
                }
            }

            if (_disableTimer > 0f)
            {
                _disableTimer -= Time.deltaTime;
                _turretRenderer.color = new Color(0.45f, 0.45f, 0.55f, 0.8f);
                return;
            }

            _turretRenderer.color = Color.white;
            RecalculateRelayBuffs();

            if (Data.isSupportTower)
            {
                // Slowly rotate Astropathic dish
                _turretTransform.Rotate(0f, 0f, 35f * Time.deltaTime);
                return;
            }

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            EnemyController target = AcquireTarget();
            if (target == null) return;

            AimTurretAt(target.transform.position);
            ExecuteAttack(target);
            _attackCooldown = 1f / Mathf.Max(0.1f, GetEffectiveFireRate());
        }

        private void RecalculateRelayBuffs()
        {
            _relayRangeBonus = 0f;
            _relayFireRateBonus = 0f;

            if (Data.isSupportTower) return;

            for (int i = 0; i < ActiveTowers.Count; i++)
            {
                TowerController other = ActiveTowers[i];
                if (other != null && other != this && other.Data != null && other.Data.isSupportTower && !other.IsDisabled)
                {
                    float dist = Vector2.Distance(transform.position, other.transform.position);
                    if (dist <= other.CurrentTierData.range)
                    {
                        _relayRangeBonus = Mathf.Max(_relayRangeBonus, other.CurrentTierData.auraRangeBonusPercent);
                        _relayFireRateBonus = Mathf.Max(_relayFireRateBonus, other.CurrentTierData.auraFireRateBonusPercent);
                    }
                }
            }
        }

        public EnemyController AcquireTarget()
        {
            float maxRange = GetEffectiveRange();
            EnemyController best = null;
            float bestScore = 0f;

            for (int i = 0; i < EnemyController.ActiveEnemies.Count; i++)
            {
                EnemyController candidate = EnemyController.ActiveEnemies[i];
                if (candidate == null || candidate.IsDead || candidate.IsStealthed)
                    continue;

                float distToTower = Vector2.Distance(transform.position, candidate.transform.position);
                if (distToTower > maxRange)
                    continue;

                float score = Priority switch
                {
                    TargetPriority.First => -candidate.GetRemainingDistanceToGoal(),
                    TargetPriority.Strongest => candidate.CurrentHealth,
                    TargetPriority.Closest => -distToTower,
                    TargetPriority.Last => candidate.GetRemainingDistanceToGoal(),
                    _ => -candidate.GetRemainingDistanceToGoal()
                };

                if (best == null || score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private void AimTurretAt(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            _turretTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ExecuteAttack(EnemyController primaryTarget)
        {
            TowerTierData tier = CurrentTierData;

            if (_turretAnimator != null && _turretFrames != null && _turretFrames.Length > 1)
            {
                _turretAnimator.TriggerOneShotFrame(_turretFrames[1], 0.11f);
            }

            if (Data.isContinuousCone)
            {
                // Hellhound Flame-Bunker continuous AoE cone
                ExecuteFlameConeAttack(primaryTarget.transform.position, tier);
                return;
            }

            if (Data.category == TowerCategory.LascannonTurret)
            {
                // Instant hitscan Lascannon beam for crisp sniper responsiveness
                DrawLaserBeam(transform.position, primaryTarget.transform.position, Data.accentColor, 0.14f, 0.12f);
                if (tier.armorVulnerabilityBonus > 0f)
                {
                    primaryTarget.ApplyArmorVulnerability(tier.armorVulnerabilityBonus, tier.armorVulnerabilityDuration);
                }
                primaryTarget.TakeDamage(tier.damage, Data.damageType, Data.ignoresCover);
                return;
            }

            // Spawn kinetic bolter shell or Basilisk artillery projectile
            GameObject projObj = new GameObject($"Proj_{Data.displayName}");
            projObj.transform.position = transform.position;
            Projectile proj = projObj.AddComponent<Projectile>();
            proj.Initialize(
                primaryTarget,
                Data.projectileSpeed,
                tier.damage,
                Data.damageType,
                Data.ignoresCover,
                tier.areaParameter,
                tier.slowFactor,
                tier.slowDuration,
                tier.armorVulnerabilityBonus,
                tier.armorVulnerabilityDuration,
                Data.accentColor,
                Data.isGlobalArtillery);
        }

        private void ExecuteFlameConeAttack(Vector3 aimPosition, TowerTierData tier)
        {
            Vector2 forward = ((Vector2)(aimPosition - transform.position)).normalized;
            float maxRange = GetEffectiveRange();
            float halfAngle = Mathf.Max(18f, tier.areaParameter);

            for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController ork = EnemyController.ActiveEnemies[i];
                if (ork == null || ork.IsDead || ork.IsStealthed) continue;

                Vector2 toOrk = (Vector2)(ork.transform.position - transform.position);
                float dist = toOrk.magnitude;
                if (dist <= maxRange && Vector2.Angle(forward, toOrk) <= halfAngle)
                {
                    ork.TakeDamage(tier.damage, DamageType.Flame, ignoresCover: true);
                }
            }

            // Render promethium flame jet visual
            Vector3 coneEnd = transform.position + (Vector3)(forward * maxRange * 0.85f);
            DrawLaserBeam(transform.position, coneEnd, new Color(1f, 0.45f, 0.08f, 0.75f), 0.18f, 0.55f);
        }

        private void DrawLaserBeam(Vector3 start, Vector3 end, Color col, float duration, float endWidth)
        {
            if (_laserOrConeRenderer == null) return;
            _laserOrConeRenderer.enabled = true;
            _laserOrConeRenderer.positionCount = 2;
            _laserOrConeRenderer.SetPosition(0, start);
            _laserOrConeRenderer.SetPosition(1, end);
            _laserOrConeRenderer.startWidth = 0.08f;
            _laserOrConeRenderer.endWidth = endWidth;
            _laserOrConeRenderer.startColor = col;
            _laserOrConeRenderer.endColor = new Color(col.r, col.g, col.b, 0.25f);
            _beamDisplayTimer = duration;
        }

        /// <summary>
        /// Checks if a world position is inside any active Astropathic Relay's stealth-detection aura.
        /// </summary>
        public static bool IsPositionRevealedByRelay(Vector3 worldPos)
        {
            for (int i = 0; i < ActiveTowers.Count; i++)
            {
                TowerController t = ActiveTowers[i];
                if (t != null && t.Data != null && t.Data.isSupportTower && !t.IsDisabled &&
                    t.CurrentTierData.revealsStealth)
                {
                    if (Vector2.Distance(worldPos, t.transform.position) <= t.GetEffectiveRange())
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Used by the Wave 20 Gargant / Warboss in Mega Armor to temporarily disable adjacent towers.
        /// </summary>
        public static int DisableNearbyTowers(Vector3 bossPos, float radius, float duration, int maxCount)
        {
            int count = 0;
            for (int i = 0; i < ActiveTowers.Count && count < maxCount; i++)
            {
                TowerController t = ActiveTowers[i];
                if (t != null && Vector2.Distance(bossPos, t.transform.position) <= radius)
                {
                    t.DisableTower(duration);
                    count++;
                }
            }
            return count;
        }
    }
}
