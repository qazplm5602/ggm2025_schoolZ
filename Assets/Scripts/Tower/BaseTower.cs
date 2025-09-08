using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseTower : MonoBehaviour
{
    [Header("Tower Base Settings")]
    [SerializeField] public TowerData towerData;
    [SerializeField] protected LayerMask enemyLayer = -1; // 기본값: Everything
    
    [Header("Visual Effects")]
    [SerializeField] protected ParticleSystem muzzleFlash;
    [SerializeField] protected GameObject upgradeEffect;
    
    // 타워 기본 프로퍼티들
    public virtual float AttackRange => towerData?.attackRange ?? 5f;
    public virtual float AttackDamage => towerData?.attackDamage ?? 10f;
    public virtual float AttackCooltime => towerData?.attackCooltime ?? 1f;
    public bool IsAttacking { get; protected set; }
    
    // 보호된 필드들
    protected Transform currentTarget;
    protected List<Transform> enemiesInRange = new List<Transform>();
    protected float nextAttackTime;
    protected bool isInitialized = false;
    
    protected virtual void Start()
    {
        SetupComponents();
        if (!isInitialized && towerData != null)
        {
            Initialize(towerData);
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
            // 타겟이 없으면 적을 찾음
            FindTarget();
        }
    }
    
    public virtual void Initialize(TowerData data)
    {
        towerData = data;
        isInitialized = true;

        // TowerData 정보 적용 (하위 클래스에서 오버라이드 가능)
        ApplyTowerData();

        // 자동으로 공격 시작
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
    
    public virtual void UpgradeTower()
    {
        if (towerData?.canUpgrade == true && towerData.upgradedVersion != null)
        {
            // 업그레이드 이펙트 재생
            if (upgradeEffect != null)
            {
                Instantiate(upgradeEffect, transform.position, transform.rotation);
            }
            
            // 데이터 업그레이드
            towerData = towerData.upgradedVersion;
            OnUpgraded();
        }
    }
    
    protected virtual void SetupComponents()
    {
        // 필요한 컴포넌트 초기화 (현재는 없음)
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

        // 디버그 로깅 (적을 찾지 못할 때만)
        if (enemiesInRange.Length == 0)
        {
            Debug.Log($"{gameObject.name} - 범위 내 콜라이더 없음, 공격 범위: {AttackRange}, 레이어 마스크: {enemyLayer.value}");
            Debug.Log($"{gameObject.name} - 현재 위치: {transform.position}");
        }

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemiesInRange)
        {
            IEnemy enemyComponent = enemy.GetComponent<IEnemy>();
            if (enemyComponent != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy.transform;
                }
            }
        }

        currentTarget = nearestEnemy;

        // 디버그 로깅
        if (currentTarget != null)
        {
            Debug.Log($"{gameObject.name}이 적을 발견했습니다: {currentTarget.name} (거리: {nearestDistance:F2})");
        }
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
        direction.y = 0; // 수평 회전만

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
    
    // 추상 메서드 - 각 타워 타입에서 구현
    protected abstract void PerformAttack();
    
    // 가상 메서드 - 필요시 오버라이드
    protected virtual void ApplyTowerData()
    {
        // 기본적으로 아무것도 하지 않음 (하위 클래스에서 오버라이드)
    }

    protected virtual void OnUpgraded()
    {
        Debug.Log($"{towerData.towerName} 타워가 업그레이드되었습니다!");

        // 업그레이드 시 TowerData 정보 재적용
        ApplyTowerData();
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
        
        // 현재 타겟 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}