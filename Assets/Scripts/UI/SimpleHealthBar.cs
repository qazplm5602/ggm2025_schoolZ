using UnityEngine;

public class SimpleHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField] private Transform healthBarTransform;
    [SerializeField] private float healthBarHeight = 1.2f; // 높이 낮춤
    [SerializeField] private bool autoAdjustHeight = true; // 자동 높이 조절
    [SerializeField] private Vector3 healthBarScale = new Vector3(1f, 0.1f, 0.02f);

    [Header("Materials")]
    [SerializeField] public Material backgroundMaterial;
    [SerializeField] public Material fillMaterial;

    private GameObject backgroundBar;
    private GameObject fillBar;
    private float maxHealth = 100f;
    private float currentHealth = 100f;

    private void Awake()
    {
        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        // 자동 높이 조절
        float actualHeight = healthBarHeight;
        if (autoAdjustHeight)
        {
            // Enemy의 크기를 기준으로 높이 계산
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                actualHeight = collider.bounds.max.y + 0.3f; // 콜라이더 위쪽 + 약간의 여유
            }
            else
            {
                // 콜라이더가 없으면 기본 높이 사용
                actualHeight = healthBarHeight;
            }
        }

        // 체력바 컨테이너 생성
        GameObject healthBarContainer = new GameObject("HealthBar");
        healthBarContainer.transform.SetParent(transform);
        healthBarContainer.transform.localPosition = new Vector3(0, actualHeight, 0);

        // Enemy 크기에 맞게 체력바 크기 자동 조절
        Vector3 adjustedScale = healthBarScale;
        if (autoAdjustHeight)
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                float enemyWidth = collider.bounds.size.x;
                adjustedScale.x = Mathf.Max(0.5f, enemyWidth * 0.8f); // Enemy 너비의 80%
            }
        }

        // 배경 바 생성
        backgroundBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backgroundBar.name = "Background";
        backgroundBar.transform.SetParent(healthBarContainer.transform);
        backgroundBar.transform.localPosition = Vector3.zero;
        backgroundBar.transform.localScale = adjustedScale;

        if (backgroundMaterial != null)
        {
            backgroundBar.GetComponent<Renderer>().material = backgroundMaterial;
        }
        else
        {
            backgroundBar.GetComponent<Renderer>().material.color = Color.black;
        }

        // 체력 표시 바 생성
        fillBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fillBar.name = "Fill";
        fillBar.transform.SetParent(backgroundBar.transform);

        // 부모의 영향을 받지 않도록 초기 크기 설정
        fillBar.transform.localPosition = new Vector3(0, 0, -5f); // 왼쪽 끝, 더 앞으로 이동
        fillBar.transform.localScale = new Vector3(1f, 1f, 1f); // 부모의 스케일을 고려한 상대적 크기

        if (fillMaterial != null)
        {
            fillBar.GetComponent<Renderer>().material = fillMaterial;
        }
        else
        {
            fillBar.GetComponent<Renderer>().material.color = Color.green;
        }

        // 콜라이더 제거 (시각적 요소만)
        Destroy(backgroundBar.GetComponent<Collider>());
        Destroy(fillBar.GetComponent<Collider>());
    }

    public void Initialize(float maxHp, float currentHp)
    {
        maxHealth = maxHp;
        currentHealth = currentHp;
    }

    public void UpdateHealth(float newCurrentHp)
    {
        currentHealth = Mathf.Clamp(newCurrentHp, 0, maxHealth);
    }

    private void LateUpdate()
    {
        // 체력바가 항상 카메라를 바라보도록 회전 및 체력바 업데이트
        if (backgroundBar != null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 1. 배경바 회전 설정
                backgroundBar.transform.LookAt(mainCamera.transform);
                backgroundBar.transform.Rotate(0, 180, 0);

                // 2. 체력바 업데이트
                if (fillBar != null)
                {
                    float healthRatio = currentHealth / maxHealth;

                    // 채우기 바 크기 업데이트
                    float fillScaleX = healthRatio;
                    fillBar.transform.localScale = new Vector3(fillScaleX, 1f, 1f);

                    // 채우기 바 위치 업데이트
                    float fillOffsetX = -0.5f + (fillScaleX * 0.5f);
                    fillBar.transform.localPosition = new Vector3(fillOffsetX, 0, -0.1f);

                    // 체력에 따른 색상 변경
                    Renderer fillRenderer = fillBar.GetComponent<Renderer>();
                    if (healthRatio > 0.6f)
                    {
                        fillRenderer.material.color = Color.green;
                    }
                    else if (healthRatio > 0.3f)
                    {
                        fillRenderer.material.color = Color.yellow;
                    }
                    else
                    {
                        fillRenderer.material.color = Color.red;
                    }

                    // 체력이 0이면 숨김
                    fillBar.SetActive(currentHealth > 0);
                }
            }
        }
    }
}
