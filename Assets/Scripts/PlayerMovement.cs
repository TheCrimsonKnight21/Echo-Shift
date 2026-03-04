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
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Movement Logic
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
    /// Disables collider to allow passing under low areas.
    /// </summary>
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
    /// Re-enables collider when standing up.
    /// </summary>
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
    /// Calculates acceleration rate based on target speed and current velocity.
    /// Returns higher acceleration for direction changes, standard for normal movement, deceleration for stops.
    /// </summary>
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

    public float GetFacingDirection()
    {
        return m_FacingRight ? 1f : -1f;
    }
    #endregion
}
