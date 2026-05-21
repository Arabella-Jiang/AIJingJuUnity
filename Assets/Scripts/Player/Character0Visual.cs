using UnityEngine;

/// <summary>
/// Plays Character0 idle / walk / run sheets based on Rigidbody2D velocity (5 direction rows).
/// Sheet has left-facing rows only; right uses flipX on rows 1–3.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class Character0Visual : MonoBehaviour
{
    [SerializeField] private Character0SpriteLibrary library;
    [SerializeField] private float walkFrameRate = 10f;
    [SerializeField] private float runFrameRate = 14f;
    [SerializeField] private float runSpeedThreshold = 3.2f;
    [SerializeField] private float movingSpeedThreshold = 0.12f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D body;
    private int lastDirectionRow;
    private bool lastFlipX;
    private float frameTimer;
    private int frameIndex;

    public Character0SpriteLibrary Library
    {
        get => library;
        set => library = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (library == null || !library.IsReady)
            return;

        Vector2 velocity = body != null ? body.velocity : Vector2.zero;
        bool moving = velocity.sqrMagnitude > movingSpeedThreshold * movingSpeedThreshold;

        if (moving)
            lastDirectionRow = VelocityToDirectionRow(velocity, out lastFlipX);

        bool useRun = moving && velocity.magnitude >= runSpeedThreshold;
        Sprite[] sheet = moving ? (useRun ? library.run : library.walk) : library.idle;
        int columns = moving ? (useRun ? library.runColumns : library.walkColumns) : library.idleColumns;
        float frameRate = moving ? (useRun ? runFrameRate : walkFrameRate) : walkFrameRate;

        if (moving)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= 1f / Mathf.Max(frameRate, 1f))
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % Mathf.Max(columns, 1);
            }
        }
        else
        {
            frameTimer = 0f;
            frameIndex = 0;
        }

        var sprite = library.GetFrame(sheet, lastDirectionRow, frameIndex, columns);
        if (sprite != null)
            spriteRenderer.sprite = sprite;

        spriteRenderer.flipX = lastFlipX;
    }

    /// <summary>
    /// Sheet rows: 0 toward camera, 1 down-right, 2 right profile, 3 up-right, 4 away.
    /// Left uses row 2 + flipX; down-left/up-left diagonals use row 1/3 + flipX.
    /// </summary>
    public static int VelocityToDirectionRow(Vector2 velocity, out bool flipX)
    {
        flipX = false;
        if (velocity.sqrMagnitude < 0.0001f)
            return 0;

        Vector2 v = velocity.normalized;
        float bestDot = float.NegativeInfinity;
        int bestRow = 0;
        bool bestFlip = false;

        Evaluate(v, new Vector2(0f, -1f), 0, false, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, Diagonal(-1f, -1f), 1, false, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, Diagonal(1f, -1f), 1, true, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, new Vector2(-1f, 0f), 2, false, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, new Vector2(1f, 0f), 2, true, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, Diagonal(-1f, 1f), 3, false, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, Diagonal(1f, 1f), 3, true, ref bestDot, ref bestRow, ref bestFlip);
        Evaluate(v, new Vector2(0f, 1f), 4, false, ref bestDot, ref bestRow, ref bestFlip);

        flipX = bestFlip;
        return bestRow;
    }

    private static Vector2 Diagonal(float x, float y)
    {
        var d = new Vector2(x, y);
        return d.normalized;
    }

    private static void Evaluate(
        Vector2 velocity,
        Vector2 facing,
        int row,
        bool mirror,
        ref float bestDot,
        ref int bestRow,
        ref bool bestFlip)
    {
        float dot = Vector2.Dot(velocity, facing);
        if (dot <= bestDot)
            return;

        bestDot = dot;
        bestRow = row;
        bestFlip = mirror;
    }
}
