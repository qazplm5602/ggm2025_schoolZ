using UnityEngine;

public class BasicEnemy : BaseEnemy
{
    readonly float lowHealthThreshold = 0.1f;
    [Header("Visual Effects")]
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private Color lowHealthColor = Color.red;
    private Renderer enemyRenderer;
    private Color originalColor;
    
    protected override void Start()
    {
        base.Start();
        
        // 렌더러 컴포넌트 찾기
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }
    
    public override float MoveSpeed => moveSpeed;

    protected override void Update()
    {
        base.Update();

        // 이동 속도 동기화
        if (agentMovement != null)
        {
            agentMovement.moveSpeed = MoveSpeed;
        }
    }
    
    protected override void OnTakeDamage(float damage)
    {
        base.OnTakeDamage(damage);
        
        // 데미지 이펙트 재생
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 1f);
        }
        
        // 체력이 낮으면 색상 변경
        UpdateVisualState();
    }
    
    private void UpdateVisualState()
    {
        if (enemyRenderer == null) return;
        
        float healthRatio = CurrentHealth / MaxHealth;
        if (healthRatio <= lowHealthThreshold)
        {
            // 체력이 낮으면 빨간색으로 변경
            enemyRenderer.material.color = Color.Lerp(originalColor, lowHealthColor, 1f - (healthRatio / lowHealthThreshold));
        }
        else
        {
            // 정상 체력이면 원래 색상
            enemyRenderer.material.color = originalColor;
        }
    }
    
    protected override void OnDie()
    {
        base.OnDie();
        
        // 기본 적이 죽을 때의 추가 효과
        Debug.Log("기본 적이 처치되었습니다!");
    }
}