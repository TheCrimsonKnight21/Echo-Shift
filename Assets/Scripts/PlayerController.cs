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
    [SerializeField] private GameObject echoPrefab;
    [SerializeField] private Transform echoSpawnPoint;

    private GameObject activeEcho;
    public PlayerMovement movement;
    public PlayerJump jump;
    public PlayerDash dash;
    public PlayerWallJump wallJump;
    private List<float> speedModifiers = new List<float>();
    
    public float jumpBufferTimer;
    public float coyoteTimer;
    
    private List<InputFrame> recordedFrames = new List<InputFrame>();
    private bool isRecording = false;
    private float echoTimer = 0f;
    private const float echoRecordDuration = 5f; // Maximum duration of echo recording in seconds
    const float k_GroundedRadius = .2f; 
	public bool m_Grounded;
    public Rigidbody2D m_Rigidbody2D;
    private bool isEcho = false;
   
    private float gravityOverride = -1f;

    private float recordingStartTime;

    public struct InputFrame
    {
        public Vector2 move;
        public bool JumpPressed;
        public bool CrouchPressed;
        public bool JumpHeld;
        public bool DashPressed;
        public float Time;
        public Vector2 Position; 
    }

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

    InputAction echoAction;

    [Header("Events")]
	[Space]

	public UnityEvent OnLandEvent;

	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnCrouchEvent;

    public interface IInputSource
    {
    float GetMove();
    bool GetJumpPressed();
    bool GetJumpHeld();
    bool GetCrouch();
    bool GetDashPressed();
    }
    private IInputSource inputSource;
public class EchoInputSource : IInputSource
{
    private List<PlayerController.InputFrame> frames;
    private int index;
    private float startTime;

    public EchoInputSource(List<PlayerController.InputFrame> recorded)
    {
        frames = recorded;
        index = 0;
        startTime = Time.fixedTime;
    }

    private PlayerController.InputFrame GetCurrentFrame()
    {
        if (frames == null || frames.Count == 0)
            return default;

        float elapsed = Time.fixedTime - startTime;

        while (index < frames.Count - 1 &&
               elapsed >= frames[index].Time - frames[0].Time)
        {
            index++;
        }

        return frames[Mathf.Clamp(index, 0, frames.Count - 1)];
    }

    private bool lastJumpPressed;

    public float GetMove() => GetCurrentFrame().move.x;

    public bool GetJumpPressed()
    {
        var frame = GetCurrentFrame();
        bool result = frame.JumpPressed && !lastJumpPressed;
        lastJumpPressed = frame.JumpPressed;
        return result;
    }

    public bool GetJumpHeld() => GetCurrentFrame().JumpHeld;
    public bool GetCrouch() => GetCurrentFrame().CrouchPressed;
    public bool GetDashPressed() => GetCurrentFrame().DashPressed;
}
public class RealInputSource : IInputSource
{
    private PlayerController controller;

    public RealInputSource(PlayerController controller)
    {
        this.controller = controller;
    }

    public float GetMove()
    {
        Vector2 moveValue = controller.moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        return moveValue.x;
    }

    public bool GetJumpPressed()
    {
        return controller.jumpAction?.WasPressedThisFrame() ?? false;
    }

    public bool GetJumpHeld()
    {
        return controller.jumpAction?.IsPressed() ?? false;
    }

    public bool GetCrouch()
    {
        return controller.crouchAction?.IsPressed() ?? false;
    }

    public bool GetDashPressed()
    {
        return controller.dashAction?.WasPressedThisFrame() ?? false;
    }
} 
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
    public InputAction MoveAction => moveAction;
    public InputAction JumpAction => jumpAction;
    public InputAction CrouchAction => crouchAction;
    public InputAction DashAction => dashAction;

    void Awake()
    {
        inputSource = new RealInputSource(this);
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
        echoAction = InputSystem.actions.FindAction("Echo");
        
       if (!isEcho)
        StartRecording();
    }

    void Update()
    {
        if (isEcho)
        return;
        if (echoAction != null && echoAction.WasPressedThisFrame())
        {
            SpawnEcho();
        }
    }
    void FixedUpdate()
    {
        ReadInput();
        if (!isEcho)
        {
            recordedFrames.Add(new InputFrame
            {
                move = new Vector2(moveValue, 0),
                JumpPressed = jumpPressed,
                JumpHeld = jumpHeld,
                DashPressed = dashPressed,
                CrouchPressed = crouch,
                Time = Time.fixedTime,
                Position = transform.position
            });

            // Remove frames older than echo duration
            while (recordedFrames.Count > 0 &&
                Time.fixedTime - recordedFrames[0].Time > echoRecordDuration)
            {
                recordedFrames.RemoveAt(0);
            }
        }
        

        if (jumpPressed)
        {
            jumpBufferTimer = 0.1f;
        }

        CheckGround();

        if (CanMove())
            movement.Move();

        jump.Jump();   // Use buffer here

        dash.Dash();
        wallJump.WallJump();

        jumpBufferTimer -= Time.fixedDeltaTime;  // subtract AFTER usage

        ApplyGravity();

    }

    void ReadInput()
    {
        moveValue = inputSource.GetMove();
        jumpPressed = inputSource.GetJumpPressed();
        jumpHeld = inputSource.GetJumpHeld();
        crouch = inputSource.GetCrouch();
        dashPressed = inputSource.GetDashPressed();
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
    public void StartRecording()
    {
        recordedFrames.Clear();
        recordingStartTime = Time.fixedTime;
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
    }
    public void SetEchoInput(List<InputFrame> frames)
    {
        isEcho = true;
        inputSource = new EchoInputSource(frames);
    }
    public void SpawnEcho()
    {
        if (recordedFrames.Count == 0)
            return;

        if (activeEcho != null)
            Destroy(activeEcho);

        Vector3 spawnPosition = transform.position;

        if (recordedFrames.Count > 0)
        {
            spawnPosition = recordedFrames[0].Position;
        }

        activeEcho = Instantiate(
            echoPrefab,
            spawnPosition,
            Quaternion.identity);
                
        PlayerController echoController =
            activeEcho.GetComponent<PlayerController>();

        echoController.SetEchoInput(
            new List<InputFrame>(recordedFrames));
        Destroy(activeEcho, echoRecordDuration);
    }
    public void ReturnToPlayerInput()
    {
        inputSource = new RealInputSource(this);
    }
    public void CheckGround()
    {
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
    }
    
}