using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct SlowEffect
{
    public float duration, multiplier, startTime;

    public SlowEffect(float duration, float multiplier)
    {
        this.duration = duration;
        this.multiplier = multiplier;
        this.startTime = Time.time;
    }

    public bool IsExpired => Time.time - startTime >= duration;
    public float RemainingTime => Mathf.Max(0, duration - (Time.time - startTime));
}

/// <summary>
/// 적 기본 클래스
/// </summary>
public abstract class BaseEnemy : Agent, IEnemy
{
    [Header("기본 스탯")]
    [SerializeField] protected float maxHealth = 150f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected int goldReward = 20;

    [Header("속도 랜덤화")]
    [SerializeField] protected float speedVariationMin = -0.3f;
    [SerializeField] protected float speedVariationMax = 0.3f;

    [Header("특수 효과")]
    [SerializeField] protected float stunDuration = 0f;
    [SerializeField] protected GameObject deathEffect;
    [SerializeField] protected Transform healthBarTransform;

    // 상태 추적 변수들
    protected bool isJumping = false;
    protected float pathFailureTime = 0f;
    protected const float JUMP_THRESHOLD_TIME = 0.3f;
    protected NavMeshPath lastCalculatedPath;

    // 경로 검사 최적화
    protected float lastPathCheckTime = 0f;
    protected const float PATH_CHECK_INTERVAL = 1.0f; // 1초마다 경로 검사

    // 경로 존재 여부 캐싱 (성능 최적화)
    protected bool? cachedPathExists = null;
    protected float lastPathCacheTime = 0f;
    protected const float PATH_CACHE_DURATION = 0.2f; // 0.2초 동안 캐시 유지

    // 컴포넌트 레퍼런스
    protected AgentMovement agentMovement;
    protected Transform playerTransform;
    protected SimpleHealthBar healthBar;

    // 슬로우 효과 관리
    protected List<SlowEffect> activeSlowEffects = new List<SlowEffect>();

    // 디버그용
    [SerializeField] protected bool forceJumpTest = false;

    public virtual float MaxHealth => maxHealth;
    public float CurrentHealth { get; protected set; }
    public virtual float MoveSpeed => moveSpeed * GetStrongestSlowMultiplier();

    protected float GetStrongestSlowMultiplier()
    {
        if (activeSlowEffects.Count == 0) return 1f;
        var validEffects = activeSlowEffects.FindAll(effect => !effect.IsExpired);
        return validEffects.Count == 0 ? 1f : validEffects.Min(effect => effect.multiplier);
    }

    public void ApplyHealthMultiplier(float multiplier)
    {
        if (multiplier <= 0) return;
        maxHealth = Mathf.RoundToInt(maxHealth * multiplier);
        CurrentHealth = maxHealth;
    }

