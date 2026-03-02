using System.Linq;
using UnityEngine;

public class PlayerWallJump : MonoBehaviour
{
	[SerializeField] private float wallJumpSpeed = 20f;							// Speed applied when the player jumps off a wall
    const float k_WallRadius = 0.2f;
	private bool m_WallTouching;
	private bool m_WallTouchingRight;
	private bool m_WallTouchingLeft;
    private PlayerController controller;
    [SerializeField] private Transform m_RightWallCheck;						// A position marking where to check for a wall on the right
	[SerializeField] private Transform m_LeftWallCheck;						// A position marking where to check for a wall on the left
	
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    private void FixedUpdate()
    {
        
     Collider2D[] rightWallColliders = Physics2D.OverlapCircleAll(m_RightWallCheck.position, k_WallRadius, controller.m_WhatIsGround);
		Collider2D[] leftWallColliders = Physics2D.OverlapCircleAll(m_LeftWallCheck.position, k_WallRadius, controller.m_WhatIsGround);
		
		// Determine which is actually right/left based on x position (accounts for player flipping)
		bool rightCheckHasColliders = rightWallColliders.Length > 0 && rightWallColliders.Any(c => c.gameObject != gameObject);
		bool leftCheckHasColliders = leftWallColliders.Length > 0 && leftWallColliders.Any(c => c.gameObject != gameObject);
		
		m_WallTouchingRight = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? rightCheckHasColliders : leftCheckHasColliders;
		m_WallTouchingLeft = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? leftCheckHasColliders : rightCheckHasColliders;


		m_WallTouching = m_WallTouchingRight || m_WallTouchingLeft;   
    }
    public void WallJump()
    {
       if (m_WallTouching && controller.jumpBufferTimer > 0f && !controller.m_Grounded)
		{
			controller.m_Rigidbody2D.linearVelocity = new Vector2(
				(m_WallTouchingLeft ? 1f : -1f) * wallJumpSpeed,  // Push away from wall
				controller.jumpVelocity
			);
			controller.jumpBufferTimer = 0f;
		}
    }
}
