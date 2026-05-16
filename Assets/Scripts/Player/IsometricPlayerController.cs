using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Hybrid movement: WASD + left-click to walk on walkable ground cells.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class IsometricPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap obstacleTilemap;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistance = 0.08f;
    [SerializeField] private bool showClickMarker = true;

    private static readonly Vector3Int[] NeighborOffsets =
    {
        Vector3Int.zero,
        Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right,
        new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0),
    };

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2? clickDestination;
    private Vector2 keyboardInput;
    private Transform clickMarker;

    public Tilemap GroundTilemap
    {
        get => groundTilemap;
        set => groundTilemap = value;
    }

    public Tilemap ObstacleTilemap
    {
        get => obstacleTilemap;
        set => obstacleTilemap = value;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        EnsureTilemapReferences();
    }

    private void Start()
    {
        EnsureTilemapReferences();

        if (groundTilemap == null)
            Debug.LogWarning("[IsometricPlayerController] Ground Tilemap not found. Click-to-move disabled. Run JingJu → Setup Isometric Demo Scene.");
    }

    private void Update()
    {
        ReadKeyboardInput();
        ReadClickInput();
        UpdateClickMarker();
    }

    private void FixedUpdate()
    {
        if (keyboardInput.sqrMagnitude > 0.01f)
        {
            rb.velocity = keyboardInput.normalized * moveSpeed;
            return;
        }

        if (!clickDestination.HasValue)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 toTarget = clickDestination.Value - pos;
        float dist = toTarget.magnitude;

        if (dist <= stopDistance)
        {
            rb.velocity = Vector2.zero;
            clickDestination = null;
            return;
        }

        rb.velocity = toTarget / dist * moveSpeed;
    }

    private void EnsureTilemapReferences()
    {
        if (groundTilemap == null)
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
                groundTilemap = ground.GetComponent<Tilemap>();
        }

        if (obstacleTilemap == null)
        {
            var obstacle = GameObject.Find("Obstacle");
            if (obstacle != null)
                obstacleTilemap = obstacle.GetComponent<Tilemap>();
        }

        // Fallback for older scenes with a single "Tilemap" child under Grid.
        if (groundTilemap == null)
        {
            var legacy = GameObject.Find("Tilemap");
            if (legacy != null)
                groundTilemap = legacy.GetComponent<Tilemap>();
        }
    }

    private void ReadKeyboardInput()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        }
        else
        {
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
        }

        keyboardInput = new Vector2(x, y);
        if (keyboardInput.sqrMagnitude > 0.01f)
            clickDestination = null;
    }

    private void ReadClickInput()
    {
        if (!WasLeftClickPressed())
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera == null || groundTilemap == null)
            return;

        Vector2 world = ScreenToWorldOnPlayPlane();
        Vector3Int cell = groundTilemap.WorldToCell(world);

        if (!TryFindWalkableCell(cell, out Vector3Int walkableCell))
            return;

        clickDestination = groundTilemap.GetCellCenterWorld(walkableCell);
    }

    private bool WasLeftClickPressed()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
        return Input.GetMouseButtonDown(0);
    }

    private Vector2 ScreenToWorldOnPlayPlane()
    {
        Vector3 screen;

        if (Mouse.current != null)
            screen = Mouse.current.position.ReadValue();
        else
            screen = Input.mousePosition;

        // Distance from camera to z=0 play plane (camera usually at z = -10).
        screen.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 world = mainCamera.ScreenToWorldPoint(screen);
        return new Vector2(world.x, world.y);
    }

    private bool TryFindWalkableCell(Vector3Int origin, out Vector3Int walkableCell)
    {
        foreach (var offset in NeighborOffsets)
        {
            var cell = origin + offset;
            if (IsWalkableCell(cell))
            {
                walkableCell = cell;
                return true;
            }
        }

        walkableCell = default;
        return false;
    }

    private bool IsWalkableCell(Vector3Int cell)
    {
        if (obstacleTilemap != null && obstacleTilemap.HasTile(cell))
            return false;

        return groundTilemap != null && groundTilemap.HasTile(cell);
    }

    private void UpdateClickMarker()
    {
        if (!showClickMarker)
            return;

        if (!clickDestination.HasValue)
        {
            if (clickMarker != null)
                clickMarker.gameObject.SetActive(false);
            return;
        }

        if (clickMarker == null)
            clickMarker = CreateClickMarker();

        clickMarker.gameObject.SetActive(true);
        clickMarker.position = new Vector3(clickDestination.Value.x, clickDestination.Value.y, 0f);
    }

    private Transform CreateClickMarker()
    {
        var go = new GameObject("ClickMarker");
        go.transform.localScale = Vector3.one * 0.25f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateDotSprite();
        sr.color = new Color(1f, 1f, 0.4f, 0.85f);
        sr.sortingOrder = 500;
        return go.transform;
    }

    private static Sprite CreateDotSprite()
    {
        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.35f;
        var center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = Vector2.Distance(new Vector2(x, y), center) <= r;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void OnDrawGizmosSelected()
    {
        if (!clickDestination.HasValue)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(clickDestination.Value, 0.15f);
    }
}
