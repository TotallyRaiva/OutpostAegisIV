using OutpostAegis.Core;
using OutpostAegis.Data;
using OutpostAegis.Enemies;
using OutpostAegis.Visuals;
using UnityEngine;

namespace OutpostAegis.Towers
{
    /// <summary>
    /// Handles Heavy Bolter kinetic rounds, Lascannon sniper beams, and Basilisk Earthshaker shells.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private Vector3 _targetPosition;
        private EnemyController _targetEnemy;
        private float _speed;
        private float _damage;
        private DamageType _damageType;
        private bool _ignoresCover;
        private float _splashRadius;
        private float _slowFactor;
        private float _slowDuration;
        private float _armorDebuff;
        private float _armorDebuffDuration;
        private bool _isArtilleryShell;

        public void Initialize(
            EnemyController target,
            float speed,
            float damage,
            DamageType damageType,
            bool ignoresCover,
            float splashRadius,
            float slowFactor,
            float slowDuration,
            float armorDebuff,
            float armorDebuffDuration,
            Color projectileColor,
            bool isArtilleryShell)
        {
            _targetEnemy = target;
            _targetPosition = target != null ? target.transform.position : transform.position;
            _speed = Mathf.Max(6f, speed);
            _damage = damage;
            _damageType = damageType;
            _ignoresCover = ignoresCover;
            _splashRadius = splashRadius;
            _slowFactor = slowFactor;
            _slowDuration = slowDuration;
            _armorDebuff = armorDebuff;
            _armorDebuffDuration = armorDebuffDuration;
            _isArtilleryShell = isArtilleryShell;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSpriteGenerator.GetCircleSprite(12, false);
            sr.color = projectileColor;
            sr.sortingOrder = 20;

            float scale = isArtilleryShell ? 0.8f : (_damageType == DamageType.ArmorPiercing ? 0.55f : 0.4f);
            transform.localScale = Vector3.one * scale;
        }

        private void Update()
        {
            if (!_isArtilleryShell && _targetEnemy != null && !_targetEnemy.IsDead)
            {
                _targetPosition = _targetEnemy.transform.position;
            }

            Vector3 dir = _targetPosition - transform.position;
            float step = _speed * Time.deltaTime;

            if (dir.magnitude <= step)
            {
                Detonate();
                return;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            transform.position += dir.normalized * step;
        }

        private void Detonate()
        {
            if (_splashRadius > 0.05f)
            {
                // Explosive splash (Basilisk Artillery Pit)
                SoundManager.Play(SfxType.ArtilleryExplosion, 0.78f);
                CameraController.TriggerShake(0.16f, 0.24f);
                FloatingTextManager.SpawnPixelBurst(
                    _targetPosition, new Color(1f, 0.58f, 0.16f), count: 10, speed: 3.4f);

                for (int i = EnemyController.ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyController enemy = EnemyController.ActiveEnemies[i];
                    if (enemy != null && !enemy.IsDead &&
                        Vector2.Distance(_targetPosition, enemy.transform.position) <= _splashRadius)
                    {
                        ApplyHitEffects(enemy);
                    }
                }
                SpawnImpactFlash(_targetPosition, _splashRadius * 2f, new Color(1f, 0.55f, 0.15f, 0.55f));
            }
            else if (_targetEnemy != null && !_targetEnemy.IsDead)
            {
                ApplyHitEffects(_targetEnemy);
            }

            Destroy(gameObject);
        }

        private void ApplyHitEffects(EnemyController enemy)
        {
            if (_armorDebuff > 0f)
            {
                enemy.ApplyArmorVulnerability(_armorDebuff, _armorDebuffDuration);
            }

            enemy.TakeDamage(_damage, _damageType, _ignoresCover);

            if (_slowFactor > 0f && !enemy.IsDead)
            {
                enemy.ApplySlow(_slowFactor, _slowDuration);
            }
        }

        public static void SpawnImpactFlash(Vector3 pos, float diameter, Color color)
        {
            GameObject flash = new GameObject("VFX_ImpactFlash");
            flash.transform.position = pos;
            flash.transform.localScale = Vector3.one * diameter;

            SpriteRenderer sr = flash.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSpriteGenerator.GetCircleSprite(48, false);
            sr.color = color;
            sr.sortingOrder = 18;

            Destroy(flash, 0.18f);
        }
    }
}
