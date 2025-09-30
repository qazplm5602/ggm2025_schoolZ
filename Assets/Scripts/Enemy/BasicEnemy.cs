using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

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

public class BasicEnemy : MonoBehaviour
{
    [SerializeField] protected float maxHealth = 150f, moveSpeed = 3f;
    [SerializeField] protected int goldReward = 20;
    [SerializeField] protected float speedVariationMin = -0.3f, speedVariationMax = 0.3f;
    [SerializeField] protected float stunDuration = 0f;
    [SerializeField] protected GameObject deathEffect, damageEffect;
    [SerializeField] protected Transform healthBarTransform;
    [SerializeField] private Color lowHealthColor = Color.red;

    private Renderer enemyRenderer;
    private Color originalColor;
    private readonly float lowHealthThreshold = 0.1f;

    protected bool isJumping = false;
    protected float pathFailureTime = 0f, lastPathCheckTime = 0f;
    protected const float JUMP_THRESHOLD_TIME = 0.3f, PATH_CHECK_INTERVAL = 1.0f, PATH_CACHE_DURATION = 0.2f;
    protected NavMeshPath lastCalculatedPath;
    protected bool? cachedPathExists = null;
    protected float lastPathCacheTime = 0f;

    protected AgentMovement agentMovement;
    protected Transform playerTransform;
    protected SimpleHealthBar healthBar;
    protected List<SlowEffect> activeSlowEffects = new List<SlowEffect>();

    public virtual float MaxHealth => maxHealth;
    public float CurrentHealth { get; protected set; }
    public virtual float MoveSpeed => moveSpeed * GetStrongestSlowMultiplier();
    public bool IsAlive { get; protected set; }

    protected float GetStrongestSlowMultiplier() =>
        activeSlowEffects.Count == 0 ? 1f :
        activeSlowEffects.FindAll(effect => !effect.IsExpired) is var validEffects && validEffects.Count == 0 ? 1f :
        validEffects.Min(effect => effect.multiplier);

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

        var existingEffect = activeSlowEffects.Find(effect => Mathf.Approximately(effect.multiplier, multiplier));
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
        if (stunDuration > 0f)
        {
            stunDuration -= Time.deltaTime;
            if (stunDuration <= 0f)
            {
                stunDuration = 0f;
                agentMovement?.ResumeMovement();
            }
        }

