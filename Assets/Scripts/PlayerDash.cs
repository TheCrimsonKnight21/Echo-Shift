using UnityEngine;

public class PlayerDash : MonoBehaviour
{   
    [SerializeField] private float dashMultiplayer = 30f;								// Speed applied when the player dashes
    [SerializeField] private float dashDuration = 0.2f;							// Duration of the dash in seconds
	[SerializeField] private float dashCooldown = 1f;							// Cooldown time between dashes in seconds

    public float dashTimer;
	public float dashCooldownTimer;


    private PlayerController controller;

    public void Initialize(PlayerController controller)
    {
        this.controller = controller;
    }

    public void Dash()
    {
        dashTimer -= Time.fixedDeltaTime;
		dashCooldownTimer -= Time.fixedDeltaTime;

        if (controller.dashPressed && dashCooldownTimer <= 0f)
        {
            dashTimer = dashDuration; 
            dashCooldownTimer = dashCooldown; 
            controller.dashPressed = false;
            controller.TryChangeState(PlayerController.PlayerState.Dashing);
            controller.AddSpeedModifier(dashMultiplayer);
            controller.OverrideGravity(0f); 
        }
        
        if (dashTimer <= 0f && dashTimer + Time.fixedDeltaTime > 0f) // Just ended dash
        {
            controller.RemoveSpeedModifier(dashMultiplayer);
            controller.TryChangeState(PlayerController.PlayerState.Normal);
            controller.ClearGravityOverride();
        }
    }
}
