using UnityEngine;
using System.Collections;

public class AttackingEnemy : BaseEnemy
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackSpeed = 1f; // 초당 공격 횟수
    [SerializeField] private LayerMask towerLayer = -1;
    
    [Header("Attack Effects")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private ParticleSystem attackParticle;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private string attackAnimationName = "Attack";
    
    private Transform currentTarget;
    private float nextAttackTime;
    
    protected override void Start()
    {
        base.Start();
        
        // 애니메이터 자동 찾기
        if (enemyAnimator == null)
            enemyAnimator = GetComponent<Animator>();
            
        StartCoroutine(ScanForTargets());
    }
    
    protected override void Update()
    {
        base.Update();

        if (!IsAlive) return;

        if (currentTarget != null && IsTargetInRange(currentTarget))
        {
            // 공격 범위 내에 타겟이 있으면 공격
            if (agentMovement != null)
            {
                agentMovement.StopMovement(); // 공격 중 이동 정지
            }
            AttackTarget();
        }
        else
        {
            // 타겟이 없거나 범위를 벗어나면 이동
            currentTarget = null;
            if (agentMovement != null)
            {
                agentMovement.ResumeMovement(); // 이동 재개
            }
        }
    }
    
    // 이동은 AgentMovement에서 처리됨
    
    private IEnumerator ScanForTargets()
    {
        while (IsAlive)
        {
            yield return new WaitForSeconds(0.3f); // 0.3초마다 스캔
            
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
            // BaseTower 컴포넌트가 있는지 확인
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
        
        // 타겟 방향으로 회전
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0; // 수평 회전만
        
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        // 공격 쿨다운 체크
        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + (1f / attackSpeed);
        }
    }
    
    private void PerformAttack()
    {
        if (currentTarget == null) return;
        
        // 애니메이션 재생
        if (enemyAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            enemyAnimator.SetTrigger(attackAnimationName);
        }
        
        // 공격 이펙트 재생
        if (attackParticle != null)
            attackParticle.Play();
            
        if (attackEffect != null)
        {
            Vector3 effectPos = currentTarget.position + Vector3.up * 0.5f;
            Instantiate(attackEffect, effectPos, Quaternion.identity);
        }
        
        // 타워에게 데미지 적용 (TowerPlacementZone을 통해 처리)
        TowerPlacementZone zone = currentTarget.GetComponentInParent<TowerPlacementZone>();
        if (zone != null)
        {
            // 존에 타워가 있다면 데미지 적용
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

        // 데미지를 받으면 잠시 공격을 멈추고 이동 (옵션)
        // currentTarget = null;
    }
    
    protected override void OnDie()
    {
        base.OnDie();
        
        // 죽을 때 추가 효과
        StopAllCoroutines();
    }
    
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 현재 타겟 표시
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}