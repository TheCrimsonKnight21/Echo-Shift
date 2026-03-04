using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Manages jump logic including variable gravity for player control, jump buffering, and coyote time.
/// Applies different gravity multipliers based on jump height and button hold duration for responsive feel.
/// </summary>
public class PlayerJump : MonoBehaviour
{
    #region Serialized Fields - Jump Settings
    [SerializeField] private float jumpGravity = 2f;
    [SerializeField] private float fallGravity = 25f;
    [SerializeField] private float lowJumpGravity = 8f;
    
    [SerializeField] private float coyoteTime = 0.1f;
    
    [SerializeField] private float apexThreshold = 1f;
    [SerializeField] private float apexGravityMultiplier = 0.5f;
    
    [SerializeField] private LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;
    #endregion

    #region Private Fields
    private PlayerController controller;
    private float gravityMultiplier = 1f;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the jump system with a reference to the player controller.
    /// Must be called during PlayerController.Awake() before any jump logic executes.
    /// </summary>
    /// <param name="controller">The PlayerController instance that owns this jump subsystem.</param>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Main Jump Logic
    /// <summary>
    /// Executes jump logic each fixed frame including coyote time decrement, jump input buffering,
    /// and gravity multiplier calculations. Called from PlayerController.FixedUpdate().
    /// </summary>
    /// <remarks>
    /// The jump executes when both coyoteTimer > 0 and jumpBufferTimer > 0, allowing forgiving jump inputs.
    /// Gravity override is skipped during dashing to allow horizontal-only movement.
    /// </remarks>
    public void Jump()
    {
        if (controller.CurrentState != PlayerController.PlayerState.Dashing)
        {
            controller.OverrideGravity(gravityMultiplier);
        }

        if (controller.m_Grounded)
            controller.coyoteTimer = coyoteTime;
        else
            controller.coyoteTimer -= Time.fixedDeltaTime;

        if (controller.coyoteTimer > 0f && controller.jumpBufferTimer > 0f)
        {
            controller.m_Grounded = false;
            controller.m_Rigidbody2D.linearVelocity = new Vector2(controller.m_Rigidbody2D.linearVelocity.x, controller.jumpVelocity);
            controller.jumpBufferTimer = 0f;
            controller.coyoteTimer = 0f;
        }

        CalculateJumpGravity();
        ApplyApexGravityEffect();

        if (controller.CurrentState != PlayerController.PlayerState.Dashing)
        {
            controller.OverrideGravity(gravityMultiplier);
        }
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Determines the gravity multiplier based on vertical velocity and jump button state.
    /// Provides responsive control by falling faster on descent and slower when button is released.
    /// </summary>
    /// <remarks>
    /// Gravity multiplier values:
    /// - Descending: fallGravity (25)
    /// - Ascending with button released: lowJumpGravity (8)
    /// - Ascending with button held: jumpGravity (2)
    /// - At peak (near zero velocity): 1.0
    /// </remarks>
    private void CalculateJumpGravity()
    {
        if (controller.m_Rigidbody2D.linearVelocity.y < 0)
        {
            gravityMultiplier = fallGravity / controller.BaseGravity;
        }
        else if (controller.m_Rigidbody2D.linearVelocity.y > 0 && !controller.jumpHeld)
        {
            gravityMultiplier = lowJumpGravity;
        }
        else if (controller.m_Rigidbody2D.linearVelocity.y > 0)
        {
            gravityMultiplier = jumpGravity;
        }
        else
        {
            gravityMultiplier = 1f;
        }
    }

    /// <summary>
    /// Reduces gravity at jump apex for floaty feel. Uses inverse lerp to smoothly transition
    /// reduced gravity only when vertical velocity is near zero at the peak of the jump.
    /// </summary>
    /// <remarks>
    /// This creates the classic platformer feel where the player hangs in air momentarily at the jump peak.
    /// The effect is strongest when velocity is below apexThreshold and scales using apexGravityMultiplier.
    /// </remarks>
    private void ApplyApexGravityEffect()
    {
        float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(controller.m_Rigidbody2D.linearVelocity.y));
        gravityMultiplier *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);
    }
    #endregion
}
