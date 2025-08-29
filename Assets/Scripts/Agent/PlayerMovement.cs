using UnityEngine;

public class PlayerMovement : AgentMovement
{
    [SerializeField] private InputSO input;
    private CharacterController controller;

    public override void InitAgent(Agent agent)
    {
        base.InitAgent(agent);
        input.OnJumpPress += HandleJump;
    }

    private void HandleJump() {
        print("jump!!!");
    }
}
