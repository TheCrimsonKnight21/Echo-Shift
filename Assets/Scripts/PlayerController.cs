using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Main controller for player character logic including movement, jumping, dashing, and echo recording.
/// Manages state transitions and coordinates input handling with subsystem modules.
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Nested Types
    
    /// <summary>
    /// Represents one frame of recorded player input for echo playback.
    /// </summary>
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

    /// <summary>
    /// Defines all possible player states for state machine behavior.
    /// </summary>
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

    /// <summary>
    /// Interface for abstracting input sources to support both player and echo playback.
    /// </summary>
    public interface IInputSource
    {
        float GetMove();
        bool GetJumpPressed();
        bool GetJumpHeld();
        bool GetCrouch();
        bool GetDashPressed();
    }
    
    #endregion

    #region Configuration - Physics
    
    [SerializeField] public float jumpVelocity = 13f;
    [SerializeField] public float baseMoveSpeed = 10f;
    [SerializeField] public float BaseGravity = 6f;
    [SerializeField] public bool m_AirControl = true;
    
    #endregion

    #region Configuration - Ground Detection
    
    [SerializeField] public LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;
    private const float k_GroundedRadius = 0.2f;
    
    #endregion

    #region Configuration - Echo System
    
    [SerializeField] private GameObject echoPrefab;
    [SerializeField] private Transform echoSpawnPoint;
    private const float echoRecordDuration = 5f;
    
    #endregion

    #region Component References
    
    public PlayerMovement movement;
    public PlayerJump jump;
    public PlayerDash dash;
    public PlayerWallJump wallJump;
    public Rigidbody2D m_Rigidbody2D;
    
    #endregion

    #region State Variables
    
    private PlayerState CurrentState = PlayerState.Normal;
    public bool m_Grounded;
    private float gravityOverride = -1f;
    private List<float> speedModifiers = new List<float>();
    
    #endregion

    #region Input State
    
    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouch;
    public float moveValue;
    public bool dashPressed;
    
    public float jumpBufferTimer;
    public float coyoteTimer;
    
    #endregion

    #region Input System Actions
    
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction dashAction;
    private InputAction echoAction;
    private IInputSource inputSource;
    
    #endregion

    #region Recording System
    
    private List<InputFrame> recordedFrames = new List<InputFrame>();
    private bool isRecording = false;
    private float recordingStartTime;
    private bool isEcho = false;
    private GameObject activeEcho;
    
    #endregion

    #region Events
    
    [Header("Events")]
    [Space]
    
    public UnityEvent OnLandEvent;
    
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnCrouchEvent;
    
    #endregion

    #region Properties
    
    /// <summary>
    /// Calculates final movement speed with all active modifiers applied.
    /// </summary>
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
    
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes components and event handlers.
    /// </summary>
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

    /// <summary>
    /// Initializes input actions and starts recording.
    /// </summary>
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

    #endregion

    #region Update Loops

    /// <summary>
    /// Handles input-based logic during the update phase.
    /// </summary>
    void Update()
    {
        if (isEcho)
            return;
            
        if (echoAction != null && echoAction.WasPressedThisFrame())
        {
            SpawnEcho();
        }
    }

    /// <summary>
    /// Main physics and logic update loop. Reads input, records frames, applies physics, and updates subsystems.
    /// </summary>
    void FixedUpdate()
    {
        ReadInput();
        
        // Update jump buffer timer
        if (jumpPressed)
        {
            jumpBufferTimer = 0.1f;
        }
        jumpBufferTimer -= Time.fixedDeltaTime;
        
        // Record input frame for echo playback if not an echo
        if (!isEcho)
        {
            RecordInputFrame();
        }

        // Update ground state
        CheckGround();

        // Apply subsystem updates
        if (CanMove())
            movement.Move();

        jump.Jump();
        dash.Dash();
        wallJump.WallJump();

        // Apply gravity last
        ApplyGravity();
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Reads current input values from the active input source.
    /// </summary>
    void ReadInput()
    {
        moveValue = inputSource.GetMove();
        jumpPressed = inputSource.GetJumpPressed();
        jumpHeld = inputSource.GetJumpHeld();
        crouch = inputSource.GetCrouch();
        dashPressed = inputSource.GetDashPressed();
    }

    #endregion

    #region Physics & Ground Detection

    /// <summary>
    /// Updates ground state and invokes landing event when transitioning from air to ground.
    /// </summary>
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

    /// <summary>
    /// Applies gravity scaling, either from override or from base gravity value.
    /// </summary>
    private void ApplyGravity()
    {
        if (gravityOverride >= 0f)
            m_Rigidbody2D.gravityScale = gravityOverride;
        else
            m_Rigidbody2D.gravityScale = BaseGravity;
    }

    /// <summary>
    /// Temporarily override gravity with a custom multiplier.
    /// </summary>
    public void OverrideGravity(float multiplier)
    {
        gravityOverride = multiplier;
    }

    /// <summary>
    /// Clears gravity override and returns to base gravity.
    /// </summary>
    public void ClearGravityOverride()
    {
        gravityOverride = -1f;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Attempts to transition to a new player state if the transition is valid.
    /// </summary>
    public bool TryChangeState(PlayerState newState)
    {
        if (!IsValidTransition(CurrentState, newState))
            return false;

        CurrentState = newState;
        return true;
    }

    /// <summary>
    /// Determines if a state transition is allowed based on current state rules.
    /// </summary>
    public bool IsValidTransition(PlayerState from, PlayerState to)
    {
        switch (from)
        {
            case PlayerState.Normal:
                return true;

            case PlayerState.Crouching:
                return to == PlayerState.Normal;
                
            case PlayerState.Dashing:
                return to == PlayerState.Normal || to == PlayerState.Attacking;
                
            case PlayerState.WallJumping:
                return to == PlayerState.Normal;
                
            case PlayerState.Attacking:
                return to == PlayerState.Normal;
                
            case PlayerState.EchoPlayback:
                return to == PlayerState.Normal;
                
            case PlayerState.Dead:
                return false;
                
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if movement and actions can currently be processed.
    /// </summary>
    bool CanMove()
    {
        return CurrentState != PlayerState.EchoPlayback
            && CurrentState != PlayerState.Dead;
    }

    #endregion

    #region Speed Modification

    /// <summary>
    /// Adds a speed multiplier modifier (e.g., crouch reduces speed).
    /// </summary>
    public void AddSpeedModifier(float modifier)
    {
        speedModifiers.Add(modifier);
    }

    /// <summary>
    /// Removes a speed multiplier modifier.
    /// </summary>
    public void RemoveSpeedModifier(float modifier)
    {
        speedModifiers.Remove(modifier);
    }

    #endregion

    #region Event Notifications

    /// <summary>
    /// Invokes the landing event to notify listeners of ground contact.
    /// </summary>
    public void NotifyLanded()
    {
        OnLandEvent.Invoke();
    }

    /// <summary>
    /// Invokes the crouch event to notify listeners of crouch state changes.
    /// </summary>
    public void NotifyCrouch(bool isCrouching)
    {
        OnCrouchEvent.Invoke(isCrouching);
    }

    #endregion

    #region Recording & Echo System

    /// <summary>
    /// Records the current input frame for echo playback.
    /// Automatically removes frames older than the maximum echo duration.
    /// </summary>
    private void RecordInputFrame()
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

        // Maintain recording buffer size
        while (recordedFrames.Count > 0 &&
            Time.fixedTime - recordedFrames[0].Time > echoRecordDuration)
        {
            recordedFrames.RemoveAt(0);
        }
    }

    /// <summary>
    /// Starts recording player input frames.
    /// </summary>
    public void StartRecording()
    {
        recordedFrames.Clear();
        recordingStartTime = Time.fixedTime;
        isRecording = true;
    }

    /// <summary>
    /// Stops recording player input frames.
    /// </summary>
    public void StopRecording()
    {
        isRecording = false;
    }

    /// <summary>
    /// Switches input source to recorded frames for echo playback.
    /// </summary>
    public void SetEchoInput(List<InputFrame> frames)
    {
        isEcho = true;
        inputSource = new EchoInputSource(frames);
    }

    /// <summary>
    /// Returns input source to actual player input.
    /// </summary>
    public void ReturnToPlayerInput()
    {
        inputSource = new RealInputSource(this);
    }

    /// <summary>
    /// Creates an echo clone that replays recorded input frames.
    /// Automatically destroys the echo after the recording duration.
    /// </summary>
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
                
        PlayerController echoController = activeEcho.GetComponent<PlayerController>();

        echoController.SetEchoInput(
            new List<InputFrame>(recordedFrames));
            
        Destroy(activeEcho, echoRecordDuration);
    }

    #endregion

    #region Input Source Implementations

    /// <summary>
    /// Provides input by replaying recorded input frames for echo playback.
    /// </summary>
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

    /// <summary>
    /// Provides input from the actual input system for normal player control.
    /// </summary>
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

    #endregion
}