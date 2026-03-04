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

        // Account for player flipping by comparing check positions rather than using absolute left/right
        bool rightCheckHasColliders = rightWallColliders.Length > 0 && rightWallColliders.Any(c => c.gameObject != gameObject);
        bool leftCheckHasColliders = leftWallColliders.Length > 0 && leftWallColliders.Any(c => c.gameObject != gameObject);

        m_WallTouchingRight = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? rightCheckHasColliders : leftCheckHasColliders;
        m_WallTouchingLeft = m_RightWallCheck.position.x > m_LeftWallCheck.position.x ? leftCheckHasColliders : rightCheckHasColliders;

        m_WallTouching = m_WallTouchingRight || m_WallTouchingLeft;
    }
    #endregion

    #region Wall Jump Logic
    public void WallJump()
    {
        if (m_WallTouching && controller.jumpBufferTimer > 0f && !controller.m_Grounded)
        {
            ExecuteWallJump();
        }
    }

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
