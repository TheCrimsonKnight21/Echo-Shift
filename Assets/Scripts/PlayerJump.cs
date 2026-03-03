using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerJump : MonoBehaviour
{   
    [SerializeField] float fallGravity = 25f;									// Gravity scale when the player is descending in a jump
    [SerializeField] float jumpGravity = 2f;									// Gravity scale when the player is ascending in a jump
    [SerializeField] float lowJumpGravity = 8f;									// Gravity scale when the player releases the jump button early
	[SerializeField] float jumpBufferTime = 0.1f;
	[SerializeField] float coyoteTime = 0.1f;
	[SerializeField] float apexThreshold = 1f;
	[SerializeField] float apexGravityMultiplier = 0.5f;
    

    [SerializeField] private LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
	
    float gravityMultiplier = 1f;
    private PlayerController controller;

    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

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
			// start jump
			controller.m_Grounded = false;
			controller.m_Rigidbody2D.linearVelocity = new Vector2(controller.m_Rigidbody2D.linearVelocity.x, controller.jumpVelocity);
			controller.jumpBufferTimer = 0f;
			controller.coyoteTimer = 0f;
		}
        
        // Apply variable jump gravity based on height and button hold
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
            gravityMultiplier = jumpGravity ;
		}
        else
        {
            gravityMultiplier = 1f;
        }
        
		float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(controller.m_Rigidbody2D.linearVelocity.y));
		gravityMultiplier *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);

        controller.OverrideGravity(gravityMultiplier);
    }
}