        int beforeCount = activeSlowEffects.Count;
        activeSlowEffects.RemoveAll(effect => effect.IsExpired);
        if (activeSlowEffects.Count != beforeCount) UpdateMovementSpeed();
    }

    public virtual void Initialize()
    {
        CurrentHealth = MaxHealth;
        IsAlive = true;
        agentMovement = GetComponent<AgentMovement>() ?? gameObject.AddComponent<AgentMovement>();
        ApplySpeedRandomization();
        FindPlayer();
        SetupHealthBar();
        SetupRenderer();
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
        healthBar?.Initialize(MaxHealth, CurrentHealth);
    }

    private void SetupRenderer()
    {
        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null) originalColor = enemyRenderer.material.color;
    }

    public virtual void Move()
    {
        if (playerTransform != null && agentMovement != null) TrackPlayer();
    }

    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        healthBar?.UpdateHealth(CurrentHealth);
        if (CurrentHealth <= 0) Die(); else OnTakeDamage(damage);
    }

    protected virtual void OnTakeDamage(float damage)
    {
        if (damageEffect != null)
        {
            var effect = Instantiate(damageEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 1f);
        }
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (enemyRenderer == null) return;
        float healthRatio = CurrentHealth / MaxHealth;
        enemyRenderer.material.color = healthRatio <= lowHealthThreshold ?
            Color.Lerp(originalColor, lowHealthColor, 1f - (healthRatio / lowHealthThreshold)) : originalColor;
    }

    public virtual void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;
        isJumping = false;
        pathFailureTime = 0f;
        lastCalculatedPath = null;

        if (deathEffect != null) Instantiate(deathEffect, transform.position, transform.rotation);
        OnDie();

        if (WaveManager.Instance != null) WaveManager.Instance.OnEnemyDeath(gameObject);
        else GameManager.Instance?.AddGold(goldReward);

        Destroy(gameObject);
    }

    protected virtual void OnDie() { }

    protected virtual void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        playerTransform = player?.transform;
        if (playerTransform == null) Debug.LogWarning($"{name}: 플레이어를 찾을 수 없습니다!");
    }

    protected virtual void TrackPlayer()
    {
        if (playerTransform == null || agentMovement == null || isJumping) return;
        
        var navAgent = agentMovement.GetComponent<NavMeshAgent>();
        if (navAgent != null && (!navAgent.isOnNavMesh || !navAgent.isActiveAndEnabled))
        {
            StartCoroutine(JumpToPlayer());
            return;
        }

        try { agentMovement.SetDestination(playerTransform.position); }
        catch { StartCoroutine(JumpToPlayer()); return; }

        if (!CheckActualPathExists())
        {
            pathFailureTime += Time.deltaTime;
            if (pathFailureTime >= JUMP_THRESHOLD_TIME)
            {
                StartCoroutine(JumpToPlayer());
                pathFailureTime = 0f;
            }
        }
        else pathFailureTime = 0f;
    }

    private bool CheckActualPathExists()
    {
        if (cachedPathExists.HasValue && Time.time - lastPathCacheTime < PATH_CACHE_DURATION)
            return cachedPathExists.Value;

        if (playerTransform == null)
        {
            cachedPathExists = false;
            lastPathCacheTime = Time.time;
            return false;
        }

        bool enemyValid = NavMesh.SamplePosition(transform.position, out NavMeshHit enemyHit, 1.0f, NavMesh.AllAreas);
        bool playerValid = NavMesh.SamplePosition(playerTransform.position, out NavMeshHit playerHit, 1.0f, NavMesh.AllAreas);

        if (!enemyValid || !playerValid)
        {
            cachedPathExists = false;
            lastPathCacheTime = Time.time;
            return false;
        }

        lastCalculatedPath = new NavMeshPath();
        bool pathExists = NavMesh.CalculatePath(enemyHit.position, playerHit.position, NavMesh.AllAreas, lastCalculatedPath) &&
                         lastCalculatedPath.status == NavMeshPathStatus.PathComplete;

        cachedPathExists = pathExists;
        lastPathCacheTime = Time.time;
        return pathExists;
    }

    protected virtual void CheckNavMeshAgentPath()
    {
        if (!IsAlive || playerTransform == null || agentMovement == null) return;
        if (cachedPathExists.HasValue && !cachedPathExists.Value && pathFailureTime >= JUMP_THRESHOLD_TIME)
        {
            StartCoroutine(JumpToPlayer());
            pathFailureTime = 0f;
        }
    }

    protected virtual IEnumerator JumpToPlayer()
    {
        if (!IsAlive || playerTransform == null || isJumping) yield break;
        isJumping = true;

        if (agentMovement?.IsUsingNavMesh == true)
        {
            agentMovement.StopMovement();
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null) navAgent.enabled = false;
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform.position;
        targetPos.y = playerTransform.position.y;

        float duration = 1.5f, elapsed = 0f;

        while (elapsed < duration && IsAlive)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y = startPos.y + Mathf.Sin(t * Mathf.PI) * 2f;
            transform.position = currentPos;
            yield return null;
        }

        if (IsAlive)
        {
            transform.position = targetPos;
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
        if (healthBar != null) healthBar.UpdateHealth(CurrentHealth);
        else if (healthBarTransform != null && Camera.main != null)
        {
            healthBarTransform.LookAt(Camera.main.transform);
            healthBarTransform.Rotate(0, 180, 0);
        }
    }

    private void Start() => Initialize();
    
    private void Update()
    {
        if (!IsAlive) return;
        UpdateSpecialEffects();
        Move();
        if (Time.time - lastPathCheckTime >= PATH_CHECK_INTERVAL)
        {
            CheckNavMeshAgentPath();
            lastPathCheckTime = Time.time;
        }
        UpdateHealthBar();
    }
}