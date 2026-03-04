using UnityEngine;

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
        // Update timers
        dashTimer -= Time.fixedDeltaTime;
        dashCooldownTimer -= Time.fixedDeltaTime;

        // Check if dash should start
        if (controller.dashPressed && dashCooldownTimer <= 0f)
        {
            StartDash();
        }

        // Check if dash should end
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

        // Use move input for direction, or use facing direction if no input
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
