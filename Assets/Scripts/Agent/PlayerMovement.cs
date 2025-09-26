using UnityEngine;

public class PlayerMovement : AgentMovement
{
    [SerializeField] private InputSO input;
    [SerializeField] private Transform camTrm;
    [SerializeField] private float camHeight = 3f;
    private CharacterController controller;
    private Animator animator;
    public float speed = 5f;
    public float jumpPower = 5f;
    public float camRotSpeed = 15f;
    private float yVelocity = 0f;
    private float gravity = -9.81f;
    private bool jumpPress = false;
    private float groundLevel = 0f; // 지상 레벨 추적
    private Vector3 previousPosition; // 이전 프레임 위치 추적

    public override void InitAgent(Agent agent)
    {
        // 플레이어는 NavMesh 대신 CharacterController 사용
        useNavMesh = false;

        base.InitAgent(agent);
        controller = GetComponent<CharacterController>();

        // 초기 groundLevel 설정
        groundLevel = transform.position.y;
        previousPosition = transform.position; // 이전 위치 초기화

        // Animator 컴포넌트 찾기
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("PlayerMovement: Animator 컴포넌트를 찾을 수 없습니다. 애니메이션 없이 동작합니다.");
        }

        // InputSO 연결 확인
        if (input != null)
        {
            input.OnJumpChange += HandleChangeJump;
        }
        else
        {
            Debug.LogError("PlayerMovement: InputSO가 설정되지 않았습니다!");
        }
    }

    protected override void Update()
    {
        if (controller == null) return;

        // UI가 활성화되면 이동과 회전을 중지 (안전성 보장)
        if (TowerPlacementSystem.Instance != null && TowerPlacementSystem.Instance.IsUIActive)
        {
            // UI 활성화 중에는 이동 완전 차단 (CharacterController는 TowerPlacementSystem에서 관리)
            return;
        }

        // Tab 키 입력 시도 체크 (UI가 열리는 순간 이동 방지)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // UI가 열리는 순간의 모든 움직임을 즉시 정지
            if (controller != null)
            {
                controller.Move(Vector3.zero); // 잔여 움직임 제거
            }

            Debug.Log($"Tab 키 입력 감지 - 플레이어 위치 즉시 고정: {transform.position}");
            return;
        }

        // 예시: WASD 입력으로 이동 (InputSO에 맞게 수정 필요)
        Vector2 moveDir = input.GetMoveDir();
        Vector3 move = new Vector3(moveDir.x, 0, moveDir.y);

        // 카메라의 y축만 적용해서 이동 방향 변환
        Quaternion camYRot = Quaternion.Euler(0, camTrm.eulerAngles.y, 0);
        move = camYRot * move;
        move *= speed;

        // Animator가 있는 경우에만 애니메이션 업데이트
        if (animator != null)
        {
            animator.SetFloat("MoveSpeed", Mathf.Lerp(animator.GetFloat("MoveSpeed"), move.sqrMagnitude > 0f ? 1 : 0, Time.deltaTime * 10));
        }

        // 중력 적용
        if (controller.isGrounded)
        {
            yVelocity = -1f;
            groundLevel = transform.position.y; // 지상 레벨 업데이트

            if (jumpPress)
                yVelocity += jumpPower;
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }
        move.y = yVelocity;

        // 이전 위치 저장 (이동 전에 저장)
        previousPosition = transform.position;

        // CharacterController 이동
        controller.Move(move * Time.deltaTime);

        if (move.sqrMagnitude > 0f)
        {
            // 카메라의 y축 기준으로 이동 방향을 변환하여, 그 방향을 바라보게 회전
            Vector3 lookDir = new Vector3(moveDir.x, 0f, moveDir.y);
            lookDir = Quaternion.Euler(0, camTrm.eulerAngles.y, 0) * lookDir;
            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

        // 카메라 위치 업데이트 (플레이어 따라다니는 스타일)
        if (camTrm != null)
        {
            // 이전 프레임과 현재 프레임의 위치 차이를 계산
            Vector3 positionDelta = transform.position - previousPosition;

            // 목표 카메라 위치 계산 (플레이어 위치 + 높이 오프셋)
            Vector3 desiredPosition = transform.position + Vector3.up * camHeight;

            // 부드러운 추적
            camTrm.position = Vector3.Lerp(camTrm.position, desiredPosition, Time.deltaTime * 20f);

            // 카메라 회전은 CamRotate.cs에서 담당하므로 위치만 업데이트
        }

    }

    private void HandleChangeJump(bool pressed)
    {
        jumpPress = pressed;
    }

    /// <summary>
    /// CharacterController와 Transform의 동기화 강제 실행
    /// UI 활성화 등에서 호출하여 위치 동기화 문제를 해결
    /// </summary>
    public void ForceSyncCharacterController()
    {
        if (controller == null) return;

        Debug.Log($"동기화 전 - Transform: {transform.position}, CC velocity: {controller.velocity}, grounded: {controller.isGrounded}");

        controller.enabled = false;
        controller.enabled = true;


        Debug.Log($"동기화 후 - Transform: {transform.position}, CC velocity: {controller.velocity}, grounded: {controller.isGrounded}");
    }


}
