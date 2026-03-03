using System.Linq;
using UnityEngine;

/// <summary>
/// Manages wall detection and wall jump mechanics.
/// Detects walls on both sides and allows jumping away from walls with directional velocity.
/// </summary>
public class PlayerWallJump : MonoBehaviour
{
    #region Configuration

    [SerializeField] private float wallJumpSpeed = 20f;
    private const float k_WallRadius = 0.2f;

    #endregion

    #region Wall State

    private bool m_WallTouching;
    private bool m_WallTouchingRight;
    private bool m_WallTouchingLeft;
    private PlayerController controller;

    #endregion

    #region References

    [SerializeField] private Transform m_RightWallCheck;
    [SerializeField] private Transform m_LeftWallCheck;

    #endregion

    #region Initialization

    /// <summary>
    /// Stores reference to the player controller for input and physics access.
    /// </summary>
    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

    #endregion

    #region Wall Detection

    /// <summary>
    /// Updates wall touching state by checking for colliders at wall detection positions.
    /// Accounts for player flipping by comparing check positions relative to each other.
    /// Called during FixedUpdate to maintain consistent collision checks.
    /// </summary>
    private void FixedUpdate()
    {
        UpdateWallDetection();
    }

    /// <summary>
    /// Detects walls using overlap circles at check positions and updates wall state flags.
    /// </summary>
    private void UpdateWallDetection()
    {
        // Get colliders at wall check positions
        Collider2D[] rightWallColliders = Physics2D.OverlapCircleAll(
            m_RightWallCheck.position,
            k_WallRadius,
            controller.m_WhatIsGround
        );
        Collider2D[] leftWallColliders = Physics2D.OverlapCircleAll(
            m_LeftWallCheck.position,
            k_WallRadius,
            controller.m_WhatIsGround
        );

        // Check if colliders exist and exclude the player itself
        bool rightCheckHasColliders = rightWallColliders.Length > 0 &&
            rightWallColliders.Any(c => c.gameObject != gameObject);
        bool leftCheckHasColliders = leftWallColliders.Length > 0 &&
            leftWallColliders.Any(c => c.gameObject != gameObject);

        // Assign wall states accounting for player flipping
        bool positionsFlipped = m_RightWallCheck.position.x > m_LeftWallCheck.position.x;
        m_WallTouchingRight = positionsFlipped ? rightCheckHasColliders : leftCheckHasColliders;
        m_WallTouchingLeft = positionsFlipped ? leftCheckHasColliders : rightCheckHasColliders;

        // Update overall wall touching state
        m_WallTouching = m_WallTouchingRight || m_WallTouchingLeft;
    }

    #endregion

    #region Wall Jump Logic

    /// <summary>
    /// Initiates wall jump when wall, jump input, and grounded conditions are met.
    /// Applies velocity away from the wall and upward.
    /// </summary>
    public void WallJump()
    {
        if (m_WallTouching && controller.jumpBufferTimer > 0f && !controller.m_Grounded)
        {
            // Calculate wall direction (pushes away from wall)
            float wallDirection = m_WallTouchingLeft ? 1f : -1f;

            controller.m_Rigidbody2D.linearVelocity = new Vector2(
                wallDirection * wallJumpSpeed,
                controller.jumpVelocity
            );

            // Clear input buffers
            controller.jumpBufferTimer = 0f;
            controller.coyoteTimer = 0f;

            // Transition to wall jumping state
            controller.TryChangeState(PlayerController.PlayerState.WallJumping);
        }
    }

    #endregion
}
