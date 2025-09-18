using UnityEngine;
using UnityEngine.AI;

public abstract class BaseEnemy : Agent, IEnemy
{
    [Header("Enemy Base Stats")]
    [SerializeField] protected float maxHealth = 150f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int goldReward = 20;

    [Header("Special Effects")]
    [SerializeField] protected float stunDuration = 0f; // 스턴 지속 시간

    // 슬로우 효과 관리 (여러 개 중첩 가능)
    protected System.Collections.Generic.List<SlowEffect> activeSlowEffects = new System.Collections.Generic.List<SlowEffect>();

    [System.Serializable]
    public class SlowEffect
    {
        public float duration;
        public float multiplier;
        public float startTime;

        public SlowEffect(float duration, float multiplier)
        {
            this.duration = duration;
            this.multiplier = multiplier;
            this.startTime = Time.time;
        }

        public bool IsExpired => Time.time - startTime >= duration;
        public float RemainingTime => Mathf.Max(0, duration - (Time.time - startTime));
    }
    
    [Header("Visual")]
    [SerializeField] protected GameObject healthBarPrefab;
    [SerializeField] protected GameObject deathEffect;

    [Header("AI Settings")]
    [SerializeField] protected bool enablePlayerTracking = true; // 플레이어 추적 활성화
    [SerializeField] protected string playerTag = "Player"; // 플레이어 태그

    // Enemy 기본 프로퍼티들
    public virtual float MaxHealth => maxHealth;
    public float CurrentHealth { get; protected set; }
    public virtual float MoveSpeed => moveSpeed * GetStrongestSlowMultiplier(); // 슬로우 효과 적용
    public bool IsAlive { get; protected set; }
    public bool IsStunned => stunDuration > 0f;
    public bool IsSlowed => activeSlowEffects.Count > 0;

    /// <summary>
    /// 웨이브 효과 적용을 위한 체력 배율 설정
    /// </summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (multiplier <= 0) return;

