using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseTower : MonoBehaviour
{
    #region Inspector Fields
    [Header("Tower Base Settings")]
    [SerializeField] public TowerData towerData;
    [SerializeField] protected LayerMask enemyLayer = -1;

    [Header("Animation")]
    [SerializeField] protected Animator towerAnimator;
    [SerializeField] protected string attackAnimationName;
    #endregion

    #region Properties
    public virtual float AttackRange { get; protected set; } = 5f;
    public virtual float AttackDamage { get; protected set; } = 10f;
    public virtual float AttackCooltime { get; protected set; } = 1f;
    public bool IsAttacking { get; protected set; }
    #endregion

    #region Protected Fields
    protected Transform currentTarget;
    protected float nextAttackTime;
    protected bool isInitialized = false;
    #endregion

    #region Unity Lifecycle
    protected virtual void Start()
    {
        if (!isInitialized && towerData != null)
        {
            Initialize(towerData);
        }
        else if (towerData != null)
        {
            UpdateCachedStats();
        }
    }

    protected virtual void Update()
    {
        if (!isInitialized) return;

        if (currentTarget != null)
        {
            if (IsTargetInRange(currentTarget))
            {
                RotateTowardsTarget();
                TryAttack();
            }
            else
            {
                currentTarget = null;
            }
        }
        else if (IsAttacking)
        {
            FindTarget();
        }
    }

    private void OnDestroy()
    {
        if (transform.parent != null)
        {
            TowerPlacementZone zone = transform.parent.GetComponent<TowerPlacementZone>();
            if (zone != null)
            {
                zone.SetOccupied(false);
            }
        }
    }
    #endregion

    #region Public Methods
    public virtual void Initialize(TowerData data)
    {
        towerData = data;
        UpdateCachedStats();
        isInitialized = true;
        StartAttacking();
        StartCoroutine(ScanForEnemies());
    }

    public virtual void StartAttacking()
    {
        IsAttacking = true;
    }

    public virtual void StopAttacking()
    {
        IsAttacking = false;
        currentTarget = null;
    }

    public TowerData[] GetAvailableUpgradeOptions()
    {
        if (towerData?.canUpgrade != true)
            return new TowerData[0];

        var options = new List<TowerData>();

        if (towerData.upgradedVersion1 != null)
            options.Add(towerData.upgradedVersion1);

        if (towerData.upgradedVersion2 != null)
            options.Add(towerData.upgradedVersion2);

        return options.ToArray();
    }
    #endregion

    #region Protected Methods
    protected virtual void UpdateCachedStats()
    {
        if (towerData != null)
        {
            AttackRange = towerData.attackRange;
            AttackDamage = towerData.attackDamage;
            AttackCooltime = towerData.attackCooltime;
            Debug.Log($"타워 스탯 업데이트: {towerData.towerName} - 공격력={AttackDamage}, 사거리={AttackRange}, 쿨타임={AttackCooltime}");
        }
        else
        {
            AttackRange = 5f;
            AttackDamage = 10f;
            AttackCooltime = 1f;
        }
    }

    protected virtual IEnumerator ScanForEnemies()
    {
        while (isInitialized)
        {
            yield return new WaitForSeconds(0.2f);
            
            if (currentTarget == null && IsAttacking)
            {
                FindTarget();
            }
        }
    }

    protected virtual void FindTarget()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        if (enemiesInRange.Length == 0)
        {
            currentTarget = null;
            return;
        }

        List<Transform> validEnemies = new List<Transform>();
        foreach (Collider enemy in enemiesInRange)
        {
            IEnemy enemyComponent = enemy.GetComponent<IEnemy>();
            if (enemyComponent != null)
            {
                validEnemies.Add(enemy.transform);
            }
        }

        if (validEnemies.Count == 0)
        {
            currentTarget = null;
            return;
        }

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Transform enemy in validEnemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        currentTarget = nearestEnemy;
    }

    protected virtual bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= AttackRange;
    }

    protected virtual void RotateTowardsTarget()
    {
        if (currentTarget == null) return;

        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    protected virtual void TryAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + AttackCooltime;
        }
    }

    protected virtual void PerformAttack()
    {
        PlayAttackAnimation();
        PerformAttackLogic();
        ApplySpecialEffectsToTarget();
    }

    protected abstract void PerformAttackLogic();

    protected virtual void ApplySpecialEffectsToTarget()
    {
        if (currentTarget == null || towerData == null) return;

        BaseEnemy enemy = currentTarget.GetComponent<BaseEnemy>();
        if (enemy == null) return;

        if (towerData.useStunEffect)
        {
            enemy.ApplyStun(towerData.stunDuration);
            Debug.Log($"{towerData.towerName}: 스턴 효과 적용 ({towerData.stunDuration}초)");
        }

        if (towerData.useSlowEffect)
        {
            enemy.ApplySlow(towerData.slowDuration, towerData.slowSpeedMultiplier);
            Debug.Log($"{towerData.towerName}: 슬로우 효과 적용 ({towerData.slowDuration}초, {towerData.slowSpeedMultiplier:P0})");
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        if (towerAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            towerAnimator.SetTrigger(attackAnimationName);
        }
    }
    #endregion

    #region Gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        DrawAttackRange();
        DrawCurrentTarget();
    }

    protected virtual void DrawAttackRange()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.1f);
        Gizmos.DrawSphere(transform.position, AttackRange);

        DrawRangeLabel();
    }

    protected virtual void DrawCurrentTarget()
    {
        if (currentTarget == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, currentTarget.position);

        Gizmos.DrawWireSphere(currentTarget.position, 0.3f);

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        DrawLabel(currentTarget.position + Vector3.up * 1.5f, $"{distance:F1}m");
    }

    private void DrawRangeLabel()
    {
        Vector3 labelPosition = transform.position + Vector3.up * (AttackRange + 0.5f);
        string rangeText = $"{AttackRange:F1}m";
        DrawLabel(labelPosition, rangeText);
    }

    protected void DrawLabel(Vector3 position, string text)
    {
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
        #endif
    }
    #endregion
}