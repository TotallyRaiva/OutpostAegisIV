using System.Collections.Generic;
using UnityEngine;

namespace OutpostAegis.Core
{
    public enum SfxType
    {
        BolterShot,
        LascannonZap,
        FlamerWhoosh,
        ArtilleryFire,
        ArtilleryExplosion,
        OrbitalLockOn,
        OrbitalStrike,
        DropPodSlam,
        OrkDeath,
        WaaaghRoar,
        TowerConstruct,
        WaveStartHorn
    }

    /// <summary>
    /// Procedural 16-bit Grimdark Audio Synthesizer.
    /// Generates all Warhammer 40k weapon, explosion, Ork WAAAGH!, and UI AudioClips in memory at startup
    /// so every action has punchy sound effects with zero external audio file dependencies.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const int SampleRate = 44100;
        private readonly Dictionary<SfxType, AudioClip> _clips = new Dictionary<SfxType, AudioClip>();
        private AudioSource _sfxSource;
        private float _lastFlamerSoundTime;
        private float _lastBolterSoundTime;
        private float _lastOrkDeathSoundTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;

            SynthesizeAllClips();
        }

        public static void Play(SfxType type, float volume = 0.75f, float pitchVariance = 0.06f)
        {
            if (Instance == null)
            {
                if (GameManager.Instance != null)
                {
                    Instance = GameManager.Instance.gameObject.AddComponent<SoundManager>();
                }
                else
                {
                    return;
                }
            }

            Instance.PlayInternal(type, volume, pitchVariance);
        }

        private void PlayInternal(SfxType type, float volume, float pitchVariance)
        {
            // Throttle high-frequency rapid sounds so multiple towers don't clip audio
            float now = Time.unscaledTime;
            if (type == SfxType.FlamerWhoosh)
            {
                if (now - _lastFlamerSoundTime < 0.14f) return;
                _lastFlamerSoundTime = now;
            }
            else if (type == SfxType.BolterShot)
            {
                if (now - _lastBolterSoundTime < 0.045f) return;
                _lastBolterSoundTime = now;
            }
            else if (type == SfxType.OrkDeath)
            {
                if (now - _lastOrkDeathSoundTime < 0.07f) return;
                _lastOrkDeathSoundTime = now;
            }

            if (!_clips.TryGetValue(type, out AudioClip clip) || clip == null)
                return;

            _sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void SynthesizeAllClips()
        {
            // 1. Heavy Bolter: Punchy low-mid thud + crisp metallic crack
            _clips[SfxType.BolterShot] = CreateClip("SFX_Bolter", 0.09f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 2.2f);
                float freq = Mathf.Lerp(190f, 55f, norm);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                float noise = (HashNoise(t * 14000f) * 2f - 1f) * 0.55f;
                return Mathf.Clamp((tone * 0.75f + noise) * env * 1.3f, -1f, 1f);
            });

            // 2. Lascannon: High-energy capacitor discharge + descending laser beam zap
            _clips[SfxType.LascannonZap] = CreateClip("SFX_Lascannon", 0.25f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 1.4f);
                float freq = Mathf.Lerp(1750f, 180f, Mathf.Pow(norm, 0.55f));
                float saw = 2f * ((t * freq) - Mathf.Floor(0.5f + t * freq));
                float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);
                float crackle = (HashNoise(t * 22000f) * 2f - 1f) * 0.25f;
                return Mathf.Clamp((saw * 0.6f + sub * 0.4f + crackle) * env, -1f, 1f);
            });

            // 3. Hellhound Flame-Bunker: Roaring promethium combustion jet
            _clips[SfxType.FlamerWhoosh] = CreateClip("SFX_Flamer", 0.18f, (t, norm) =>
            {
                float env = Mathf.Sin(norm * Mathf.PI);
                float lowRumble = Mathf.Sin(2f * Mathf.PI * 85f * t) * 0.35f;
                float roarNoise = (HashNoise(t * 4200f) * 2f - 1f) * 0.75f;
                return (lowRumble + roarNoise) * env * 0.7f;
            });

            // 4. Basilisk Artillery Launch: Deep howitzer breach thump
            _clips[SfxType.ArtilleryFire] = CreateClip("SFX_ArtilleryFire", 0.32f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 1.8f);
                float freq = Mathf.Lerp(130f, 35f, norm);
                float boom = Mathf.Sin(2f * Mathf.PI * freq * t);
                float blast = (HashNoise(t * 6000f) * 2f - 1f) * 0.6f;
                return Mathf.Clamp((boom + blast) * env, -1f, 1f);
            });

            // 5. Basilisk Earthshaker Explosion: Heavy seismic detonation
            _clips[SfxType.ArtilleryExplosion] = CreateClip("SFX_ArtilleryExplosion", 0.52f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 1.5f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 28f, norm) * t) * 0.8f;
                float rumble = (HashNoise(t * 3200f) * 2f - 1f) * 0.85f;
                return Mathf.Clamp((sub + rumble) * env, -1f, 1f);
            });

            // 6. Orbital Bombardment Lock-On: Auspex targeting klaxon
            _clips[SfxType.OrbitalLockOn] = CreateClip("SFX_OrbitalLock", 0.45f, (t, norm) =>
            {
                float beepEnv = (norm < 0.45f || (norm > 0.55f && norm < 0.95f)) ? 0.8f : 0f;
                float freq = norm < 0.5f ? 660f : 880f;
                return Mathf.Sin(2f * Mathf.PI * freq * t) * beepEnv;
            });

            // 7. Orbital Bombardment Strike: Massive orbital lance beam + seismic roar
            _clips[SfxType.OrbitalStrike] = CreateClip("SFX_OrbitalStrike", 0.85f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 1.15f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 24f, norm) * t) * 0.85f;
                float beamHarmonic = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(820f, 110f, norm) * t) * 0.4f;
                float roar = (HashNoise(t * 2800f) * 2f - 1f) * 0.9f;
                return Mathf.Clamp((sub + beamHarmonic + roar) * env, -1f, 1f);
            });

            // 8. Drop Pod Slam: Descending whistle + metallic ceramite crash
            _clips[SfxType.DropPodSlam] = CreateClip("SFX_DropPodSlam", 0.48f, (t, norm) =>
            {
                if (norm < 0.25f)
                {
                    float wNorm = norm / 0.25f;
                    float freq = Mathf.Lerp(1100f, 260f, wNorm);
                    return Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f;
                }
                float cNorm = (norm - 0.25f) / 0.75f;
                float env = Mathf.Pow(1f - cNorm, 1.8f);
                float clang = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.6f;
                float impact = (HashNoise(t * 7500f) * 2f - 1f) * 0.8f;
                return Mathf.Clamp((clang + impact) * env, -1f, 1f);
            });

            // 9. Ork Death Grunt: Guttural greenskin growl
            _clips[SfxType.OrkDeath] = CreateClip("SFX_OrkDeath", 0.16f, (t, norm) =>
            {
                float env = Mathf.Sin(norm * Mathf.PI);
                float freq = Mathf.Lerp(155f, 82f, norm);
                float saw = 2f * ((t * freq) - Mathf.Floor(0.5f + t * freq));
                float growl = Mathf.Sin(2f * Mathf.PI * 38f * t);
                return (saw * 0.65f * growl) * env * 0.8f;
            });

            // 10. Boss WAAAGH! Roar: Ferocious Warboss bellow + warhorn
            _clips[SfxType.WaaaghRoar] = CreateClip("SFX_WaaaghRoar", 0.85f, (t, norm) =>
            {
                float env = Mathf.Sin(Mathf.Pow(norm, 0.6f) * Mathf.PI);
                float f1 = Mathf.Lerp(145f, 195f, Mathf.Sin(norm * Mathf.PI));
                float saw1 = 2f * ((t * f1) - Mathf.Floor(0.5f + t * f1));
                float saw2 = 2f * ((t * f1 * 1.49f) - Mathf.Floor(0.5f + t * f1 * 1.49f));
                float rasp = (HashNoise(t * 1800f) * 2f - 1f) * 0.45f;
                return Mathf.Clamp((saw1 * 0.5f + saw2 * 0.35f + rasp) * env, -1f, 1f);
            });

            // 11. Tower Construct / Upgrade: Hydraulic metallic lock
            _clips[SfxType.TowerConstruct] = CreateClip("SFX_Construct", 0.18f, (t, norm) =>
            {
                float env = Mathf.Pow(1f - norm, 2f);
                float freq = norm < 0.4f ? 320f : 480f;
                float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.45f;
                return sq * env;
            });

            // 12. Wave Start Horn: Grimdark low brass alarm horn
            _clips[SfxType.WaveStartHorn] = CreateClip("SFX_WaveHorn", 0.55f, (t, norm) =>
            {
                float env = Mathf.Sin(norm * Mathf.PI);
                float f = 110f; // A2 brass drone
                float brass = Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f
                            + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.3f
                            + Mathf.Sin(2f * Mathf.PI * f * 3f * t) * 0.2f;
                return brass * env * 0.85f;
            });
        }

        private static AudioClip CreateClip(string name, float durationSeconds, System.Func<float, float, float> generator)
        {
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(SampleRate * durationSeconds));
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / SampleRate;
                float norm = (float)i / sampleCount;
                samples[i] = generator(t, norm);
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float HashNoise(float seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed * 12.9898f) * 43758.5453f, 1f);
        }
    }
}
