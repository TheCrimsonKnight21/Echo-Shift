using UnityEngine;

/// <summary>
/// Handles horizontal movement with friction-based acceleration/deceleration.
/// Manages crouch state transitions with collision detection and applies velocity changes based on input.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Serialized Fields - Movement Settings
    [Header("Movement")]
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 160f;
    [SerializeField] private float airAcceleration = 25f;
    #endregion

    #region Serialized Fields - Crouch Settings
    [Header("Crouching")]
    [Range(0, 1)]
    [SerializeField] private float crouchMultiplier = 0.2f;
    [SerializeField] private Collider2D m_CrouchDisableCollider;
    [SerializeField] private Transform m_CeilingCheck;
    #endregion

    #region Constants
    private const float k_CeilingRadius = 0.2f;
    #endregion

    #region Private Fields - State
    private PlayerController controller;
    private bool crouching;
    private bool m_FacingRight = true;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the movement system with a reference to the player controller.
    /// Must be called during PlayerController.Awake() before any movement logic executes.
    /// </summary>
    /// <param name="controller">The PlayerController instance that owns this movement subsystem.</param>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Movement Logic
    /// <summary>
    /// Executes the complete movement cycle each frame: crouch state updates, acceleration/deceleration, and sprite flipping.
    /// Called from PlayerController.FixedUpdate() and skipped during dash state.
    /// </summary>
    /// <remarks>
    /// Execution order:
    /// 1. Early return if dashing (dash handles its own velocity)
    /// 2. Update crouch state transitions
    /// 3. Apply movement acceleration if grounded or air control enabled
    /// 4. Update player facing direction based on input
    /// </remarks>
    public void Move()
    {
        if (controller.CurrentState == PlayerController.PlayerState.Dashing)
            return;

        UpdateCrouchState();

        if (controller.m_Grounded || controller.m_AirControl)
        {
            ApplyMovement();
            UpdateFacing();
        }
    }
    #endregion

    #region Private Helper Methods - Crouch
    private void UpdateCrouchState()
    {
        bool wantsToCrouch = controller.crouch;

        if (!crouching && wantsToCrouch)
        {
            TryStartCrouch();
        }
        else if (crouching && !wantsToCrouch)
        {
            TryStopCrouch();
        }
    }

    /// <summary>
    /// Attempts to start crouch if ceiling check passes and state transition succeeds.
    /// Disables collider to allow passing under low areas and applies speed reduction modifier.
    /// </summary>
    /// <remarks>
    /// Crouch only initiates if:
    /// 1. Ceiling check at m_CeilingCheck position is clear
    /// 2. State transition to Crouching state is valid
    /// Notifies controller of crouch state and disables collider for collision size reduction.
    /// </remarks>
    private void TryStartCrouch()
    {
        if (!Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, controller.m_WhatIsGround)
            && controller.TryChangeState(PlayerController.PlayerState.Crouching))
        {
            crouching = true;
            controller.NotifyCrouch(true);
            controller.AddSpeedModifier(crouchMultiplier);
            
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;
        }
    }

    /// <summary>
    /// Attempts to stop crouch if ceiling check passes and state transition succeeds.
    /// Re-enables collider and removes speed reduction modifier when standing up.
    /// </summary>
    /// <remarks>
    /// Crouch only ends if:
    /// 1. Ceiling check at m_CeilingCheck position is clear (space to stand)
    /// 2. State transition from Crouching to Normal is valid
    /// Restores full-size collider and removes crouch speed penalty.
    /// </remarks>
    private void TryStopCrouch()
    {
        if (!Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, controller.m_WhatIsGround)
            && controller.TryChangeState(PlayerController.PlayerState.Normal))
        {
            crouching = false;
            controller.NotifyCrouch(false);
            controller.RemoveSpeedModifier(crouchMultiplier);
            
            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = true;
        }
    }
    #endregion

    #region Private Helper Methods - Movement
    /// <summary>
    /// Applies smooth movement acceleration/deceleration to match target speed.
    /// Increases acceleration when changing direction for responsive control.
    /// </summary>
    /// <remarks>
    /// Uses Mathf.MoveTowards to smoothly transition to target speed using the calculated acceleration rate.
    /// Preserves Y velocity (gravity) while only modifying X velocity.
    /// </remarks>
    private void ApplyMovement()
    {
        float targetSpeed = controller.moveValue * controller.CurrentMoveSpeed;
        float accelRate = CalculateAccelerationRate(targetSpeed);

        float newX = Mathf.MoveTowards(
            controller.m_Rigidbody2D.linearVelocity.x,
            targetSpeed,
            accelRate * Time.fixedDeltaTime
        );

        controller.m_Rigidbody2D.linearVelocity = new Vector2(newX, controller.m_Rigidbody2D.linearVelocity.y);
    }

    /// <summary>
    /// Calculates acceleration rate based on movement state and direction.
    /// </summary>
    /// <param name="targetSpeed">The desired horizontal speed to reach (input * current move speed).</param>
    /// <returns>The acceleration rate in units/second to apply this frame.</returns>
    /// <remarks>
    /// Returns different rates based on conditions:
    /// - Direction change: acceleration * 2 (faster snappy turns)
    /// - Normal movement: acceleration (grounded) or airAcceleration (airborne)
    /// - Deceleration: deceleration rate (when targetSpeed is ~0)
    /// </remarks>
    private float CalculateAccelerationRate(float targetSpeed)
    {
        if (Mathf.Sign(targetSpeed) != Mathf.Sign(controller.m_Rigidbody2D.linearVelocity.x)
            && Mathf.Abs(targetSpeed) > 0.01f)
        {
            return acceleration * 2f;
        }
        else if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            return controller.m_Grounded ? acceleration : airAcceleration;
        }
        else
        {
            return deceleration;
        }
    }

    private void UpdateFacing()
    {
        if (controller.moveValue > 0 && !m_FacingRight)
        {
            Flip();
        }
        else if (controller.moveValue < 0 && m_FacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        m_FacingRight = !m_FacingRight;

        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    /// <summary>
    /// Returns the direction the player is currently facing.
    /// </summary>
    /// <returns>1 if facing right, -1 if facing left. Used for in-place dashing and directional checks.</returns>
    public float GetFacingDirection()
    {
        return m_FacingRight ? 1f : -1f;
    }
    #endregion
}
