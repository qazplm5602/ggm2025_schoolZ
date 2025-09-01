using UnityEngine;

public class PlayerMovement : AgentMovement
{
    [SerializeField] private InputSO input;
    [SerializeField] private Transform camTrm;
    [SerializeField] private float camHeight;
    private CharacterController controller;
    public float speed = 5f;
    public float jumpPower = 5f;
    public float camRotSpeed = 15f;
    private float yVelocity = 0f;
    private float gravity = -9.81f;
    private bool jumpPress = false;

    public override void InitAgent(Agent agent)
    {
        base.InitAgent(agent);
        controller = GetComponent<CharacterController>();
        input.OnJumpChange += HandleChangeJump;
    }

    private void Update()
    {
        if (controller == null) return;

        // 예시: WASD 입력으로 이동 (InputSO에 맞게 수정 필요)
        Vector2 moveDir = input.GetMoveDir();
        Vector3 move = new Vector3(moveDir.x, 0, moveDir.y);
        move = transform.TransformDirection(move);
        move *= speed;

        if (move.sqrMagnitude > 0f)
        {
            // 카메라의 y축을 기준으로 월드 forward 방향으로 회전
            Vector3 worldForward = Quaternion.Euler(0, camTrm.eulerAngles.y, 0) * Vector3.forward;
            Quaternion targetRot = Quaternion.LookRotation(worldForward, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * camRotSpeed);
        }
        camTrm.position = transform.position + Vector3.up * camHeight;

        // 중력 적용
        if (controller.isGrounded)
        {
            yVelocity = -1f;

            if (jumpPress)
                yVelocity += jumpPower;
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }
        move.y = yVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private void HandleChangeJump(bool pressed)
    {
        jumpPress = pressed;
    }
}
