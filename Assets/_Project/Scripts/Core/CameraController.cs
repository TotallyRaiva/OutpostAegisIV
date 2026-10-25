using UnityEngine;

namespace OutpostAegis.Core
{
    /// <summary>
    /// Mobile-friendly 2D tactical camera supporting touch drag panning, pinch-to-zoom,
    /// and mouse drag/scroll fallback within Outpost Aegis-IV map bounds.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Limits (World Units)")]
        [SerializeField] private Vector2 minBounds = new Vector2(-4f, -2.5f);
        [SerializeField] private Vector2 maxBounds = new Vector2(4f, 2.5f);

        [Header("Zoom Limits (Orthographic Size)")]
        [SerializeField] private float minOrthoSize = 4.5f;
        [SerializeField] private float maxOrthoSize = 8.5f;
        [SerializeField] private float zoomSpeed = 2.0f;

        private Camera _cam;
        private Vector3 _dragOriginWorld;
        private bool _isDragging;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (Input.touchCount == 2)
            {
                HandleTouchPinchZoom();
                _isDragging = false;
                return;
            }

            HandlePanInput();
            HandleScrollWheelZoom();
            ClampPosition();
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
                transform.position += new Vector3(delta.x, delta.y, 0f);
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

        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            transform.position = pos;
        }
    }
}