        maxHealth = Mathf.RoundToInt(maxHealth * multiplier);
        CurrentHealth = maxHealth;

    }

    /// <summary>
    /// 웨이브 효과 적용을 위한 속도 배율 설정
    /// </summary>
    public void ApplySpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0) return;

        moveSpeed *= multiplier;

        // AgentMovement에도 적용
        if (agentMovement != null)
        {
            agentMovement.moveSpeed = moveSpeed;
            // NavMeshAgent의 속도도 업데이트
            agentMovement.UpdateNavMeshSpeed();
        }

    }

    /// <summary>
    /// 스턴 효과 적용 (시간 초기화 방식)
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;

        // 새로운 스턴 시간 적용 (항상 더 긴 시간 선택)
        float newDuration = Mathf.Max(duration, stunDuration);
        bool wasStunnedBefore = IsStunned;

        stunDuration = newDuration;

        // 스턴이 처음 적용되는 경우 이동 중지
        if (!wasStunnedBefore && IsStunned)
        {
            if (agentMovement != null && agentMovement.IsUsingNavMesh)
            {
                agentMovement.StopMovement();
                Debug.Log($"{gameObject.name}: 스턴 적용으로 이동 중지 ({stunDuration:F1}초)");
            }
            else
            {
                Debug.Log($"{gameObject.name}: 스턴 적용됨 ({stunDuration:F1}초) - NavMesh 미사용");
            }
        }
        else
        {
            Debug.Log($"{gameObject.name}: 스턴 연장 ({stunDuration:F1}초)");
        }
    }

    /// <summary>
    /// 슬로우 효과 적용 (강도별 중첩 방식)
    /// </summary>
    public void ApplySlow(float duration, float multiplier)
    {
        if (duration <= 0f) return;

        multiplier = Mathf.Max(0.1f, multiplier); // 최소 10% 속도 보장

        // 같은 강도의 슬로우 효과가 이미 있는지 확인
        SlowEffect existingEffect = activeSlowEffects.Find(effect => Mathf.Approximately(effect.multiplier, multiplier));

        if (existingEffect != null)
        {
            // 같은 강도의 효과가 있으면 duration만 비교해서 더 긴 것으로 업데이트
            if (duration > existingEffect.RemainingTime)
            {
                float oldTime = existingEffect.RemainingTime;
                existingEffect.duration = duration;
                existingEffect.startTime = Time.time; // 시간 초기화
                Debug.Log($"{gameObject.name}: 슬로우 연장 ({multiplier:P0}) - {oldTime:F1}초 → {duration:F1}초");
            }
            else
            {
                Debug.Log($"{gameObject.name}: 슬로우 유지 ({multiplier:P0}) - 남은시간: {existingEffect.RemainingTime:F1}초 (새로운 {duration:F1}초 무시)");
            }
        }
        else
        {
            // 다른 강도의 효과면 새로 추가
            SlowEffect newEffect = new SlowEffect(duration, multiplier);
            activeSlowEffects.Add(newEffect);
            Debug.Log($"{gameObject.name}: 슬로우 추가 ({multiplier:P0}, {duration:F1}초) - 총 효과: {activeSlowEffects.Count}개");
        }

        // 속도 즉시 업데이트
        UpdateMovementSpeed();
    }

    /// <summary>
    /// 가장 강한 슬로우 효과의 배율을 반환
    /// </summary>
    protected float GetStrongestSlowMultiplier()
    {
        if (activeSlowEffects.Count == 0) return 1f;

        // 유효한 효과들만 필터링
        var validEffects = activeSlowEffects.FindAll(effect => !effect.IsExpired);

        if (validEffects.Count == 0) return 1f;

        // 가장 강한 슬로우 효과 찾기 (가장 작은 multiplier = 가장 강한 슬로우)
        SlowEffect strongestEffect = validEffects[0];
        foreach (var effect in validEffects)
        {
            if (effect.multiplier < strongestEffect.multiplier)
            {
                strongestEffect = effect;
            }
        }

        // 디버그: 적용되는 슬로우 효과 정보
        if (validEffects.Count > 1)
        {
            Debug.Log($"{gameObject.name}: 다중 슬로우 적용 - 가장 강한 효과: {strongestEffect.multiplier:P0} ({strongestEffect.RemainingTime:F1}초 남음), 총 효과: {validEffects.Count}개");
        }

        return strongestEffect.multiplier;
    }

    /// <summary>
    /// 이동 속도 업데이트
    /// </summary>
    protected void UpdateMovementSpeed()
    {
        if (agentMovement != null)
        {
            agentMovement.moveSpeed = MoveSpeed;
            agentMovement.UpdateNavMeshSpeed();
        }
    }

    /// <summary>
    /// 스턴/슬로우 효과 타이머 업데이트
    /// </summary>
    protected void UpdateSpecialEffects()
    {
        // 스턴 타이머 감소
        if (stunDuration > 0f)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0f)
            {
                stunDuration = 0f;
                // 스턴 해제 시 이동 재개
                if (agentMovement != null && agentMovement.IsUsingNavMesh)
                {
                    agentMovement.ResumeMovement();
                    Debug.Log($"{gameObject.name}: 스턴 해제 - 이동 재개");
                }
                else if (agentMovement != null)
                {
                    Debug.Log($"{gameObject.name}: 스턴 해제 - NavMesh 미사용");
                }
                Debug.Log($"{gameObject.name}: 스턴 해제");
            }
        }

        // 만료된 슬로우 효과들 정리
        int beforeCount = activeSlowEffects.Count;
        activeSlowEffects.RemoveAll(effect => effect.IsExpired);

        if (activeSlowEffects.Count != beforeCount)
        {
            Debug.Log($"{gameObject.name}: 슬로우 효과 정리 - {beforeCount}개 → {activeSlowEffects.Count}개");

            // 슬로우 효과가 모두 사라졌으면 속도 복구
            if (activeSlowEffects.Count == 0)
            {
                UpdateMovementSpeed();
                Debug.Log($"{gameObject.name}: 모든 슬로우 효과 해제");
            }
            else
            {
                // 남은 효과 중 가장 강한 것으로 속도 업데이트
                UpdateMovementSpeed();
            }
        }
    }

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
            // 스턴/슬로우 타이머 업데이트
            UpdateSpecialEffects();

            // 스턴 상태가 아니면 이동
            if (!IsStunned)
            {
                // 플레이어 추적 (활성화된 경우)
                if (enablePlayerTracking && playerTransform != null)
                {
                    TrackPlayer();
                }

                Move();
            }
            else
            {
                // 스턴 상태일 때는 이동 정지
                if (agentMovement != null)
                {
                    agentMovement.StopMovement();
                }
            }

            UpdateHealthBar();
        }
    }
    
    public virtual void Initialize()
    {
        CurrentHealth = MaxHealth;
        IsAlive = true;

        // 슬로우 효과 리스트 초기화
        if (activeSlowEffects == null)
        {
            activeSlowEffects = new System.Collections.Generic.List<SlowEffect>();
        }
        activeSlowEffects.Clear();

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
        
        
        // 죽음 이펙트 재생
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, transform.rotation);
        }

        // 죽을 때 추가 효과
        OnDie();
        
        Destroy(gameObject);
    }
    
    protected virtual void OnReachedDestination()
    {

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

        // 웨이브 매니저에 죽음 알림 (골드 지급 처리)
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyDeath(gameObject);
        }
        else
        {
            // WaveManager가 없으면 직접 골드 지급 (fallback)
            if (GameManager.Instance != null)
            {
                int goldBefore = GameManager.Instance.CurrentGold;
                GameManager.Instance.AddGold(goldReward);
                int goldAfter = GameManager.Instance.CurrentGold;
                Debug.Log($"적 사망 골드 지급 (fallback): {gameObject.name} → +{goldReward}G (이전: {goldBefore}G → 현재: {goldAfter}G)");
            }
        }
    }

    // 플레이어 찾기 메소드
    protected virtual void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
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