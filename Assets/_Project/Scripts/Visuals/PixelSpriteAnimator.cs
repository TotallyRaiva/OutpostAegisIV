using UnityEngine;

namespace OutpostAegis.Visuals
{
    /// <summary>
    /// Lightweight zero-allocation 2D sprite frame animator for Ork walk cycles,
    /// molten promethium river flow, plasma reactor coils, and tower firing animations.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PixelSpriteAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _loopFrames;
        private float _framesPerSecond = 8f;
        private float _timer;
        private int _currentFrame;

        // Optional one-shot override (e.g. turret firing recoil / muzzle flash frame)
        private Sprite _overrideSprite;
        private float _overrideTimer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void SetLoopAnimation(Sprite[] frames, float fps = 8f, bool randomStartFrame = true)
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _loopFrames = frames;
            _framesPerSecond = Mathf.Max(0.5f, fps);

            if (_loopFrames != null && _loopFrames.Length > 0)
            {
                _currentFrame = randomStartFrame ? Random.Range(0, _loopFrames.Length) : 0;
                _renderer.sprite = _loopFrames[_currentFrame];
            }
        }

        public void TriggerOneShotFrame(Sprite flashOrRecoilSprite, float duration = 0.09f)
        {
            if (flashOrRecoilSprite == null) return;
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();

            _overrideSprite = flashOrRecoilSprite;
            _overrideTimer = duration;
            _renderer.sprite = _overrideSprite;
        }

        private void Update()
        {
            if (_renderer == null) return;

            if (_overrideTimer > 0f)
            {
                _overrideTimer -= Time.deltaTime;
                if (_overrideTimer <= 0f && _loopFrames != null && _loopFrames.Length > 0)
                {
                    _renderer.sprite = _loopFrames[_currentFrame % _loopFrames.Length];
                }
                return;
            }

            if (_loopFrames == null || _loopFrames.Length <= 1) return;

            _timer += Time.deltaTime * _framesPerSecond;
            if (_timer >= 1f)
            {
                int advance = Mathf.FloorToInt(_timer);
                _timer -= advance;
                _currentFrame = (_currentFrame + advance) % _loopFrames.Length;
                _renderer.sprite = _loopFrames[_currentFrame];
            }
        }
    }
}