    public void ApplySpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0) return;
        moveSpeed *= multiplier;
        UpdateMovementSpeed();
    }

    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;

        bool wasStunnedBefore = stunDuration > 0f;
        stunDuration = Mathf.Max(duration, stunDuration);

        if (!wasStunnedBefore && agentMovement?.IsUsingNavMesh == true)
            agentMovement.StopMovement();
    }

    public void ApplySlow(float duration, float multiplier)
    {
        if (duration <= 0f) return;
        multiplier = Mathf.Max(0.1f, multiplier);

        SlowEffect existingEffect = activeSlowEffects.Find(effect => Mathf.Approximately(effect.multiplier, multiplier));
        if (existingEffect.startTime != 0f)
        {
            if (duration > existingEffect.RemainingTime)
            {
                existingEffect.duration = duration;
                existingEffect.startTime = Time.time;
            }
        }
        else
        {
            activeSlowEffects.Add(new SlowEffect(duration, multiplier));
        }
        UpdateMovementSpeed();
    }

    protected void UpdateMovementSpeed()
    {
        if (agentMovement != null)
        {
            agentMovement.moveSpeed = MoveSpeed;
            agentMovement.UpdateNavMeshSpeed();
        }
    }

    protected void UpdateSpecialEffects()
    {
        // 스턴 타이머 업데이트
        if (stunDuration > 0f)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0f)
            {
                stunDuration = 0f;
                agentMovement?.ResumeMovement();
            }
        }

        // 만료된 슬로우 효과 제거
        int beforeCount = activeSlowEffects.Count;
        activeSlowEffects.RemoveAll(effect => effect.IsExpired);

        if (activeSlowEffects.Count != beforeCount)
            UpdateMovementSpeed();
    }

    public virtual void Initialize()
    {
        CurrentHealth = MaxHealth;
        IsAlive = true;

        agentMovement = GetComponent<AgentMovement>() ?? gameObject.AddComponent<AgentMovement>();
        ApplySpeedRandomization();

        FindPlayer();
        SetupHealthBar();
    }

    private void ApplySpeedRandomization()
    {
        float speedVariation = Random.Range(speedVariationMin, speedVariationMax);
        moveSpeed = Mathf.Max(0.5f, moveSpeed + speedVariation);
        agentMovement.moveSpeed = MoveSpeed;
    }

    private void SetupHealthBar()
    {
        healthBar = GetComponentInChildren<SimpleHealthBar>();
        if (healthBar != null) healthBar.Initialize(MaxHealth, CurrentHealth);
    }

    public virtual void Move()
    {
        if (playerTransform != null && agentMovement != null)
            TrackPlayer();
    }

    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        healthBar?.UpdateHealth(CurrentHealth);

        if (CurrentHealth <= 0) Die();
        else OnTakeDamage(damage);
    }

    public virtual void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;

        // 상태 초기화
        isJumping = false;
        pathFailureTime = 0f;
        lastCalculatedPath = null;

        // 죽음 이펙트
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, transform.rotation);

        OnDie();

        // 골드 지급
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemyDeath(gameObject);
        else
            GameManager.Instance?.AddGold(goldReward);

        Destroy(gameObject);
    }

    protected virtual void OnTakeDamage(float damage) { }
    protected virtual void OnDie() { }

    protected virtual void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player?.transform;
        if (playerTransform == null) Debug.LogWarning($"{name}: 플레이어를 찾을 수 없습니다!");
    }

    protected virtual void TrackPlayer()
    {
        if (playerTransform == null || agentMovement == null || isJumping) return;
        // NavMeshAgent 상태 확인
        var navAgent = agentMovement.GetComponent<NavMeshAgent>();
        if (navAgent != null && (!navAgent.isOnNavMesh || !navAgent.isActiveAndEnabled))
        {
            StartCoroutine(JumpToPlayer());
            return;
        }

        // 목적지 설정
        try
        {
            agentMovement.SetDestination(playerTransform.position);
        }
        catch
        {
            StartCoroutine(JumpToPlayer());
            return;
        }

        // 경로가 없으면 타이머 증가, 있으면 리셋
        if (!CheckActualPathExists())
        {
            pathFailureTime += Time.deltaTime;
            if (pathFailureTime >= JUMP_THRESHOLD_TIME) // 0.1초 후 점프
            {
                StartCoroutine(JumpToPlayer());
                pathFailureTime = 0f;
            }
        }
        else
        {
            pathFailureTime = 0f;
        }
    }

    private bool CheckActualPathExists()
    {
        // 캐시된 결과가 유효한지 확인
        if (cachedPathExists.HasValue && Time.time - lastPathCacheTime < PATH_CACHE_DURATION)
        {
            return cachedPathExists.Value;
        }

        if (playerTransform == null)
        {
            cachedPathExists = false;
            lastPathCacheTime = Time.time;
            return false;
        }

        // NavMesh 위의 유효한 포인트가 있는지 먼저 확인
        bool enemyValid = NavMesh.SamplePosition(transform.position, out NavMeshHit enemyHit, 1.0f, NavMesh.AllAreas);
        bool playerValid = NavMesh.SamplePosition(playerTransform.position, out NavMeshHit playerHit, 1.0f, NavMesh.AllAreas);

        // 둘 다 유효한 포인트가 있어야 경로 계산 진행
        if (!enemyValid || !playerValid)
        {
            cachedPathExists = false;
            lastPathCacheTime = Time.time;
            return false;
        }

        lastCalculatedPath = new NavMeshPath();
        bool pathExists = NavMesh.CalculatePath(enemyHit.position, playerHit.position, NavMesh.AllAreas, lastCalculatedPath) &&
                         lastCalculatedPath.status == NavMeshPathStatus.PathComplete;

        // 결과 캐싱
        cachedPathExists = pathExists;
        lastPathCacheTime = Time.time;

        return pathExists;
    }


    protected virtual void CheckNavMeshAgentPath()
    {
        if (!IsAlive || playerTransform == null || agentMovement == null) return;

        // 보험용: 경로가 없고 일정 시간 지나면 점프
        // TrackPlayer()에서 이미 검사하므로 여기서는 최소한의 검사만 수행
        if (cachedPathExists.HasValue && !cachedPathExists.Value && pathFailureTime >= JUMP_THRESHOLD_TIME)
        {
            StartCoroutine(JumpToPlayer());
            pathFailureTime = 0f;
        }
    }

    protected virtual System.Collections.IEnumerator JumpToPlayer()
    {
        if (!IsAlive || playerTransform == null || isJumping) yield break;
        isJumping = true;

        // NavMeshAgent 비활성화
        if (agentMovement?.IsUsingNavMesh == true)
        {
            agentMovement.StopMovement();
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null) navAgent.enabled = false;
        }

        // 점프 실행
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform.position;
        targetPos.y = playerTransform.position.y;

        float duration = 1.5f;
        float elapsed = 0f;

        while (elapsed < duration && IsAlive)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y = startPos.y + Mathf.Sin(t * Mathf.PI) * 2f; // 간단한 포물선

            transform.position = currentPos;
            yield return null;
        }

        // 착지 처리
        if (IsAlive)
        {
            transform.position = targetPos;

            // NavMeshAgent 재활성화
            if (agentMovement != null)
            {
                var navAgent = GetComponent<NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.enabled = true;
                    agentMovement.SetDestination(playerTransform.position);
                }
            }

            OnReachedDestination();
        }

        isJumping = false;
    }

    protected virtual void OnReachedDestination() { }

    protected virtual void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.UpdateHealth(CurrentHealth);
        else if (healthBarTransform != null)
        {
            if (Camera.main != null)
            {
                healthBarTransform.LookAt(Camera.main.transform);
                healthBarTransform.Rotate(0, 180, 0);
            }
        }
    }

    public bool IsAlive { get; protected set; }

    private void Start() => Initialize();
    private void Update()
    {
        if (!IsAlive) return;

        UpdateSpecialEffects();
        Move();

        // 경로 검사는 일정 간격으로만 수행 (성능 최적화)
        if (Time.time - lastPathCheckTime >= PATH_CHECK_INTERVAL)
        {
            CheckNavMeshAgentPath();
            lastPathCheckTime = Time.time;
        }

        UpdateHealthBar();
    }
}