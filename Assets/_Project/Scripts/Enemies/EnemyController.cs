using System;
using System.Collections.Generic;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Map;
using OutpostAegis.Towers;
using OutpostAegis.Visuals;
using UnityEngine;

namespace OutpostAegis.Enemies
{
    /// <summary>
    /// Controls Ork unit movement along canyon paths, stealth state (Kommandos),
    /// terrain bypass (Warbikers/Stormboyz), melee engagement with Drop Pod Space Marines,
    /// and Wave 20 Boss abilities (WAAAGH! Roar & Tower Disable).
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        public static readonly List<EnemyController> ActiveEnemies = new List<EnemyController>();

        public EnemyDataSO Data { get; private set; }
        public WaypointPath Path { get; private set; }
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public int CurrentWaypointIndex { get; private set; }
        public bool IsDead { get; private set; }

        // Stealth & Melee Blocking
        public bool IsRevealedByRelay { get; private set; }
        public bool IsBlockedInMelee { get; private set; }
        public bool IsStealthed => Data != null && Data.isStealthUnit && !IsRevealedByRelay && !IsBlockedInMelee;

        // Status Effects
        private float _slowFactor;
        private float _slowTimer;
        private float _speedBuffMultiplier = 1f;
        private float _speedBuffTimer;
        private float _armorDebuffBonus;
        private float _armorDebuffTimer;

        // Boss timers
        private float _bossRoarTimer;
        private float _bossDisableTimer;

        // Visual components
        private SpriteRenderer _spriteRenderer;
        private Transform _healthBarFill;
        private SpriteRenderer _healthBarRenderer;

        public event Action<EnemyController> OnEnemyDefeated;

