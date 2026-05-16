using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orthographic camera: SmoothDamp follow, map-edge clamp.
/// Zoom: Ctrl + scroll wheel, or UI +/- buttons (no keyboard +/-).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class StardewStyleCamera2D : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayerOnStart = true;
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Orthographic zoom")]
    [SerializeField] private float minOrthographicSize = 4f;
    [SerializeField] private float maxOrthographicSize = 14f;
    [SerializeField] private float zoomStep = 0.5f;
    [SerializeField] private float scrollZoomSpeed = 0.8f;

    [Header("Bounds")]
    [SerializeField] private Collider2D mapBoundsCollider;
    [SerializeField] private bool useManualCenterBounds;
    [SerializeField] private Vector2 manualCenterMin = new Vector2(-8f, -6f);
    [SerializeField] private Vector2 manualCenterMax = new Vector2(8f, 6f);

    private Camera cam;
    private Vector3 smoothVelocity;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public Collider2D MapBoundsCollider
    {
        get => mapBoundsCollider;
        set => mapBoundsCollider = value;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    private void Start()
    {
        if (target == null && autoFindPlayerOnStart)
        {
            var player = GameObject.Find("Player");
            if (player != null)
                target = player.transform;
        }

        if (mapBoundsCollider == null)
        {
            var bounds = GameObject.Find("MapBounds");
            if (bounds != null)
                mapBoundsCollider = bounds.GetComponent<Collider2D>();
        }
    }

    private void Update()
    {
        HandleCtrlScrollZoom();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = target.position + followOffset;
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref smoothVelocity, smoothTime);
        smoothed.z = followOffset.z;

        Vector2 center = new Vector2(smoothed.x, smoothed.y);
        center = ClampCenter(center);
        transform.position = new Vector3(center.x, center.y, followOffset.z);
    }

    /// <summary>Called by UI "+" button — zoom in (closer).</summary>
    public void ZoomIn()
    {
        cam.orthographicSize = Mathf.Max(minOrthographicSize, cam.orthographicSize - zoomStep);
    }

    /// <summary>Called by UI "-" button — zoom out (farther).</summary>
    public void ZoomOut()
    {
        cam.orthographicSize = Mathf.Min(maxOrthographicSize, cam.orthographicSize + zoomStep);
    }

    private void HandleCtrlScrollZoom()
    {
        if (!IsCtrlHeld())
            return;

        float scroll = 0f;
        if (Mouse.current != null)
            scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) < 0.01f)
            return;

        float next = cam.orthographicSize - scroll * scrollZoomSpeed * 0.01f;
        cam.orthographicSize = Mathf.Clamp(next, minOrthographicSize, maxOrthographicSize);
    }

    private static bool IsCtrlHeld()
    {
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftCtrlKey.isPressed
                   || Keyboard.current.rightCtrlKey.isPressed;
        }

        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private Vector2 ClampCenter(Vector2 center)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        if (useManualCenterBounds)
            return ClampCenterToRect(center, manualCenterMin, manualCenterMax, halfW, halfH);

        if (mapBoundsCollider == null)
            return center;

        Bounds b = mapBoundsCollider.bounds;
        Vector2 min = new Vector2(b.min.x + halfW, b.min.y + halfH);
        Vector2 max = new Vector2(b.max.x - halfW, b.max.y - halfH);

        if (min.x <= max.x)
            center.x = Mathf.Clamp(center.x, min.x, max.x);

        if (min.y <= max.y)
            center.y = Mathf.Clamp(center.y, min.y, max.y);

        return center;
    }

    private static Vector2 ClampCenterToRect(Vector2 center, Vector2 rectMin, Vector2 rectMax, float halfW, float halfH)
    {
        Vector2 min = new Vector2(rectMin.x + halfW, rectMin.y + halfH);
        Vector2 max = new Vector2(rectMax.x - halfW, rectMax.y - halfH);

        if (min.x <= max.x)
            center.x = Mathf.Clamp(center.x, min.x, max.x);

        if (min.y <= max.y)
            center.y = Mathf.Clamp(center.y, min.y, max.y);

        return center;
    }
}
