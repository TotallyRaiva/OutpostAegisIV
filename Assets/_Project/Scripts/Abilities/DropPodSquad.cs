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
    /// Represents a Drop Pod Assault squad of Adeptus Astartes deployed onto the causeway/path
    /// for 15 seconds to physically block and fight Ork forces in melee & bolter fire.
    /// </summary>
    public class DropPodSquad : MonoBehaviour
    {
        private AbilityDataSO _abilityData;
        private float _remainingLifetime;
        private float _attackTickTimer;
        private readonly List<EnemyController> _blockedOrks = new List<EnemyController>();
        private readonly List<Transform> _marineVisuals = new List<Transform>();

        public void Initialize(AbilityDataSO data)
        {
            _abilityData = data;
            _remainingLifetime = data.squadLifetimeSeconds;

            SoundManager.Play(SfxType.DropPodSlam, 0.90f);
            CameraController.TriggerShake(0.30f, 0.38f);
            FloatingTextManager.SpawnCallout(
                transform.position, "FOR THE EMPEROR!", new Color(0.35f, 0.78f, 1.0f), 34, 1.1f);
            FloatingTextManager.SpawnPixelBurst(
                transform.position, new Color(0.35f, 0.78f, 1.0f), count: 12, speed: 3.2f);

            // Impact shockwave damage when the Drop Pod slams down
            for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController ork = EnemyController.ActiveEnemies[i];
                if (ork != null && !ork.IsDead &&
                    Vector2.Distance(transform.position, ork.transform.position) <= data.effectRadius)
                {
                    ork.TakeDamage(data.impactDamage, DamageType.Explosive, ignoresCover: true);
                }
            }

            Projectile.SpawnImpactFlash(transform.position, data.effectRadius * 2f, new Color(0.25f, 0.6f, 1.0f, 0.55f));
            CreateDropPodAndMarinesVisuals();
        }

        private void CreateDropPodAndMarinesVisuals()
        {
            // Drop Pod central hull
            SpriteRenderer podSr = gameObject.AddComponent<SpriteRenderer>();
            podSr.sprite = PixelSpriteGenerator.GetTowerBaseSprite(TowerCategory.LascannonTurret, 2);
            podSr.color = new Color(0.22f, 0.45f, 0.88f);
            podSr.sortingOrder = 8;

            // 3 Tactical Space Marines positioned in a triangle around the pod
            int count = Mathf.Max(1, _abilityData.marineCount);
            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / count) + 30f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * 0.65f, Mathf.Sin(angle) * 0.55f, 0f);

                GameObject marineObj = new GameObject($"SpaceMarine_{i + 1}");
                marineObj.transform.SetParent(transform, false);
                marineObj.transform.localPosition = offset;

                SpriteRenderer sr = marineObj.AddComponent<SpriteRenderer>();
                sr.sprite = PixelSpriteGenerator.GetSpaceMarineSprite();
                sr.sortingOrder = 12;
                _marineVisuals.Add(marineObj.transform);
            }
        }

        private void Update()
        {
            if (_abilityData == null) return;

            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                ReleaseAllBlockedEnemies();
                Destroy(gameObject);
                return;
            }

            EngageAndBlockNearbyEnemies();

            _attackTickTimer -= Time.deltaTime;
            if (_attackTickTimer <= 0f)
            {
                _attackTickTimer = 0.25f;
                DealSquadDamage(0.25f);
            }
        }

        private void EngageAndBlockNearbyEnemies()
        {
            // Clean up dead enemies from blocked list
            for (int i = _blockedOrks.Count - 1; i >= 0; i--)
            {
                if (_blockedOrks[i] == null || _blockedOrks[i].IsDead)
                {
                    _blockedOrks.RemoveAt(i);
                }
            }

            // Physically block new Orks entering the Drop Pod zone (also forces Kommandos out of stealth!)
            for (int i = 0; i < EnemyController.ActiveEnemies.Count; i++)
            {
                if (_blockedOrks.Count >= _abilityData.maxBlockedEnemies) break;

                EnemyController ork = EnemyController.ActiveEnemies[i];
                if (ork == null || ork.IsDead || _blockedOrks.Contains(ork)) continue;

                float dist = Vector2.Distance(transform.position, ork.transform.position);
                if (dist <= _abilityData.squadEngagementRadius * 0.75f)
                {
                    ork.SetMeleeBlocked(true);
                    _blockedOrks.Add(ork);
                }
            }
        }

        private void DealSquadDamage(float dt)
        {
            // 1. Chainsword melee damage to all blocked enemies
            float meleeDamagePerTick = _abilityData.squadMeleeDPS * dt;
            for (int i = _blockedOrks.Count - 1; i >= 0; i--)
            {
                EnemyController ork = _blockedOrks[i];
                if (ork != null && !ork.IsDead)
                {
                    ork.TakeDamage(meleeDamagePerTick, DamageType.Kinetic, ignoresCover: true);
                }
            }

            // 2. Bolter fire against nearest enemy in engagement radius
            float bolterDamagePerTick = _abilityData.squadBolterDPS * dt;
            for (int i = 0; i < EnemyController.ActiveEnemies.Count; i++)
            {
                EnemyController ork = EnemyController.ActiveEnemies[i];
                if (ork != null && !ork.IsDead && !ork.IsStealthed &&
                    Vector2.Distance(transform.position, ork.transform.position) <= _abilityData.squadEngagementRadius)
                {
                    ork.TakeDamage(bolterDamagePerTick, DamageType.Kinetic, ignoresCover: false);
                    break;
                }
            }
        }

        private void ReleaseAllBlockedEnemies()
        {
            for (int i = 0; i < _blockedOrks.Count; i++)
            {
                if (_blockedOrks[i] != null && !_blockedOrks[i].IsDead)
                {
                    _blockedOrks[i].SetMeleeBlocked(false);
                }
            }
            _blockedOrks.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAllBlockedEnemies();
        }
    }
}
