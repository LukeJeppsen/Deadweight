using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Receives the Player Input messages (Move / Jump / Dash / Attack / Interact) and exposes them to the controller.
/// Set Locked = true (cutscenes, death, pickups, pause) and everything reads as "nothing pressed".
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private Vector2 move;
    private bool jumpHeld;

    /// <summary>While true the player ignores all input.</summary>
    public bool Locked;

    public Vector2 MoveInput { get { return Locked ? Vector2.zero : move; } }
    public bool JumpHeld { get { return !Locked && jumpHeld; } }
    public bool JumpTriggered { get; private set; }
    public bool DashTriggered { get; private set; }
    public bool AttackTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }

    // Called by Player Input component
    public void OnMove(InputValue value)
    {
        move = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jumpHeld = value.isPressed;
        if (jumpHeld && !Locked) JumpTriggered = true;
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && !Locked) DashTriggered = true;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !Locked) AttackTriggered = true;
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed && !Locked) InteractTriggered = true;
    }

    private void LateUpdate()
    {
        // The jump trigger lasts one frame so the controller doesn't think you pressed jump every frame.
        JumpTriggered = false;
    }

    public void ResetDashTrigger() { DashTriggered = false; }
    public void ResetAttackTrigger() { AttackTriggered = false; }
    public void ResetInteractTrigger() { InteractTriggered = false; }

    public void ClearAll()
    {
        JumpTriggered = false;
        DashTriggered = false;
        AttackTriggered = false;
        InteractTriggered = false;
    }
}
