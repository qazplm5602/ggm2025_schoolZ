using UnityEngine;
using System.Collections.Generic;

public class MeleeTower : BaseTower
{
    [Header("Melee Tower Settings")]
    [SerializeField] private float meleeRadius = 2f;
    [SerializeField] private int maxTargets = 3;
    [SerializeField] private GameObject slashEffect;
    
    [Header("Animation")]
    [SerializeField] private Animator towerAnimator;
    [SerializeField] private string attackAnimationName = "Attack";
    
    private List<Transform> currentTargets = new List<Transform>();
    
    protected override void Start()
    {
        base.Start();
        
        // TowerData에서 근접 공격 정보 가져오기
        if (towerData != null)
        {
            meleeRadius = towerData.meleeRadius;
            maxTargets = towerData.maxTargets;
        }
        
        // 애니메이터 자동 찾기
        if (towerAnimator == null)
            towerAnimator = GetComponent<Animator>();
            
        StartAttacking(); // 자동으로 공격 시작
    }
    
    protected override void FindTarget()
    {
        // 근접 타워는 여러 타겟을 동시에 공격할 수 있음
        currentTargets.Clear();
        
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, meleeRadius, enemyLayer);
        
        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.GetComponent<IEnemy>() != null && currentTargets.Count < maxTargets)
            {
                currentTargets.Add(enemy.transform);
            }
        }
        
        // 주 타겟 설정 (가장 가까운 적)
        if (currentTargets.Count > 0)
        {
            currentTarget = GetNearestTarget();
        }
        else
        {
            currentTarget = null;
        }
    }
    
    private Transform GetNearestTarget()
    {
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (Transform target in currentTargets)
        {
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }
        }
        
        return nearest;
    }
    
    protected override bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= meleeRadius;
    }
    
    protected override void PerformAttack()
    {
        if (currentTargets.Count == 0) return;
        
        // 애니메이션 재생
        if (towerAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            towerAnimator.SetTrigger(attackAnimationName);
        }
        
        // 범위 내 모든 적에게 데미지 적용
        AttackAllTargetsInRange();
        
        // 공격 이펙트 재생
        PlayAttackEffects();
        
        Debug.Log($"근접 타워가 {currentTargets.Count}명의 적을 공격했습니다!");
    }
    
    private void AttackAllTargetsInRange()
    {
        // 현재 범위 내의 모든 적을 다시 확인
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, meleeRadius, enemyLayer);
        
        int attackCount = 0;
        foreach (Collider enemyCollider in enemiesInRange)
        {
            if (attackCount >= maxTargets) break;
            
            IEnemy enemy = enemyCollider.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);
                attackCount++;
                
                // 개별 타격 이펙트 (옵션)
                if (slashEffect != null)
                {
                    Vector3 effectPos = enemyCollider.transform.position + Vector3.up * 0.5f;
                    Instantiate(slashEffect, effectPos, Quaternion.identity);
                }
            }
        }
    }
    
    private void PlayAttackEffects()
    {
        // 머즐 플래시를 공격 이펙트로 사용
        if (muzzleFlash != null)
            muzzleFlash.Play();
    }
    
    protected override void OnUpgraded()
    {
        base.OnUpgraded();
        
        // 업그레이드 시 근접 공격 정보 갱신
        if (towerData != null)
        {
            meleeRadius = towerData.meleeRadius;
            maxTargets = towerData.maxTargets;
        }
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // 근접 공격 범위 시각화
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, meleeRadius);
        
        // 현재 타겟들 표시
        Gizmos.color = Color.yellow;
        foreach (Transform target in currentTargets)
        {
            if (target != null)
            {
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
        
        // 기본 범위도 표시
        base.OnDrawGizmosSelected();
    }
}