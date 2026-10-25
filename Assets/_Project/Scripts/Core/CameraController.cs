using UnityEngine;

namespace OutpostAegis.Core
{
    /// <summary>
    /// Mobile-friendly 2D tactical camera supporting touch drag panning, pinch-to-zoom,
    /// mouse drag/scroll fallback, and trauma-based screen shake for Orbital Bombardments & explosions.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Pan Limits (World Units)")]
        [SerializeField] private Vector2 minBounds = new Vector2(-4f, -2.5f);
        [SerializeField] private Vector2 maxBounds = new Vector2(4f, 2.5f);

        [Header("Zoom Limits (Orthographic Size)")]
        [SerializeField] private float minOrthoSize = 4.5f;
        [SerializeField] private float maxOrthoSize = 8.5f;
        [SerializeField] private float zoomSpeed = 2.0f;

        private Camera _cam;
        private Vector3 _basePosition;
        private Vector3 _dragOriginWorld;
        private bool _isDragging;

        // Screen shake state
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;

        private void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            _basePosition = transform.position;
        }

        public static void TriggerShake(float intensity = 0.25f, float duration = 0.35f)
        {
            if (Instance == null && Camera.main != null)
            {
                Instance = Camera.main.GetComponent<CameraController>();
            }
            if (Instance == null) return;

            Instance._shakeIntensity = Mathf.Max(Instance._shakeIntensity, intensity);
            Instance._shakeDuration = Mathf.Max(0.05f, duration);
            Instance._shakeTimer = Mathf.Max(Instance._shakeTimer, duration);
        }

        private void LateUpdate()
        {
            // Restore unshaken base position before processing input
            transform.position = _basePosition;

            if (Input.touchCount == 2)
            {
                HandleTouchPinchZoom();
                _isDragging = false;
            }
            else
            {
                HandlePanInput();
                HandleScrollWheelZoom();
            }

            ClampBasePosition();
            ApplyScreenShakeOffset();
        }

        private void HandlePanInput()
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                _dragOriginWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                _isDragging = true;
            }

            if (_isDragging && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector3 currentWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 delta = _dragOriginWorld - currentWorld;
                _basePosition += new Vector3(delta.x, delta.y, 0f);
            }

            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                _isDragging = false;
            }
        }

        private void HandleTouchPinchZoom()
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevPos0 = t0.position - t0.deltaPosition;
            Vector2 prevPos1 = t1.position - t1.deltaPosition;

            float prevMag = (prevPos0 - prevPos1).magnitude;
            float currentMag = (t0.position - t1.position).magnitude;
            float diff = currentMag - prevMag;

            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - diff * 0.01f * zoomSpeed,
                minOrthoSize,
                maxOrthoSize);
        }

        private void HandleScrollWheelZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _cam.orthographicSize = Mathf.Clamp(
                    _cam.orthographicSize - scroll * zoomSpeed * 2f,
                    minOrthoSize,
                    maxOrthoSize);
            }
        }

        private void ClampBasePosition()
        {
            _basePosition.x = Mathf.Clamp(_basePosition.x, minBounds.x, maxBounds.x);
            _basePosition.y = Mathf.Clamp(_basePosition.y, minBounds.y, maxBounds.y);
            _basePosition.z = -10f;
        }

        private void ApplyScreenShakeOffset()
        {
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                float damper = Mathf.Clamp01(_shakeTimer / _shakeDuration);
                Vector2 offset = Random.insideUnitCircle * (_shakeIntensity * damper);
                transform.position = _basePosition + new Vector3(offset.x, offset.y, 0f);

                if (_shakeTimer <= 0f)
                {
                    _shakeIntensity = 0f;
                }
            }
            else
            {
                transform.position = _basePosition;
            }
        }
    }
}
