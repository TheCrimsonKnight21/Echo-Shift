using System.Linq;
using UnityEngine;

/// <summary>
/// Detects wall contact and executes wall jumps. Wall detection accounts for player sprite flipping
/// by using relative positioning rather than absolute left/right checks.
/// </summary>
public class PlayerWallJump : MonoBehaviour
{
    #region Constants
    private const float k_WallRadius = 0.2f;
    #endregion

    #region Serialized Fields
    [SerializeField] private float wallJumpSpeed = 20f;
    [SerializeField] private Transform m_RightWallCheck;
    [SerializeField] private Transform m_LeftWallCheck;
    #endregion

    #region Private Fields
    private PlayerController controller;
    private bool m_WallTouching;
    private bool m_WallTouchingRight;
    private bool m_WallTouchingLeft;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the wall jump system with a reference to the player controller.
    /// Must be called during PlayerController.Awake() before any wall jump logic executes.
    /// </summary>
    /// <param name="controller">The PlayerController instance that owns this wall jump subsystem.</param>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }
    #endregion

    #region Wall Detection
    private void FixedUpdate()
    {
        DetectWalls();
    }

    private void DetectWalls()
    {
        Collider2D[] rightWallColliders = Physics2D.OverlapCircleAll(m_RightWallCheck.position, k_WallRadius, controller.m_WhatIsGround);
        Collider2D[] leftWallColliders = Physics2D.OverlapCircleAll(m_LeftWallCheck.position, k_WallRadius, controller.m_WhatIsGround);

        /// <summary>
        /// Detects wall contact by checking Physics2D overlaps at left and right wall detection points.
        /// Dynamically adjusts detection based on player position to handle sprite flipping.
        /// Uses overlap position comparison rather than absolute directions to account for sprite flipping.
        /// Sets m_WallTouching, m_WallTouchingLeft, and m_WallTouchingRight booleans each frame.
        /// </summary>
        bool rightCheckHasColliders = rightWallColliders.Length > 0 && rightWallColliders.Any(c => c.gameObject != gameObject);
        bool leftCheckHasColliders = leftWallColliders.Length > 0 && leftWallColliders.Any(c => c.gameObject != gameObject);

        m_WallTouchingRight = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? rightCheckHasColliders : leftCheckHasColliders;
        m_WallTouchingLeft = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? leftCheckHasColliders : rightCheckHasColliders;

        m_WallTouching = m_WallTouchingRight || m_WallTouchingLeft;
    }
    #endregion

    #region Wall Jump Logic
    /// <summary>
    /// Checks if conditions are met to execute a wall jump.
    /// Requires wall contact, active jump buffer, and player not grounded.
    /// </summary>
    /// <remarks>
    /// Wall jump requires:
    /// 1. m_WallTouching - Player is in contact with a wall
    /// 2. jumpBufferTimer > 0 - Player pressed jump within the buffer window
    /// 3. !m_Grounded - Player is not on the ground (prevents jump during slide)
    /// </remarks>
    public void WallJump()
    {
        if (m_WallTouching && controller.jumpBufferTimer > 0f && !controller.m_Grounded)
        {
            ExecuteWallJump();
        }
    }

    /// <summary>
    /// Applies wall jump velocity and clears input buffers.
    /// Launches player away from wall at wallJumpSpeed with upward velocity equal to normal jump velocity.
    /// </summary>
    /// <remarks>
    /// Velocity direction determined by which wall is touched:
    /// - If touching left wall, push right (1f)
    /// - If touching right wall, push left (-1f)
    /// </remarks>
    private void ExecuteWallJump()
    {
        controller.m_Rigidbody2D.linearVelocity = new Vector2(
            (m_WallTouchingLeft ? 1f : -1f) * wallJumpSpeed,
            controller.jumpVelocity
        );
        controller.jumpBufferTimer = 0f;
        controller.coyoteTimer = 0f;
        controller.TryChangeState(PlayerController.PlayerState.WallJumping);
    }
    #endregion
}
