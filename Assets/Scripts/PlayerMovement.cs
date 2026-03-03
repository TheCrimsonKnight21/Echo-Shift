using UnityEngine;

/// <summary>
/// Manages horizontal movement, acceleration/deceleration, air control, and character facing direction.
/// Handles crouch mechanics including ceiling detection and collider management.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    #region Configuration - Movement

    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float deceleration = 160f;
    [SerializeField] private float airAcceleration = 25f;

    #endregion

    #region Configuration - Crouch

    [Range(0, 1)]
    [SerializeField] private float crouchMultiplier = 0.2f;
    [SerializeField] private Collider2D m_CrouchDisableCollider;
    [SerializeField] private Transform m_CeilingCheck;
    private const float k_CeilingRadius = 0.2f;

    #endregion

    #region State

    private bool crouching;
    private bool m_FacingRight = true;
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

    #region Movement Logic

    /// <summary>
    /// Main movement update handling crouch state transitions, speed calculations,
    /// acceleration/deceleration, and character facing direction.
    /// </summary>
    public void Move()
    {
        UpdateCrouchState();
        ApplyMovement();
    }

    #endregion

    #region Crouch System

    /// <summary>
    /// Handles crouch state transitions with ceiling detection.
    /// Updates speed modifiers and collider state when crouch state changes.
    /// </summary>
    private void UpdateCrouchState()
    {
        bool wantsToCrouch = controller.crouch;
        bool ceilingBlocking = Physics2D.OverlapCircle(
            m_CeilingCheck.position,
            k_CeilingRadius,
            controller.m_WhatIsGround
        );

        // Transition from normal to crouching
        if (!crouching && wantsToCrouch && !ceilingBlocking &&
            controller.TryChangeState(PlayerController.PlayerState.Crouching))
        {
            crouching = true;
            controller.NotifyCrouch(true);
            controller.AddSpeedModifier(crouchMultiplier);

            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = false;
        }
        // Transition from crouching to normal
        else if (crouching && !wantsToCrouch && !ceilingBlocking &&
            controller.TryChangeState(PlayerController.PlayerState.Normal))
        {
            crouching = false;
            controller.NotifyCrouch(false);
            controller.RemoveSpeedModifier(crouchMultiplier);

            if (m_CrouchDisableCollider != null)
                m_CrouchDisableCollider.enabled = true;
        }
    }

    #endregion

    #region Acceleration & Velocity

    /// <summary>
    /// Applies acceleration or deceleration to achieve target speed.
    /// Distinguishes between grounded acceleration and air control.
    /// </summary>
    private void ApplyMovement()
    {
        // Only apply movement if grounded or air control is enabled
        if (!controller.m_Grounded && !controller.m_AirControl)
            return;

        // Calculate target speed based on input and movement modifiers
        float targetSpeed = controller.moveValue * controller.CurrentMoveSpeed;

        // Determine acceleration rate
        float accelRate = CalculateAccelerationRate(targetSpeed);

        // Apply smooth velocity transition
        float newX = Mathf.MoveTowards(
            controller.m_Rigidbody2D.linearVelocity.x,
            targetSpeed,
            accelRate * Time.fixedDeltaTime
        );

        controller.m_Rigidbody2D.linearVelocity = new Vector2(
            newX,
            controller.m_Rigidbody2D.linearVelocity.y
        );

        // Update facing direction based on movement input
        UpdateFacingDirection();
    }

    /// <summary>
    /// Calculates appropriate acceleration rate based on current velocity and target.
    /// Higher acceleration used when changing direction for snappier controls.
    /// </summary>
    private float CalculateAccelerationRate(float targetSpeed)
    {
        float currentVelX = controller.m_Rigidbody2D.linearVelocity.x;
        bool changingDirection = Mathf.Sign(targetSpeed) != Mathf.Sign(currentVelX);
        bool hasTargetSpeed = Mathf.Abs(targetSpeed) > 0.01f;

        if (changingDirection && hasTargetSpeed)
        {
            // Snappier direction change
            return acceleration * 2f;
        }
        else if (hasTargetSpeed)
        {
            // Normal acceleration/deceleration based on ground state
            return controller.m_Grounded ? acceleration : airAcceleration;
        }
        else
        {
            // Deceleration when no input
            return deceleration;
        }
    }

    #endregion

    #region Facing Direction

    /// <summary>
    /// Updates character facing direction based on movement input.
    /// Flips the character model when direction changes.
    /// </summary>
    private void UpdateFacingDirection()
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

    /// <summary>
    /// Flips the character model by negating the x-axis scale.
    /// </summary>
    private void Flip()
    {
        m_FacingRight = !m_FacingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    #endregion
}
