// using System.Linq;
// using UnityEngine;
// using UnityEngine.Events;

// public class PlayerMovementOld : MonoBehaviour
// {

// 	[Range(0, 1)][SerializeField] private float maxSpeed = 0.4f;				// Maximum horizontal speed of the player
// 	[SerializeField] private float dashSpeed = 30f;								// Speed applied when the player dashes
	
// 	[SerializeField] private float wallJumpSpeed = 20f;							// Speed applied when the player jumps off a wall
// 	[SerializeField] private float dashDuration = 0.2f;							// Duration of the dash in seconds
// 	[SerializeField] private float dashCooldown = 1f;							// Cooldown time between dashes in seconds
// 	[SerializeField] float acceleration = 40f;									// Acceleration while grounded. Higher means faster to reach max speed.
// 	[SerializeField] float deceleration = 160f;									// Deceleration while grounded. Higher means faster to stop from max speed.
// 	[SerializeField] float airAcceleration = 25f;								// Acceleration while in the air. Higher means faster to reach max speed.
// 	[SerializeField] float jumpVelocity = 13f;							      	// Amount of force added when the player jumps.
// 	[SerializeField] float jumpGravity = 2f;									// Gravity scale when the player is ascending in a jump
// 	[SerializeField] float fallGravity = 6f;									// Gravity scale when the player is descending in a jump
// 	[SerializeField] float lowJumpGravity = 8f;									// Gravity scale when the player releases the jump button early
// 	[SerializeField] float jumpBufferTime = 0.1f;
// 	[SerializeField] float coyoteTime = 0.1f;
// 	[SerializeField] float apexThreshold = 1f;
// 	[SerializeField] float apexGravityMultiplier = 0.5f;
// 	[Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .2f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
// 	[SerializeField] private bool m_AirControl = false;							// Whether or not a player can steer while jumping;
// 	[SerializeField] public LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
// 	[SerializeField] private Transform m_GroundCheck;							// A position marking where to check if the player is grounded.
// 	[SerializeField] private Transform m_CeilingCheck;							// A position marking where to check for ceilings
// 	[SerializeField] private Collider2D m_CrouchDisableCollider;				// A collider that will be disabled when crouching

// 	[SerializeField] private Transform m_RightWallCheck;						// A position marking where to check for a wall on the right
// 	[SerializeField] private Transform m_LeftWallCheck;						// A position marking where to check for a wall on the left
// 	const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
// 	private bool m_Grounded;            // Whether or not the player is grounded.
// 	const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
// 	private Rigidbody2D m_Rigidbody2D;
// 	private bool m_FacingRight = true;  // For determining which way the player is currently facing.
// 	const float k_WallRadius = 0.2f;
// 	private bool m_WallTouching;
// 	private bool m_WallTouchingRight;
// 	private bool m_WallTouchingLeft;
// 	float coyoteTimer;
// 	float jumpBufferTimer;
// 	float dashTimer;
// 	float dashCooldownTimer;


// 	[Header("Events")]
// 	[Space]

// 	public UnityEvent OnLandEvent;

// 	[System.Serializable]
// 	public class BoolEvent : UnityEvent<bool> { }

// 	public BoolEvent OnCrouchEvent;
// 	private bool m_wasCrouching = false;

// 	private void Awake()
// 	{
// 		m_Rigidbody2D = GetComponent<Rigidbody2D>();

// 		if (OnLandEvent == null)
// 			OnLandEvent = new UnityEvent();

// 		if (OnCrouchEvent == null)
// 			OnCrouchEvent = new BoolEvent();
// 	}

// 	private void FixedUpdate()
// 	{
// 		bool wasGrounded = m_Grounded;
// 		m_Grounded = false;

// 		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
// 		// This can be done using layers instead but Sample Assets will not overwrite your project settings.
// 		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
// 		for (int i = 0; i < colliders.Length; i++)
// 		{
// 			if (colliders[i].gameObject != gameObject)
// 			{
// 				m_Grounded = true;
// 				if (!wasGrounded)
// 					OnLandEvent.Invoke();
// 			}
// 		}


// 		// Check for wall contact
// 		Collider2D[] rightWallColliders = Physics2D.OverlapCircleAll(m_RightWallCheck.position, k_WallRadius, m_WhatIsGround);
// 		Collider2D[] leftWallColliders = Physics2D.OverlapCircleAll(m_LeftWallCheck.position, k_WallRadius, m_WhatIsGround);
		
// 		// Determine which is actually right/left based on x position (accounts for player flipping)
// 		bool rightCheckHasColliders = rightWallColliders.Length > 0 && rightWallColliders.Any(c => c.gameObject != gameObject);
// 		bool leftCheckHasColliders = leftWallColliders.Length > 0 && leftWallColliders.Any(c => c.gameObject != gameObject);
		
// 		m_WallTouchingRight = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? rightCheckHasColliders : leftCheckHasColliders;
// 		m_WallTouchingLeft = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? leftCheckHasColliders : rightCheckHasColliders;


// 		m_WallTouching = m_WallTouchingRight || m_WallTouchingLeft;
// 	}


// 	/// <summary>
// 	/// Move the character horizontally and handle jumping/crouching.
// 	/// <paramref name="jump"/> should be true only on the fixed update when
// 	/// the button is first pressed; <paramref name="jumpHeld"/> should reflect
// 	/// whether the jump button is currently held down.
// 	/// </summary>
// 	public void Move()
// 	{
		
// 		if (inputData.jumpPressed)
// 			jumpBufferTimer = jumpBufferTime;
// 		else
// 			jumpBufferTimer -= Time.fixedDeltaTime;
		
