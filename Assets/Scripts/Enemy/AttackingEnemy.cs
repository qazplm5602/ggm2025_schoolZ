using UnityEngine;
using System.Collections;

public class AttackingEnemy : BasicEnemy
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private LayerMask towerLayer = -1;
    
    [Header("Attack Effects")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private ParticleSystem attackParticle;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string attackAnimationName = "Attack";
    
    private Transform currentTarget;
    private float nextAttackTime;
    
    public override void Initialize()
    {
        base.Initialize();
        
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<Animator>();
            
        StartCoroutine(ScanForTargets());
    }

    private void Update()
    {
        if (!IsAlive) return;

        UpdateSpecialEffects();
        
        if (currentTarget != null && IsTargetInRange(currentTarget))
        {
            if (agentMovement != null)
            {
                agentMovement.StopMovement();
            }
            AttackTarget();
        }
        else
        {
            currentTarget = null;
            if (agentMovement != null)
            {
                agentMovement.ResumeMovement();
            }
            Move();
        }

        if (Time.time - lastPathCheckTime >= PATH_CHECK_INTERVAL)
        {
            CheckNavMeshAgentPath();
            lastPathCheckTime = Time.time;
        }

        UpdateHealthBar();
    }
    
    private IEnumerator ScanForTargets()
    {
        while (IsAlive)
        {
            yield return new WaitForSeconds(0.3f);
            
            if (currentTarget == null)
            {
                FindNearestTower();
            }
        }
    }
    
    private void FindNearestTower()
    {
        Collider[] towersInRange = Physics.OverlapSphere(transform.position, attackRange, towerLayer);
        
        Transform nearestTower = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (Collider tower in towersInRange)
        {
            if (tower.GetComponent<BaseTower>() != null)
            {
                float distance = Vector3.Distance(transform.position, tower.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTower = tower.transform;
                }
            }
        }
        
        currentTarget = nearestTower;
    }
    
    private bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= attackRange;
    }
    
    private void AttackTarget()
    {
        if (currentTarget == null) return;
        
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + (1f / attackSpeed);
        }
    }
    
    private void PerformAttack()
    {
        if (currentTarget == null) return;
        
        if (enemyAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            enemyAnimator.SetTrigger(attackAnimationName);
        }
        
        if (attackParticle != null)
            attackParticle.Play();
            
        if (attackEffect != null)
        {
            Vector3 effectPos = currentTarget.position + Vector3.up * 0.5f;
            Instantiate(attackEffect, effectPos, Quaternion.identity);
        }
        
        TowerPlacementZone zone = currentTarget.GetComponentInParent<TowerPlacementZone>();
        if (zone != null)
        {
            BaseTower tower = zone.GetPlacedTower();
            if (tower != null)
            {
                // 타워 데미지 시스템 구현 예정
                // tower.TakeDamage(damage);
            }
        }
    }
    
    protected override void OnTakeDamage(float damage)
    {
        base.OnTakeDamage(damage);
        // 추가 로직이 필요하면 여기에
    }
    
    protected override void OnDie()
    {
        base.OnDie();
        StopAllCoroutines();
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}