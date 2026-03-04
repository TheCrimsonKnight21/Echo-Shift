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
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Main Dash Logic
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
    private void StartDash()
    {
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        controller.dashPressed = false;
        controller.TryChangeState(PlayerController.PlayerState.Dashing);

        // Use movement direction if input exists, otherwise dash in facing direction to allow in-place dashing
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
