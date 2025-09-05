using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Components")]
    [SerializeField] public Image healthFillImage;
    [SerializeField] public TextMeshProUGUI healthText;

    [Header("Settings")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float mediumHealthThreshold = 0.6f;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    private float maxHealth = 100f;
    private float currentHealth = 100f;

    private void Awake()
    {
        // 자동으로 컴포넌트 찾기
        if (healthFillImage == null)
        {
            healthFillImage = GetComponentInChildren<Image>();
        }

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Initialize(float maxHp, float currentHp)
    {
        maxHealth = maxHp;
        currentHealth = currentHp;
        UpdateHealthBar();
    }

    public void UpdateHealth(float newCurrentHp)
    {
        currentHealth = Mathf.Clamp(newCurrentHp, 0, maxHealth);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage != null)
        {
            // 체력 비율 계산
            float healthRatio = currentHealth / maxHealth;
            healthFillImage.fillAmount = healthRatio;

            // 체력에 따른 색상 변경
            if (healthRatio > mediumHealthThreshold)
            {
                healthFillImage.color = fullHealthColor;
            }
            else if (healthRatio > lowHealthThreshold)
            {
                healthFillImage.color = mediumHealthColor;
            }
            else
            {
                healthFillImage.color = lowHealthColor;
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
    }

    // 자동 회전 (카메라를 바라보도록)
    private void LateUpdate()
    {
        // 카메라 방향으로 회전
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
