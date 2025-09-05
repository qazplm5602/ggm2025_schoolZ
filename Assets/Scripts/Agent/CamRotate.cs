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
        Vector2 mouseDir = input.GetLookDir() * mouseSensitivity * Time.deltaTime;

        // y축(좌우) 회전: 카메라 자체에서 처리
        float yRotation = transform.localEulerAngles.y + mouseDir.x;

        // x축(상하) 회전: Clamp 적용
        xRotation -= mouseDir.y;
        xRotation = Mathf.Clamp(xRotation, -xClamp, xClamp);

        transform.localEulerAngles = new Vector3(xRotation, yRotation, 0f);
    }
}
