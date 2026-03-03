using UnityEngine;

/// <summary>
/// Manages dash mechanic including dash cooldown, duration, gravity override during dash,
/// and state transitions to/from the dashing state.
/// </summary>
public class PlayerDash : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float dashMultiplier = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    #endregion

    #region State

    public float dashTimer;
    public float dashCooldownTimer;
    private PlayerController controller;

    #endregion

    #region Initialization

    /// <summary>
    /// Stores reference to the player controller for state and physics access.
    /// </summary>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

    #endregion

    #region Dash Logic

    /// <summary>
    /// Updates dash timers, initiates dash when input and cooldown conditions are met,
    /// and handles transition back to normal state when dash completes.
    /// </summary>
    public void Dash()
    {
        // Decrement active timers
        dashTimer -= Time.fixedDeltaTime;
        dashCooldownTimer -= Time.fixedDeltaTime;

        // Initiate dash if conditions are met
        if (controller.dashPressed && dashCooldownTimer <= 0f)
        {
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            controller.dashPressed = false;
            controller.TryChangeState(PlayerController.PlayerState.Dashing);

            // Apply dash velocity in movement direction
            Vector2 velocity = new Vector2(
                controller.moveValue * dashMultiplier,
                controller.m_Rigidbody2D.linearVelocity.y
            );
            controller.m_Rigidbody2D.linearVelocity = velocity;
            controller.OverrideGravity(0f);
        }

        // Detect dash completion and return to normal state
        if (dashTimer <= 0f && dashTimer + Time.fixedDeltaTime > 0f)
        {
            controller.TryChangeState(PlayerController.PlayerState.Normal);
            controller.ClearGravityOverride();
        }
    }

    #endregion
}
