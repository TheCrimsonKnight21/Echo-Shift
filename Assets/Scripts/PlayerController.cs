using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    #region Serialized Fields
    [Header("Movement")]
    [SerializeField] public float baseMoveSpeed = 10f;
    [SerializeField] public bool m_AirControl = true;

    [Header("Jumping")]
    [SerializeField] public float jumpVelocity = 13f;
    [SerializeField] public float BaseGravity = 6f;

    [Header("Ground & Wall Detection")]
    [SerializeField] public LayerMask m_WhatIsGround;
    [SerializeField] private Transform m_GroundCheck;

    [Header("Echo System")]
    [SerializeField] private GameObject echoPrefab;
    #endregion

    #region Constants
    private const float k_GroundedRadius = 0.2f;
    private const float echoRecordDuration = 5f;
    #endregion

    #region Component References
    [Header("Components")]
    public PlayerMovement movement;
    public PlayerJump jump;
    public PlayerDash dash;
    public PlayerWallJump wallJump;
    public Rigidbody2D m_Rigidbody2D;
    #endregion

    #region State & Input
     [Header("States & Inputs")]
    public PlayerState CurrentState = PlayerState.Normal;
    public bool m_Grounded;
    
    private IInputSource inputSource;
    private bool isEcho = false;
    #endregion

    #region Input Values (Frame-based)
    public float moveValue;
    public bool jumpPressed;
    public bool jumpHeld;
    public bool crouch;
    public bool dashPressed;
    
    private float cachedMove;
    private bool cachedJumpPressed;
    private bool cachedJumpHeld;
    private bool cachedDashPressed;
    private bool cachedCrouch;
    #endregion

    #region Timers
    public float jumpBufferTimer;
    public float coyoteTimer;
    #endregion

    #region Movement Modifiers
    private List<float> speedModifiers = new List<float>();
    private float gravityOverride = -1f;
    #endregion

    #region Echo Recording
    private List<InputFrame> recordedFrames = new List<InputFrame>();
    #endregion

    #region Input System Actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction dashAction;
    private InputAction echoAction;
    #endregion

    #region Enums
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
    #endregion

    #region Nested Types
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

    public interface IInputSource
    {
        float GetMove();
        bool GetJumpPressed();
        bool GetJumpHeld();
        bool GetCrouch();
        bool GetDashPressed();
    }
    #endregion

    #region Events
    [Header("Events")]
    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
    public BoolEvent OnCrouchEvent;
    #endregion

    #region Properties
    public float CurrentMoveSpeed
    {
        get
        {
            float finalMultiplier = 1f;
            if (speedModifiers.Count == 0)
                return baseMoveSpeed;
            foreach (var mod in speedModifiers)
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
    private void Awake()
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

    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        dashAction = InputSystem.actions.FindAction("Dash");
        echoAction = InputSystem.actions.FindAction("Echo");

        if (!isEcho)
            StartRecording();
    }
    #endregion

    #region Update Loop
    private void Update()
    {
        if (inputSource == null)
            return;

        cachedMove = inputSource.GetMove();

        if (inputSource.GetJumpPressed())
            cachedJumpPressed = true;

        cachedJumpHeld = inputSource.GetJumpHeld();

        if (inputSource.GetDashPressed())
            cachedDashPressed = true;

        cachedCrouch = inputSource.GetCrouch();

        if (cachedJumpPressed)
            jumpBufferTimer = 0.1f;

        if (isEcho)
            return;

        if (echoAction != null && echoAction.WasPressedThisFrame())
            SpawnEcho();
    }

    private void FixedUpdate()
    {
        moveValue = cachedMove;
        jumpPressed = cachedJumpPressed;
        jumpHeld = cachedJumpHeld;
        dashPressed = cachedDashPressed;
        crouch = cachedCrouch;
        cachedJumpPressed = false;
        cachedDashPressed = false;

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

        CheckGround();

        if (CanMove())
            movement.Move();

        jump.Jump();
        dash.Dash();
        wallJump.WallJump();

        jumpBufferTimer -= Time.fixedDeltaTime;

        ApplyGravity();
    }
    #endregion

    #region Public Methods
    public bool TryChangeState(PlayerState newState)
    {
        if (!IsValidTransition(CurrentState, newState))
            return false;

        CurrentState = newState;
        return true;
    }

    public void OverrideGravity(float multiplier)
    {
        gravityOverride = multiplier;
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
    }

    public void SetEchoInput(List<InputFrame> frames)
    {
        isEcho = true;
        inputSource = new EchoInputSource(frames);
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
    #endregion

    #region Private Methods
    private bool CanMove()
    {
        return CurrentState != PlayerState.EchoPlayback
            && CurrentState != PlayerState.Dead;
    }

    private bool IsValidTransition(PlayerState from, PlayerState to)
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

    private void ApplyGravity()
    {
        if (gravityOverride >= 0f)
            m_Rigidbody2D.gravityScale = gravityOverride;
        else
            m_Rigidbody2D.gravityScale = BaseGravity;
    }

    private void SpawnEcho()
    {
        if (recordedFrames.Count == 0)
            return;

        GameObject oldEcho = transform.Find("Echo")?.gameObject;
        if (oldEcho != null)
            Destroy(oldEcho);

        Vector3 spawnPosition = recordedFrames.Count > 0 ? recordedFrames[0].Position : transform.position;

        GameObject echoInstance = Instantiate(echoPrefab, spawnPosition, Quaternion.identity);
        PlayerController echoController = echoInstance.GetComponent<PlayerController>();

        echoController.SetEchoInput(new List<InputFrame>(recordedFrames));
        Destroy(echoInstance, echoRecordDuration);
    }
    #endregion

    #region Input Source Implementations
    private class EchoInputSource : IInputSource
    {
        private List<PlayerController.InputFrame> frames;
        private int index;
        private float startTime;
        private bool lastJumpPressed;
        private bool lastDashPressed;

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

        public bool GetDashPressed()
        {
            var frame = GetCurrentFrame();
            bool result = frame.DashPressed && !lastDashPressed;
            lastDashPressed = frame.DashPressed;
            return result;
        }
    }

    private class RealInputSource : IInputSource
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