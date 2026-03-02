using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float acceleration = 40f;									// Acceleration while grounded. Higher means faster to reach max speed.
	[SerializeField] float deceleration = 160f;									// Deceleration while grounded. Higher means faster to stop from max speed.
	[SerializeField] float airAcceleration = 25f;								// Acceleration while in the air. Higher means faster to reach max speed.
    [Range(0, 1)] [SerializeField] private float crouchMultiplier = .2f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
    
    private bool crouching;
    private bool m_wasCrouching = false;
    private PlayerController controller;
    [SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching
    [SerializeField] private Transform m_CeilingCheck;							// A position marking where to check for ceilings
    const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.

    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    public void Move()
    {
        // If crouching, check to see if the character can stand up
		if (!crouching)
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, controller.m_WhatIsGround) && controller.TryChangeState(PlayerController.PlayerState.Crouching))
			{
				crouching = true;
			}
		}

	
		if (controller.m_Grounded || controller.m_AirControl)
        {
            if (!m_wasCrouching && crouching)
				{
					m_wasCrouching = true;
					controller.NotifyCrouch(true);
				}

				// Reduce the speed by the crouchSpeed multiplier
				controller.AddSpeedModifier(crouchMultiplier);

				// Disable one of the colliders when crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = false;
			} else
			{
				// Enable the collider when not crouching
				if (m_CrouchDisableCollider != null)
					m_CrouchDisableCollider.enabled = true;

				if (m_wasCrouching && controller.TryChangeState(PlayerController.PlayerState.Normal))
				{
					m_wasCrouching = false;
					controller.NotifyCrouch(false);
                    controller.RemoveSpeedModifier(crouchMultiplier);
				}
			}

            float targetSpeed = controller.moveValue * controller.CurrentMoveSpeed;
            float accelRate;

			// Apply dash boost if active
            if (Mathf.Sign(targetSpeed) != Mathf.Sign(controller.m_Rigidbody2D.linearVelocity.x) &&
				Mathf.Abs(targetSpeed) > 0.01f)
			{
				accelRate = acceleration * 2f; // faster turn
			}
			else if (Mathf.Abs(targetSpeed) > 0.01f)
			{
				accelRate = controller.m_Grounded ? acceleration : airAcceleration;
			}
			else
			{
				accelRate = deceleration;
			}
            Debug.Log($"Target Speed: {targetSpeed}, Current Speed: {controller.m_Rigidbody2D.linearVelocity.x}, Accel Rate: {accelRate}");
			float newX = Mathf.MoveTowards(
				controller.m_Rigidbody2D.linearVelocity.x,
				targetSpeed,
				accelRate * Time.fixedDeltaTime
			);

			controller.m_Rigidbody2D.linearVelocity =
				new Vector2(newX, controller.m_Rigidbody2D.linearVelocity.y);

			// If the input is moving the player right and the player is facing left...
			if (controller.moveValue > 0 && !m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
			// Otherwise if the input is moving the player left and the player is facing right...
			else if (controller.moveValue < 0 && m_FacingRight)
			{
				// ... flip the player.
				Flip();
			}
        }
    private void Flip()
	{
		// Switch the way the player is labelled as facing.
		m_FacingRight = !m_FacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}
}
