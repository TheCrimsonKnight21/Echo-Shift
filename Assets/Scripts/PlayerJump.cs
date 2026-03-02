using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerJump : MonoBehaviour
{
    [SerializeField] float jumpGravity = .33f;									// Gravity scale when the player is ascending in a jump
    [SerializeField] float lowJumpGravity = 1.33f;									// Gravity scale when the player releases the jump button early
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
		}
		if (controller.m_Rigidbody2D.linearVelocity.y > 0)
		{
			if (controller.jumpHeld)
				gravityMultiplier = jumpGravity;
			else
				gravityMultiplier = lowJumpGravity;
		}
        
		float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(controller.m_Rigidbody2D.linearVelocity.y));

		gravityMultiplier *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);

        controller.OverrideGravity(gravityMultiplier);
		controller.jumpBufferTimer = 0f;
		controller.coyoteTimer = 0f;

    }
}