        private void OnEnable()
        {
            if (!ActiveEnemies.Contains(this))
                ActiveEnemies.Add(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        public void Initialize(EnemyDataSO data, WaypointPath path)
        {
            Data = data;
            Path = path;
            MaxHealth = data.maxHealth;
            CurrentHealth = MaxHealth;
            CurrentWaypointIndex = 0;
            IsDead = false;
            IsBlockedInMelee = false;
            IsRevealedByRelay = false;

            _bossRoarTimer = data.bossRoarInterval * 0.6f;
            _bossDisableTimer = data.bossTowerDisableInterval * 0.75f;

            if (Path != null && Path.Count > 0)
            {
                transform.position = Path.GetWaypointPosition(0);
                CurrentWaypointIndex = Mathf.Min(1, Path.Count - 1);
            }

            SetupVisuals();
        }

        private void SetupVisuals()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            Sprite[] walkFrames = PixelSpriteGenerator.GetEnemyWalkFrames(Data.category);
            _spriteRenderer.sprite = Data.sprite != null ? Data.sprite : walkFrames[0];
            _spriteRenderer.sortingOrder = 10;
            transform.localScale = Vector3.one * Data.visualScale;

            if (Data.sprite == null && walkFrames.Length > 1)
            {
                PixelSpriteAnimator anim = GetComponent<PixelSpriteAnimator>();
                if (anim == null) anim = gameObject.AddComponent<PixelSpriteAnimator>();
                float animFps = Mathf.Clamp(Data.moveSpeed * 4.2f, 5f, 12f);
                anim.SetLoopAnimation(walkFrames, animFps, randomStartFrame: true);
            }

            // Create world-space pixel health bar
            Transform existingBar = transform.Find("HealthBarBg");
            if (existingBar == null)
            {
                GameObject bgObj = new GameObject("HealthBarBg");
                bgObj.transform.SetParent(transform, false);
                bgObj.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                bgObj.transform.localScale = new Vector3(3.2f, 0.45f, 1f);

                SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
                bgSr.sprite = PixelSpriteGenerator.GetWhiteSquareSprite();
                bgSr.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                bgSr.sortingOrder = 25;

                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(bgObj.transform, false);
                fillObj.transform.localPosition = Vector3.zero;
                fillObj.transform.localScale = Vector3.one;

                _healthBarRenderer = fillObj.AddComponent<SpriteRenderer>();
                _healthBarRenderer.sprite = PixelSpriteGenerator.GetWhiteSquareSprite();
                _healthBarRenderer.color = new Color(0.2f, 0.9f, 0.25f, 0.95f);
                _healthBarRenderer.sortingOrder = 26;
                _healthBarFill = fillObj.transform;
            }
        }

        private void Update()
        {
            if (IsDead || Data == null) return;

            UpdateStatusTimers(Time.deltaTime);
            UpdateStealthDetection();

            if (Data.isBoss)
            {
                UpdateBossAbilities(Time.deltaTime);
            }

            if (!IsBlockedInMelee)
            {
                MoveAlongPath(Time.deltaTime);
            }

            UpdateVisualAppearance();
        }

        private void UpdateStatusTimers(float dt)
        {
            if (_slowTimer > 0f)
            {
                _slowTimer -= dt;
                if (_slowTimer <= 0f) _slowFactor = 0f;
            }

            if (_speedBuffTimer > 0f)
            {
                _speedBuffTimer -= dt;
                if (_speedBuffTimer <= 0f) _speedBuffMultiplier = 1f;
            }

            if (_armorDebuffTimer > 0f)
            {
                _armorDebuffTimer -= dt;
                if (_armorDebuffTimer <= 0f) _armorDebuffBonus = 0f;
            }
        }

        private void UpdateStealthDetection()
        {
            if (!Data.isStealthUnit)
            {
                IsRevealedByRelay = true;
                return;
            }

            IsRevealedByRelay = TowerController.IsPositionRevealedByRelay(transform.position);
        }

        private void MoveAlongPath(float dt)
        {
            if (Path == null || Path.Count == 0) return;

            Vector3 targetPos = Path.GetWaypointPosition(CurrentWaypointIndex);
            float effectiveSpeed = Data.moveSpeed * (1f - _slowFactor) * _speedBuffMultiplier;

            // Standard ground units slow down slightly in rough canyon obstacle segments,
            // while Warbikers and Stormboyz bypass terrain obstacles completely.
            if (!Data.bypassesTerrainObstacles && Path.IsRoughTerrainSegment(CurrentWaypointIndex))
            {
                effectiveSpeed *= 0.8f;
            }
            else if (Data.bypassesTerrainObstacles && Path.IsRoughTerrainSegment(CurrentWaypointIndex))
            {
                effectiveSpeed *= 1.2f; // Stormboyz rocket-boost over rough terrain
            }

            Vector3 dir = targetPos - transform.position;
            if (dir.x != 0f && _spriteRenderer != null)
            {
                _spriteRenderer.flipX = dir.x < 0f;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, effectiveSpeed * dt);

            if (Vector3.Distance(transform.position, targetPos) <= 0.05f)
            {
                CurrentWaypointIndex++;
                if (CurrentWaypointIndex >= Path.Count)
                {
                    ReachPlasmaReactor();
                }
            }
        }

        private void UpdateBossAbilities(float dt)
        {
            // 1. WAAAGH! Roar: Speeds up nearby Orks
            _bossRoarTimer -= dt;
            if (_bossRoarTimer <= 0f)
            {
                _bossRoarTimer = Data.bossRoarInterval;
                PerformBossWaaaghRoar();
            }

            // 2. Scrap-EMP / Mega-Slam: Temporarily disables adjacent towers
            _bossDisableTimer -= dt;
            if (_bossDisableTimer <= 0f)
            {
                _bossDisableTimer = Data.bossTowerDisableInterval;
                PerformBossTowerDisable();
            }
        }

        private void PerformBossWaaaghRoar()
        {
            int buffedCount = 0;
            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                EnemyController ork = ActiveEnemies[i];
                if (ork != null && !ork.IsDead &&
                    Vector2.Distance(transform.position, ork.transform.position) <= Data.bossRoarRadius)
                {
                    ork.ApplySpeedBuff(Data.bossRoarSpeedMultiplier, Data.bossRoarDuration);
                    buffedCount++;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.BroadcastVox(
                    $"WAAAGH! ROAR! {Data.displayName} frenzies {buffedCount} nearby Orks (+45% speed)!");
            }
        }

        private void PerformBossTowerDisable()
        {
            int disabled = TowerController.DisableNearbyTowers(
                transform.position,
                Data.bossTowerDisableRadius,
                Data.bossTowerDisableDuration,
                Data.bossMaxTowersDisabled);

            if (disabled > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.BroadcastVox(
                    $"ALERT: {Data.displayName} unleashed a Mega-Slam shockwave, disabling {disabled} nearby tower(s)!");
            }
        }

        public void TakeDamage(float rawDamage, DamageType damageType, bool ignoresCover = false)
        {
            if (IsDead) return;

            bool inCover = CoverZone.IsPositionInCover(transform.position);
            float finalDamage = DamageMatrix.CalculateFinalDamage(
                rawDamage,
                damageType,
                Data.armorType,
                inCover,
                ignoresCover,
                1f + _armorDebuffBonus);

            CurrentHealth -= finalDamage;

            if (_healthBarFill != null)
            {
                float ratio = Mathf.Clamp01(CurrentHealth / MaxHealth);
                _healthBarFill.localScale = new Vector3(ratio, 1f, 1f);
                _healthBarFill.localPosition = new Vector3((ratio - 1f) * 0.12f, 0f, 0f);
                if (_healthBarRenderer != null)
                {
                    _healthBarRenderer.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.25f), ratio);
                }
            }

