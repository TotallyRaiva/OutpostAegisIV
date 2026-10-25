using System.Collections;
using OutpostAegis.Data;
using UnityEngine;

namespace OutpostAegis.Visuals
{
    /// <summary>
    /// Spawns world-space floating damage numbers, critical/armor callouts, Requisition kill bounties,
    /// and 32 PPU pixel shrapnel/splatter bursts.
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;
            GameObject obj = new GameObject("FloatingTextManager");
            Instance = obj.AddComponent<FloatingTextManager>();
        }

        public static void SpawnDamageNumber(
            Vector3 worldPos,
            float damage,
            DamageType damageType,
            float effectivenessMultiplier)
        {
            int rounded = Mathf.RoundToInt(damage);
            if (rounded <= 0) return;

            EnsureInstance();

            Color col = new Color(0.98f, 0.94f, 0.78f);
            int fontSize = 28;
            string suffix = "";

            if (effectivenessMultiplier >= 1.45f)
            {
                // Super-effective (e.g. Lascannon vs Heavy Vehicle or Bolter vs Fast Biker)
                col = damageType == DamageType.ArmorPiercing
                    ? new Color(0.35f, 0.95f, 1.0f)
                    : new Color(1.0f, 0.85f, 0.25f);
                fontSize = 36;
                suffix = "!";
            }
            else if (effectivenessMultiplier <= 0.55f)
            {
                // Armor resisted
                col = new Color(0.62f, 0.65f, 0.70f);
                fontSize = 22;
            }
            else if (damageType == DamageType.Flame)
            {
                col = new Color(1.0f, 0.52f, 0.14f);
            }
            else if (damageType == DamageType.Explosive)
            {
                col = new Color(1.0f, 0.68f, 0.22f);
                fontSize = 32;
            }

            Vector3 jitter = new Vector3(Random.Range(-0.22f, 0.22f), Random.Range(0.25f, 0.45f), 0f);
            Instance.StartCoroutine(Instance.AnimateFloatingTextRoutine(
                worldPos + jitter, $"{rounded}{suffix}", col, fontSize, 0.65f, 1.25f));
        }

        public static void SpawnCallout(
            Vector3 worldPos,
            string message,
            Color color,
            int fontSize = 32,
            float duration = 0.95f)
        {
            EnsureInstance();
            Instance.StartCoroutine(Instance.AnimateFloatingTextRoutine(
                worldPos + new Vector3(0f, 0.55f, 0f), message, color, fontSize, duration, 1.0f));
        }

        public static void SpawnPixelBurst(Vector3 worldPos, Color color, int count = 7, float speed = 2.5f)
        {
            EnsureInstance();
            Sprite sq = PixelSpriteGenerator.GetWhiteSquareSprite();

            for (int i = 0; i < count; i++)
            {
                GameObject p = new GameObject("VFX_PixelParticle");
                p.transform.position = worldPos;
                p.transform.localScale = Vector3.one * Random.Range(0.35f, 0.65f);

                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = sq;
                sr.color = color;
                sr.sortingOrder = 22;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(speed * 0.4f, speed);
                Instance.StartCoroutine(Instance.AnimateParticleRoutine(p.transform, sr, vel, 0.32f));
            }
        }

        private IEnumerator AnimateFloatingTextRoutine(
            Vector3 startPos,
            string content,
            Color color,
            int fontSize,
            float duration,
            float riseSpeed)
        {
            GameObject root = new GameObject("FloatingText");
            root.transform.position = startPos;

            TextMesh mainTm = root.AddComponent<TextMesh>();
            mainTm.text = content;
            mainTm.fontSize = fontSize;
            mainTm.characterSize = 0.085f;
            mainTm.anchor = TextAnchor.MiddleCenter;
            mainTm.alignment = TextAlignment.Center;
            mainTm.fontStyle = FontStyle.Bold;
            mainTm.color = color;

            MeshRenderer mainMr = root.GetComponent<MeshRenderer>();
            if (mainMr != null) mainMr.sortingOrder = 40;

            // Crisp dark pixel drop-shadow
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(root.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.03f, -0.03f, 0.01f);
            TextMesh shadowTm = shadowObj.AddComponent<TextMesh>();
            shadowTm.text = content;
            shadowTm.fontSize = fontSize;
            shadowTm.characterSize = 0.085f;
            shadowTm.anchor = TextAnchor.MiddleCenter;
            shadowTm.alignment = TextAlignment.Center;
            shadowTm.fontStyle = FontStyle.Bold;
            shadowTm.color = new Color(0f, 0f, 0f, 0.85f);

            MeshRenderer shadowMr = shadowObj.GetComponent<MeshRenderer>();
            if (shadowMr != null) shadowMr.sortingOrder = 39;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float norm = Mathf.Clamp01(elapsed / duration);

                root.transform.position = startPos + new Vector3(0f, elapsed * riseSpeed, 0f);
                float alpha = norm < 0.6f ? 1f : 1f - ((norm - 0.6f) / 0.4f);

                mainTm.color = new Color(color.r, color.g, color.b, alpha);
                shadowTm.color = new Color(0f, 0f, 0f, alpha * 0.85f);
                yield return null;
            }

            Destroy(root);
        }

        private IEnumerator AnimateParticleRoutine(
            Transform tr,
            SpriteRenderer sr,
            Vector2 velocity,
            float lifetime)
        {
            float elapsed = 0f;
            Color baseCol = sr.color;

            while (elapsed < lifetime && tr != null)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                velocity.y -= 4.5f * dt; // Gravity
                tr.position += (Vector3)(velocity * dt);

                float alpha = 1f - Mathf.Clamp01(elapsed / lifetime);
                sr.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
                yield return null;
            }

            if (tr != null)
                Destroy(tr.gameObject);
        }
    }
}
