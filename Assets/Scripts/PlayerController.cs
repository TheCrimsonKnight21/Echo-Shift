using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float jumpVelocity = 13f;							      	// Amount of force added when the player jumps.
    [SerializeField] public float baseMoveSpeed = 10f;                                      // Speed at which the player runs horizontally. Higher means faster.
    [SerializeField] public float BaseGravity = 6f;

    [SerializeField] public bool m_AirControl = true;							// Whether or not a player can steer while jumping;
    [SerializeField] public LayerMask m_WhatIsGround;							// A mask determining what is ground to the character
	[SerializeField] private Transform m_GroundCheck;
    
    public PlayerMovement movement;
    public PlayerJump jump;
    public PlayerDash dash;
    public PlayerWallJump wallJump;
    private List<float> speedModifiers = new List<float>();
    
    public float jumpBufferTimer;
    public float coyoteTimer;
    
    const float k_GroundedRadius = .2f; 
	public bool m_Grounded;
    public Rigidbody2D m_Rigidbody2D;

   
    private float gravityOverride = -1f;



    public enum PlayerState
    {
        Normal = 0,
        Crouching = 1,
        Dashing = 2,
        WallJumping = 3,
        Attacking = 4,
        EchoPlayback = 5,
        Dead = 6
    }

    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouch;
    public float moveValue;
    public bool dashPressed;
 
    InputAction moveAction;
    InputAction jumpAction;
    InputAction crouchAction;
    PlayerState CurrentState = PlayerState.Normal;
    InputAction dashAction;

    [Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnCrouchEvent;


    public float CurrentMoveSpeed
    {
        get
        {
            float finalMultiplier = 1f;
            if (speedModifiers.Count == 0)
                return baseMoveSpeed;
            foreach(var mod in speedModifiers)
                finalMultiplier *= mod;

            return baseMoveSpeed * finalMultiplier;
        }
    }

    void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

		if (OnLandEvent == null)
			OnLandEvent = new UnityEvent();

		if (OnCrouchEvent == null)
			OnCrouchEvent = new BoolEvent();

        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();
        dash = GetComponent<PlayerDash>();
        wallJump = GetComponent<PlayerWallJump>();

        movement.Initialize(this);
        jump.Initialize(this);
        dash.Initialize(this);
        wallJump.Initialize(this);
    }
    void Start()
    {
        moveAction   = InputSystem.actions.FindAction("Move");
        jumpAction   = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        dashAction = InputSystem.actions.FindAction("Dash");

        if (jumpAction != null)
        {
            jumpAction.performed += ctx => { this.jumpPressed = true; this.jumpHeld = true; this.jumpBufferTimer = 0.1f; };
            jumpAction.canceled  += ctx => { this.jumpHeld = false; };
        }
        else
        {
            Debug.LogError("Jump action not found!");
        }
    }

    void Update()
    {
        
    }

    void FixedUpdate()
    {
        ReadInput();
        
        // Update jump buffer timer
        jumpBufferTimer -= Time.fixedDeltaTime;
        
        bool wasGrounded = m_Grounded;
		m_Grounded = false;

		Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i].gameObject != gameObject)
			{
				m_Grounded = true;
				if (!wasGrounded)
					OnLandEvent.Invoke();
			}
		}
        
        // Update movement
        if (CanMove())
        {
            movement.Move();
        }

        // Handle jumping
        if (this.jumpPressed)
        {
            jump.Jump();
            this.jumpPressed = false;
        }
        else
        {
            jump.Jump(); // Still apply jump gravity even when not pressing jump
        }
        
        // Handle dashing
        dash.Dash();
        
        // Handle wall jumping
        wallJump.WallJump();
        
        ApplyGravity();

    }

    void ReadInput()
    {
        Vector2 moveValue = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        // Calculate horizontal movement
        this.moveValue = moveValue.x;
        // Read crouch input
        this.crouch = crouchAction?.IsPressed() ?? false;
        // Read dash input
        this.dashPressed = dashAction?.WasPressedThisFrame() ?? false;
    }

    bool CanMove()
    {
         return CurrentState != PlayerState.EchoPlayback
        && CurrentState != PlayerState.Dead;
    }

    public bool TryChangeState(PlayerState newState)
    {
        if(!IsValidTransition(CurrentState, newState))
            return false;

        CurrentState = newState;
        return true;
    }


    public bool IsValidTransition(PlayerState from, PlayerState to)
    {
        switch (from)
        {
            case PlayerState.Normal:
                return true; // Can transition to any state from Normal

            case PlayerState.Crouching:
                return to == PlayerState.Normal; // Can only transition to Normal from Crouching
            case PlayerState.Dashing:
                return to == PlayerState.Normal || to == PlayerState.Attacking; // Can only transition to Normal or Attacking from Dashing
            case PlayerState.WallJumping:
                return to == PlayerState.Normal; // Can only transition to Normal from WallJumping
            case PlayerState.Attacking:
                return to == PlayerState.Normal; // Can only transition to Normal from Attacking
            case PlayerState.EchoPlayback:
                return to == PlayerState.Normal; // Can only transition to Normal from EchoPlayback
            case PlayerState.Dead:
                return false; // Cannot transition out of Dead
            default:
                return false;
        }
    }

    public void OverrideGravity(float multiplier)
    {
        gravityOverride = multiplier;
    }

    private void ApplyGravity()
    {
        if (gravityOverride >= 0f)
        m_Rigidbody2D.gravityScale = gravityOverride;
    else
        m_Rigidbody2D.gravityScale = BaseGravity;
    }

    public void ClearGravityOverride()
{
    gravityOverride = -1f;
}
    public void AddSpeedModifier(float modifier)
    {
        speedModifiers.Add(modifier);
    }

    public void RemoveSpeedModifier(float modifier)
    {
        speedModifiers.Remove(modifier);
    }

    public void NotifyLanded()
    {
        OnLandEvent.Invoke();
    }
    public void NotifyCrouch(bool isCrouching)
    {
        OnCrouchEvent.Invoke(isCrouching);
    }
}