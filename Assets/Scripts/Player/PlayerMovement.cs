using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal top-down movement using the generated PlayerInput (Input System).
/// Mirrors the movement pattern used in the old project:
/// - Read Move Vector2 from input callbacks
/// - Apply Rigidbody2D linearVelocity in FixedUpdate
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Vector2 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = new PlayerInput();

        // Top-down defaults (apply immediately so it works even in very small test scenes)
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        playerInput.Default.Enable();
    }

    private void OnDisable()
    {
        if (playerInput == null) return;

        playerInput.Default.Disable();
    }

    private void FixedUpdate()
    {
        // Prefer Input System (old project), but fall back to legacy axes if Input System isn't active yet.
        movement = playerInput.Default.Move.ReadValue<Vector2>();
        if (movement.sqrMagnitude < 0.0001f)
        {
            movement = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
        }

        if (movement.sqrMagnitude > 1f)
            movement = movement.normalized;

        rb.velocity = movement * moveSpeed;
    }
}

