using UnityEngine;

/// <summary>
/// Manages jump mechanics including variable gravity based on jump height and button hold duration.
/// Implements coyote time for forgiving jump input and apex gravity reduction.
/// </summary>
public class PlayerJump : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float fallGravity = 25f;
    [SerializeField] private float jumpGravity = 2f;
    [SerializeField] private float lowJumpGravity = 8f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float apexThreshold = 1f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;

    #endregion

    #region State

    private float gravityMultiplier = 1f;
    private PlayerController controller;

    #endregion

    #region Initialization

    /// <summary>
    /// Stores reference to the player controller for input and physics access.
    /// </summary>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

    #endregion

    #region Jump Logic

    /// <summary>
    /// Updates coyote timer, handles jump initiation when buffer and coyote conditions are met,
    /// and applies dynamic gravity based on jump velocity and button hold state.
    /// </summary>
    public void Jump()
    {
        // Update coyote timer based on ground state
        if (controller.m_Grounded)
        {
            controller.coyoteTimer = coyoteTime;
        }
        else
        {
            controller.coyoteTimer -= Time.fixedDeltaTime;
        }

        // Initiate jump if both buffer and coyote windows are active
        if (controller.coyoteTimer > 0f && controller.jumpBufferTimer > 0f)
        {
            controller.m_Grounded = false;
            controller.m_Rigidbody2D.linearVelocity = new Vector2(
                controller.m_Rigidbody2D.linearVelocity.x,
                controller.jumpVelocity
            );
            controller.jumpBufferTimer = 0f;
            controller.coyoteTimer = 0f;
        }

        // Calculate gravity multiplier based on jump velocity and button hold
        CalculateJumpGravity();

        // Apply calculated gravity to controller
        controller.OverrideGravity(gravityMultiplier);
    }

    /// <summary>
    /// Determines gravity multiplier based on vertical velocity and jump button hold state.
    /// Applies apex gravity reduction when velocity approaches zero at jump peak.
    /// </summary>
    private void CalculateJumpGravity()
    {
        float verticalVelocity = controller.m_Rigidbody2D.linearVelocity.y;

        // Select base gravity multiplier based on velocity direction and button state
        if (verticalVelocity < 0)
        {
            // Falling
            gravityMultiplier = fallGravity / controller.BaseGravity;
        }
        else if (verticalVelocity > 0 && !controller.jumpHeld)
        {
            // Early jump release
            gravityMultiplier = lowJumpGravity;
        }
        else if (verticalVelocity > 0)
        {
            // Ascending with button held
            gravityMultiplier = jumpGravity;
        }
        else
        {
            // Stationary or transitional
            gravityMultiplier = 1f;
        }

        // Apply apex gravity reduction when near jump peak
        float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(verticalVelocity));
        gravityMultiplier *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);
    }

    #endregion
}
