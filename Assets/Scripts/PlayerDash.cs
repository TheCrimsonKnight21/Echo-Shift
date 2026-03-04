using UnityEngine;

/// <summary>
/// Handles dash mechanic with cooldown management and instant directional movement.
/// Dash is purely horizontal with gravity disabled, allowing dashing in place or while airborne.
/// </summary>
public class PlayerDash : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private float dashMultiplayer = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    #endregion

    #region Private Fields
    private PlayerController controller;
    #endregion

    #region Public Fields - Timers
    public float dashTimer;
    public float dashCooldownTimer;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the dash system with a reference to the player controller.
    /// Must be called during PlayerController.Awake() before any dash logic executes.
    /// </summary>
    /// <param name="controller">The PlayerController instance that owns this dash subsystem.</param>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Main Dash Logic
    /// <summary>
    /// Executes dash cooldown timing and initiates dash when input detected and cooldown expired.
    /// Handles dash duration and returns to normal state when dash completes.
    /// Called from PlayerController.FixedUpdate() each frame.
    /// </summary>
    public void Dash()
    {
        dashTimer -= Time.fixedDeltaTime;
        dashCooldownTimer -= Time.fixedDeltaTime;

        if (controller.dashPressed && dashCooldownTimer <= 0f)
        {
            StartDash();
        }

        if (dashTimer <= 0f && dashTimer + Time.fixedDeltaTime > 0f)
        {
            EndDash();
        }
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Initiates a dash in the appropriate direction.
    /// Resets Y velocity to zero for purely horizontal movement and disables gravity during dash.
    /// </summary>
    /// <remarks>
    /// Direction priority:
    /// 1. Use moveValue if player is providing input
    /// 2. Fall back to facing direction for in-place dashing
    /// Gravity is disabled (0) and Y velocity reset to prevent vertical momentum from affecting dash trajectory.
    /// </remarks>
    private void StartDash()
    {
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        controller.dashPressed = false;
        controller.TryChangeState(PlayerController.PlayerState.Dashing);

        float dashDirection = controller.moveValue != 0f ? controller.moveValue : controller.movement.GetFacingDirection();
        Vector2 velocity = new Vector2(dashDirection * dashMultiplayer, 0f);
        controller.m_Rigidbody2D.linearVelocity = velocity;
        controller.OverrideGravity(0f);
    }

    private void EndDash()
    {
        controller.TryChangeState(PlayerController.PlayerState.Normal);
        controller.ClearGravityOverride();
    }
    #endregion
}
