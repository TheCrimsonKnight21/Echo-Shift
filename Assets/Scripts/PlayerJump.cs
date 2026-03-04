using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Main Jump Logic
    public void Jump()
    {
        // Apply gravity override for non-dashing states
        if (controller.CurrentState != PlayerController.PlayerState.Dashing)
        {
            controller.OverrideGravity(gravityMultiplier);
        }

        // Update coyote timer
        if (controller.m_Grounded)
            controller.coyoteTimer = coyoteTime;
        else
            controller.coyoteTimer -= Time.fixedDeltaTime;

        // Execute jump if conditions are met
        if (controller.coyoteTimer > 0f && controller.jumpBufferTimer > 0f)
        {
            controller.m_Grounded = false;
            controller.m_Rigidbody2D.linearVelocity = new Vector2(controller.m_Rigidbody2D.linearVelocity.x, controller.jumpVelocity);
            controller.jumpBufferTimer = 0f;
            controller.coyoteTimer = 0f;
        }

        // Calculate variable jump gravity
        CalculateJumpGravity();
        ApplyApexGravityEffect();

        // Only apply gravity if not dashing
        if (controller.CurrentState != PlayerController.PlayerState.Dashing)
        {
            controller.OverrideGravity(gravityMultiplier);
        }
    }
    #endregion

    #region Private Helper Methods
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

    private void ApplyApexGravityEffect()
    {
        float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(controller.m_Rigidbody2D.linearVelocity.y));
        gravityMultiplier *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);
    }
    #endregion
}
