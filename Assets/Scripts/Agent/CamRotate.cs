using UnityEngine;

public class CamRotate : MonoBehaviour
{
    [SerializeField] private InputSO input;
    public float mouseSensitivity = 3f;
    public float xClamp = 80f;
    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Tab 키 입력 시도 체크 (UI가 열리는 순간 회전 방지)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
        {
            return;
        }

        // 마우스 입력 처리
        Vector2 mouseDir = input.GetLookDir() * mouseSensitivity * Time.deltaTime;

        // 마우스 입력이 있으면 회전 적용
        if (mouseDir.sqrMagnitude > 0.001f)
        {
            // X축 회전 (상하 회전) - 누적
            xRotation -= mouseDir.y;
            xRotation = Mathf.Clamp(xRotation, -xClamp, xClamp);

            // Y축 회전 (좌우 회전) - 누적
            float yRotation = transform.localEulerAngles.y + mouseDir.x;

            // 새로운 로컬 회전 설정
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }
}
