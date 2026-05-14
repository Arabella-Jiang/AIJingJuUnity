using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orthographic 2D camera: smooth follow (SmoothDamp), optional map-edge clamp, +/- zoom.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class StardewStyleCamera2D : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Orthographic zoom")]
    [Tooltip("Smaller value = closer / more zoomed in.")]
    [SerializeField] private float minOrthographicSize = 3f;
    [Tooltip("Larger value = farther / more zoomed out.")]
    [SerializeField] private float maxOrthographicSize = 12f;
    [SerializeField] private float zoomStep = 0.35f;

    [Header("Bounds")]
    [Tooltip("If set, camera center is clamped so the orthographic view stays inside this collider's world bounds.")]
    [SerializeField] private Collider2D mapBoundsCollider;

    [SerializeField] private bool useManualCenterBounds;
    [SerializeField] private Vector2 manualCenterMin = new Vector2(-15f, -10f);
    [SerializeField] private Vector2 manualCenterMax = new Vector2(15f, 10f);

    [Header("Input")]
    [SerializeField] private bool usePlayerInputAsset = true;

    private Camera cam;
    private Vector3 dampVelocity;
    private PlayerInput playerInput;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (!cam.orthographic)
            cam.orthographic = true;

        minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrthographicSize, maxOrthographicSize);

        if (usePlayerInputAsset)
            playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        if (playerInput == null) return;

        playerInput.Default.Enable();
        playerInput.Default.ZoomIn.performed += OnZoomInPerformed;
        playerInput.Default.ZoomOut.performed += OnZoomOutPerformed;
    }

    private void OnDisable()
    {
        if (playerInput == null) return;

        playerInput.Default.ZoomIn.performed -= OnZoomInPerformed;
        playerInput.Default.ZoomOut.performed -= OnZoomOutPerformed;
        playerInput.Default.Disable();
    }

    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.Dispose();
            playerInput = null;
        }
    }

    private void OnZoomInPerformed(InputAction.CallbackContext _) => ZoomIn();

    private void OnZoomOutPerformed(InputAction.CallbackContext _) => ZoomOut();

    /// <summary>UI / script: zoom in (smaller orthographic size).</summary>
    public void ZoomIn()
    {
        if (cam == null) return;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize - zoomStep,
            minOrthographicSize,
            maxOrthographicSize);
    }

    /// <summary>UI / script: zoom out (larger orthographic size).</summary>
    public void ZoomOut()
    {
        if (cam == null) return;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize + zoomStep,
            minOrthographicSize,
            maxOrthographicSize);
    }

    private void Update()
    {
        if (usePlayerInputAsset) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.numpadPlusKey.wasPressedThisFrame || kb.equalsKey.wasPressedThisFrame)
            ZoomIn();
        if (kb.numpadMinusKey.wasPressedThisFrame || kb.minusKey.wasPressedThisFrame)
            ZoomOut();
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        Vector3 desired = target.position + followOffset;
        desired.z = transform.position.z;

        desired = ClampCameraCenter(desired);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref dampVelocity, Mathf.Max(0.0001f, smoothTime));
        transform.position = ClampCameraCenter(transform.position);
    }

    private Vector3 ClampCameraCenter(Vector3 center)
    {
        if (!TryGetWorldBounds(out Bounds b))
            return center;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minCenterX = b.min.x + halfWidth;
        float maxCenterX = b.max.x - halfWidth;
        float minCenterY = b.min.y + halfHeight;
        float maxCenterY = b.max.y - halfHeight;

        if (minCenterX > maxCenterX)
        {
            float mid = (b.min.x + b.max.x) * 0.5f;
            minCenterX = maxCenterX = mid;
        }

        if (minCenterY > maxCenterY)
        {
            float mid = (b.min.y + b.max.y) * 0.5f;
            minCenterY = maxCenterY = mid;
        }

        center.x = Mathf.Clamp(center.x, minCenterX, maxCenterX);
        center.y = Mathf.Clamp(center.y, minCenterY, maxCenterY);
        return center;
    }

    private bool TryGetWorldBounds(out Bounds bounds)
    {
        if (mapBoundsCollider != null)
        {
            bounds = mapBoundsCollider.bounds;
            return true;
        }

        if (useManualCenterBounds)
        {
            Vector3 min = new Vector3(manualCenterMin.x, manualCenterMin.y, 0f);
            Vector3 max = new Vector3(manualCenterMax.x, manualCenterMax.y, 0f);
            bounds = new Bounds((min + max) * 0.5f, max - min);
            return true;
        }

        bounds = default;
        return false;
    }

    private void OnValidate()
    {
        minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
    }
}
