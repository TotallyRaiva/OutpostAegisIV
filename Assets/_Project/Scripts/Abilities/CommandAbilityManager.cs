using System.Collections;
using System.Collections.Generic;
using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Enemies;
using OutpostAegis.Towers;
using OutpostAegis.Visuals;
using UnityEngine;

namespace OutpostAegis.Abilities
{
    /// <summary>
    /// Manages Command Point tactical strikes:
    /// 1. Orbital Bombardment (High-damage lance strike on targeted area)
    /// 2. Drop Pod Assault (Deploys temporary squad of Space Marines for 15s to block & fight Orks)
    /// </summary>
    public class CommandAbilityManager : MonoBehaviour
    {
        public static CommandAbilityManager Instance { get; private set; }

        public AbilityDataSO ActiveTargetingAbility { get; private set; }
        private readonly Dictionary<CommandAbilityType, float> _cooldownTimers = new Dictionary<CommandAbilityType, float>();

        private GameObject _targetingReticle;
        private SpriteRenderer _reticleRenderer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateTargetingReticle();
        }

        private void CreateTargetingReticle()
        {
            _targetingReticle = new GameObject("AbilityTargetingReticle");
            _targetingReticle.transform.SetParent(transform, false);
            _reticleRenderer = _targetingReticle.AddComponent<SpriteRenderer>();
            _reticleRenderer.sprite = PixelSpriteGenerator.GetCircleSprite(96, hollowRing: true);
            _reticleRenderer.sortingOrder = 30;
            _targetingReticle.SetActive(false);
        }

        private void Update()
        {
            // Tick cooldowns
            List<CommandAbilityType> keys = new List<CommandAbilityType>(_cooldownTimers.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                CommandAbilityType key = keys[i];
                if (_cooldownTimers[key] > 0f)
                {
                    _cooldownTimers[key] = Mathf.Max(0f, _cooldownTimers[key] - Time.deltaTime);
                }
            }

            if (ActiveTargetingAbility != null && Camera.main != null)
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0f;
                _targetingReticle.transform.position = worldPos;

                // Left click / tap on map executes the selected Command Ability
                if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
                {
                    ExecuteAbilityAt(ActiveTargetingAbility, worldPos);
                    CancelTargeting();
                }
                else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelTargeting();
                }
            }
        }

        public float GetRemainingCooldown(CommandAbilityType type)
        {
            return _cooldownTimers.TryGetValue(type, out float rem) ? rem : 0f;
        }

        public bool CanActivateAbility(AbilityDataSO ability)
        {
            if (ability == null || GameManager.Instance == null) return false;
            if (GetRemainingCooldown(ability.abilityType) > 0f) return false;
            return GameManager.Instance.CanAffordCommandPoints(ability.commandPointCost);
        }

        public void BeginTargetingAbility(AbilityDataSO ability)
        {
            if (!CanActivateAbility(ability)) return;

            ActiveTargetingAbility = ability;
            _targetingReticle.SetActive(true);
            float diameter = ability.effectRadius * 2f;
            _targetingReticle.transform.localScale = new Vector3(diameter / 3f, diameter / 3f, 1f);
            _reticleRenderer.color = ability.indicatorColor;

            GameManager.Instance.BroadcastVox($"TARGETING {ability.displayName.ToUpper()}: Tap battlefield location to confirm coordinates.");
        }

        public void CancelTargeting()
        {
            ActiveTargetingAbility = null;
            if (_targetingReticle != null)
                _targetingReticle.SetActive(false);
        }

        public void ExecuteAbilityAt(AbilityDataSO ability, Vector3 worldPos)
        {
            if (!CanActivateAbility(ability)) return;
            if (!GameManager.Instance.SpendCommandPoints(ability.commandPointCost)) return;

            _cooldownTimers[ability.abilityType] = ability.cooldownSeconds;

            if (ability.abilityType == CommandAbilityType.OrbitalBombardment)
            {
                StartCoroutine(OrbitalBombardmentRoutine(ability, worldPos));
            }
            else if (ability.abilityType == CommandAbilityType.DropPodAssault)
            {
                DeployDropPodSquad(ability, worldPos);
            }
        }

        private IEnumerator OrbitalBombardmentRoutine(AbilityDataSO ability, Vector3 targetPos)
        {
            GameManager.Instance.BroadcastVox("ORBITAL LANCE LOCKED: Stand clear of bombardment zone!");
            Projectile.SpawnImpactFlash(targetPos, ability.effectRadius * 2f, new Color(1f, 0.15f, 0.15f, 0.35f));

            yield return new WaitForSeconds(ability.strikeWarningDelay);

            // Orbital laser column visual
            GameObject beamObj = new GameObject("VFX_OrbitalLance");
            LineRenderer lr = beamObj.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 28;
            lr.positionCount = 2;
            lr.SetPosition(0, targetPos + new Vector3(0f, 14f, 0f));
            lr.SetPosition(1, targetPos);
            lr.startWidth = 1.2f;
            lr.endWidth = 1.6f;
            lr.startColor = new Color(1f, 0.9f, 0.4f, 0.95f);
            lr.endColor = new Color(1f, 0.3f, 0.1f, 0.95f);
            Destroy(beamObj, 0.55f);

            Projectile.SpawnImpactFlash(targetPos, ability.effectRadius * 2.2f, new Color(1f, 0.7f, 0.2f, 0.8f));

            for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController ork = EnemyController.ActiveEnemies[i];
                if (ork != null && !ork.IsDead &&
                    Vector2.Distance(targetPos, ork.transform.position) <= ability.effectRadius)
                {
                    ork.TakeDamage(ability.impactDamage, ability.damageType, ignoresCover: true);
                }
            }
        }

        private void DeployDropPodSquad(AbilityDataSO ability, Vector3 targetPos)
        {
            GameManager.Instance.BroadcastVox("DROP POD ASSAULT: Adeptus Astartes Tactical Squad deployed for 15 seconds!");
            GameObject squadObj = new GameObject("DropPod_AstartesSquad");
            squadObj.transform.position = targetPos;
            DropPodSquad squad = squadObj.AddComponent<DropPodSquad>();
            squad.Initialize(ability);
        }

        private static bool IsPointerOverUI()
        {
            // Prevent clicks in top/bottom HUD strips from accidentally firing abilities
            float y = Input.mousePosition.y;
            return y < Screen.height * 0.14f || y > Screen.height * 0.88f;
        }
    }
}