// 		dashTimer -= Time.fixedDeltaTime;
// 		dashCooldownTimer -= Time.fixedDeltaTime;
// 		// If crouching, check to see if the character can stand up
// 		if (!inputData.crouch)
// 		{
// 			// If the character has a ceiling preventing them from standing up, keep them crouching
// 			if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
// 			{
// 				inputData.crouch = true;
// 			}
// 		}

// 		// only control the player if grounded or airControl is turned on
// 		if (m_Grounded || m_AirControl)
// 		{
// 			if (m_Grounded)
// 				coyoteTimer = coyoteTime;
// 			else
// 				coyoteTimer -= Time.fixedDeltaTime;
// 			// If crouching and on the ground
// 			if (inputData.crouch && m_Grounded)
// 			{
// 				if (!m_wasCrouching)
// 				{
// 					m_wasCrouching = true;
// 					OnCrouchEvent.Invoke(true);
// 				}

// 				// Reduce the speed by the crouchSpeed multiplier
// 				inputData.moveValue *= m_CrouchSpeed;

// 				// Disable one of the colliders when crouching
// 				if (m_CrouchDisableCollider != null)
// 					m_CrouchDisableCollider.enabled = false;
// 			} else
// 			{
// 				// Enable the collider when not crouching
// 				if (m_CrouchDisableCollider != null)
// 					m_CrouchDisableCollider.enabled = true;

// 				if (m_wasCrouching)
// 				{
// 					m_wasCrouching = false;
// 					OnCrouchEvent.Invoke(false);
// 				}
// 			}
		
// 			// Finding the target velocity
// 			float targetSpeed = inputData.moveValue * maxSpeed;

// 			// Check for dash input and start dash if available
// 			if (inputData.dashPressed && dashCooldownTimer <= 0f)
// 			{
// 				dashTimer = dashDuration; // 1 second dash duration
// 				dashCooldownTimer = dashCooldown; // 1 second cooldown
// 				inputData.dashPressed = false;
// 			}

// 			float accelRate;

// 			// Apply dash boost if active
// 			if (dashTimer > 0f)
// 			{
// 				if (Mathf.Abs(targetSpeed) > 0.01f)
// 				targetSpeed = Mathf.Sign(targetSpeed) * dashSpeed;
// 				else
// 			targetSpeed = (m_FacingRight ? 1f : -1f) * dashSpeed;
// 				accelRate = acceleration * 3f; // faster acceleration for dash
// 			}
// 			// change direction instantly
// 			else if (Mathf.Sign(targetSpeed) != Mathf.Sign(m_Rigidbody2D.linearVelocity.x) &&
// 				Mathf.Abs(targetSpeed) > 0.01f)
// 			{
// 				accelRate = acceleration * 2f; // faster turn
// 			}
// 			else if (Mathf.Abs(targetSpeed) > 0.01f)
// 			{
// 				accelRate = m_Grounded ? acceleration : airAcceleration;
// 			}
// 			else
// 			{
// 				accelRate = deceleration;
// 			}

// 			float newX = Mathf.MoveTowards(
// 				m_Rigidbody2D.linearVelocity.x,
// 				targetSpeed,
// 				accelRate * Time.fixedDeltaTime
// 			);

// 			m_Rigidbody2D.linearVelocity =
// 				new Vector2(newX, m_Rigidbody2D.linearVelocity.y);

// 			// If the input is moving the player right and the player is facing left...
// 			if (inputData.moveValue > 0 && !m_FacingRight)
// 			{
// 				// ... flip the player.
// 				Flip();
// 			}
// 			// Otherwise if the input is moving the player left and the player is facing right...
// 			else if (inputData.moveValue < 0 && m_FacingRight)
// 			{
// 				// ... flip the player.
// 				Flip();
// 			}
// 		}

// 		// handle jumping (initial impulse + variable height)
// 		if (coyoteTimer > 0f && jumpBufferTimer > 0f)
// 		{
// 			// start jump
// 			m_Grounded = false;
// 			m_Rigidbody2D.linearVelocity = new Vector2(m_Rigidbody2D.linearVelocity.x, jumpVelocity);

// 			jumpBufferTimer = 0f;
// 		}
// 		float gravity = fallGravity;
// 		if (m_Rigidbody2D.linearVelocity.y > 0)
// 		{
// 			if (inputData.jumpHeld)
// 				gravity = jumpGravity;
// 			else
// 				gravity = lowJumpGravity;
// 		}
// 		// Handle wall jump
// 		if (m_WallTouching && jumpBufferTimer > 0f && !m_Grounded)
// 		{
// 			m_Rigidbody2D.linearVelocity = new Vector2(
// 				(m_WallTouchingLeft ? 1f : -1f) * wallJumpSpeed,  // Push away from wall
// 				jumpVelocity
// 			);
// 			jumpBufferTimer = 0f;
// 		}


// 		float apexPoint = Mathf.InverseLerp(apexThreshold, 0, Mathf.Abs(m_Rigidbody2D.linearVelocity.y));

// 		gravity *= Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);
		
// 		// Disable gravity during dash
// 		if (dashTimer > 0f)
// 			m_Rigidbody2D.gravityScale = 0f;
// 		else
// 			m_Rigidbody2D.gravityScale = gravity;

// 		jumpBufferTimer = 0f;
// 		coyoteTimer = 0f;
// 	}

// 	private void Flip()
// 	{
// 		// Switch the way the player is labelled as facing.
// 		m_FacingRight = !m_FacingRight;

// 		// Multiply the player's x local scale by -1.
// 		Vector3 theScale = transform.localScale;
// 		theScale.x *= -1;
// 		transform.localScale = theScale;
// 	}
// }