            if (CurrentHealth <= 0f)
            {
                Die(rewardPlayer: true);
            }
        }

        public void ApplySlow(float slowFactor, float duration)
        {
            if (slowFactor <= 0f) return;
            _slowFactor = Mathf.Max(_slowFactor, Mathf.Clamp(slowFactor, 0f, 0.75f));
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        public void ApplySpeedBuff(float multiplier, float duration)
        {
            _speedBuffMultiplier = Mathf.Max(_speedBuffMultiplier, multiplier);
            _speedBuffTimer = Mathf.Max(_speedBuffTimer, duration);
        }

        public void ApplyArmorVulnerability(float bonusDamageRatio, float duration)
        {
            _armorDebuffBonus = Mathf.Max(_armorDebuffBonus, bonusDamageRatio);
            _armorDebuffTimer = Mathf.Max(_armorDebuffTimer, duration);
        }

        public void SetMeleeBlocked(bool blocked)
        {
            IsBlockedInMelee = blocked;
        }

        public float GetRemainingDistanceToGoal()
        {
            if (Path == null) return float.MaxValue;
            return Path.CalculateRemainingDistance(transform.position, CurrentWaypointIndex);
        }

        private void UpdateVisualAppearance()
        {
            if (_spriteRenderer == null) return;

            Color c = Color.white;
            if (_speedBuffTimer > 0f)
            {
                c = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.2f), 0.5f);
            }
            else if (_slowTimer > 0f)
            {
                c = Color.Lerp(Color.white, new Color(0.55f, 0.75f, 1f), 0.5f);
            }

            // Kommandos render translucent while stealthed
            c.a = IsStealthed ? 0.35f : 1.0f;
            _spriteRenderer.color = c;
        }

        private void ReachPlasmaReactor()
        {
            if (IsDead) return;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DamagePlasmaReactor(Data.reactorDamage);
            }
            Die(rewardPlayer: false);
        }

        private void Die(bool rewardPlayer)
        {
            if (IsDead) return;
            IsDead = true;

            if (rewardPlayer && GameManager.Instance != null)
            {
                GameManager.Instance.AddRequisition(Data.requisitionReward);
            }

            OnEnemyDefeated?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
