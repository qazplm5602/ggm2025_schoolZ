using UnityEngine;
using UnityEngine.AI;

public abstract class BaseEnemy : Agent, IEnemy
{
    [Header("Enemy Base Stats")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int goldReward = 10;
    
    [Header("Visual")]
    [SerializeField] protected GameObject healthBarPrefab;
    [SerializeField] protected GameObject deathEffect;

    [Header("AI Settings")]
    [SerializeField] protected bool enablePlayerTracking = true; // 플레이어 추적 활성화
    [SerializeField] protected string playerTag = "Player"; // 플레이어 태그

    // Enemy 기본 프로퍼티들
    public virtual float MaxHealth => maxHealth;
    public float CurrentHealth { get; protected set; }
    public virtual float MoveSpeed => moveSpeed;
    public bool IsAlive { get; protected set; }

    protected Transform healthBarTransform;
    protected AgentMovement agentMovement;
    protected Transform playerTransform; // 플레이어 Transform
    protected SimpleHealthBar healthBar; // 체력바 컴포넌트
    
    protected virtual void Start()
    {
        Initialize();
    }
    
    protected virtual void Update()
    {
        if (IsAlive)
        {
            // 플레이어 추적 (활성화된 경우)
            if (enablePlayerTracking && playerTransform != null)
            {
                TrackPlayer();
            }

            Move();
            UpdateHealthBar();
        }
    }
    
    public virtual void Initialize()
    {
        CurrentHealth = MaxHealth;
        IsAlive = true;

        // AgentMovement 컴포넌트 가져오기
        agentMovement = GetComponent<AgentMovement>();
        if (agentMovement == null)
        {
            agentMovement = gameObject.AddComponent<AgentMovement>();
        }

        // 이동 속도 동기화
        agentMovement.moveSpeed = MoveSpeed;

        // 플레이어 찾기 (추적 모드인 경우)
        if (enablePlayerTracking)
        {
            FindPlayer();
        }

        // 체력바 생성 (옵션)
        if (healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab, transform);
            healthBarTransform = healthBarObj.transform;

            // SimpleHealthBar 컴포넌트 찾기
            healthBar = healthBarObj.GetComponent<SimpleHealthBar>();
            if (healthBar != null)
            {
                healthBar.Initialize(MaxHealth, CurrentHealth);
            }
        }
        else
        {
            // 체력바 프리팹이 없으면 컴포넌트로 직접 찾기 (자식 포함)
            healthBar = GetComponentInChildren<SimpleHealthBar>();
            if (healthBar != null)
            {
                healthBar.Initialize(MaxHealth, CurrentHealth);
            }
        }
    }
    
    public virtual void Move()
    {
        // AgentMovement이 이동을 처리하므로 여기서는 추가 로직만 수행
        // 기본 Enemy는 별도 로직 없음
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        Debug.Log($"{gameObject.name}이 {damage} 데미지를 받았습니다. 남은 체력: {CurrentHealth}");

        // 체력바 업데이트
        UpdateHealthBar();

        // 데미지 받을 때 추가 효과
        OnTakeDamage(damage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    public virtual void Die()
    {
        if (!IsAlive) return;
        
        IsAlive = false;
        
        Debug.Log($"{gameObject.name}이 죽었습니다!");
        
        // 죽음 이펙트 재생
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }
        
        // 보상 지급 (옵션)
        // GameManager.Instance.AddGold(goldReward);
        
        // 죽을 때 추가 효과
        OnDie();
        
        Destroy(gameObject);
    }
    
    protected virtual void OnReachedDestination()
    {
        Debug.Log($"{gameObject.name}이 끝점에 도달했습니다!");

        // 플레이어 체력 감소 등 처리 (옵션)
        // GameManager.Instance.TakeDamage(1);

        Destroy(gameObject);
    }
    
    protected virtual void UpdateHealthBar()
    {
        // SimpleHealthBar 컴포넌트가 있으면 사용
        if (healthBar != null)
        {
            healthBar.UpdateHealth(CurrentHealth);
            return;
        }

        // 기존 방식 (healthBarTransform이 있는 경우)
        if (healthBarTransform != null)
        {
            // 체력바가 항상 카메라를 향하도록
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                healthBarTransform.LookAt(mainCamera.transform);
                healthBarTransform.Rotate(0, 180, 0);
            }

            // 체력 비율에 따라 스케일 조정 (간단한 방법)
            float healthRatio = CurrentHealth / MaxHealth;
            // 실제 체력바 UI 업데이트는 여기서 처리
        }
    }
    
    // 웨이포인트 찾기는 AgentMovement에서 처리됨
    
    // 가상 메서드 - 필요시 오버라이드
    protected virtual void OnTakeDamage(float damage)
    {
        // 데미지 받을 때 추가 효과 (예: 넉백, 상태이상 등)
    }
    
    protected virtual void OnDie()
    {
        // 죽을 때 추가 효과 (예: 폭발, 아이템 드롭 등)
    }

    // 플레이어 찾기 메소드
    protected virtual void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log($"{gameObject.name}이 플레이어를 찾았습니다: {player.name}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: '{playerTag}' 태그를 가진 플레이어를 찾을 수 없습니다!");
        }
    }

    // 플레이어 추적 메소드
    protected virtual void TrackPlayer()
    {
        if (playerTransform == null || agentMovement == null) return;

        // 플레이어 위치로 이동 설정
        agentMovement.SetDestination(playerTransform.position);
    }
}