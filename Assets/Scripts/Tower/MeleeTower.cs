using UnityEngine;
using System.Collections.Generic;

public class MeleeTower : BaseTower
{
    [Header("Melee Tower Settings")]
    [SerializeField] private GameObject slashEffect;
    
    [Header("Animation")]
    [SerializeField] private Animator towerAnimator;
    [SerializeField] private string attackAnimationName;
    
    private List<Transform> currentTargets = new List<Transform>();
    
    protected override void ApplyTowerData()
    {
        // maxTargets 설정 제거 - 제한 없이 모든 적 공격
        // TowerData의 다른 설정들은 BaseTower에서 처리됨

        // 애니메이터 자동 찾기 (자식 객체 포함)
        if (towerAnimator == null)
            towerAnimator = GetComponentInChildren<Animator>();
    }
    
    protected override void FindTarget()
    {
        // 근접 타워는 범위 내 모든 적을 동시에 공격할 수 있음
        currentTargets.Clear();

        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.GetComponent<IEnemy>() != null)
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
        return distance <= AttackRange;
    }
    
    protected override void PerformAttack()
    {
        if (currentTargets.Count == 0) return;

        // 애니메이션 재생
        if (towerAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            towerAnimator.SetTrigger(attackAnimationName);
        }

        // 공격 이펙트 재생
        PlayAttackEffects();

        // 0.1초 딜레이 후 데미지 적용
        StartCoroutine(ApplyDamageWithDelay(0.1f));
    }

    private System.Collections.IEnumerator ApplyDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 범위 내 모든 적에게 데미지 적용
        AttackAllTargetsInRange();

        Debug.Log($"근접 타워가 {currentTargets.Count}명의 적을 공격했습니다!");
    }
    
    private void AttackAllTargetsInRange()
    {
        // 현재 범위 내의 모든 적을 공격 (제한 없음)
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        int attackedCount = 0;
        foreach (Collider enemyCollider in enemiesInRange)
        {
            IEnemy enemy = enemyCollider.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);
                attackedCount++;

                // 개별 타격 이펙트 (옵션)
                if (slashEffect != null)
                {
                    Vector3 effectPos = enemyCollider.transform.position + Vector3.up * 0.5f;
                    Instantiate(slashEffect, effectPos, Quaternion.identity);
                }
            }
        }

        Debug.Log($"근접 타워가 {attackedCount}명의 적을 공격했습니다!");
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
        // ApplyTowerData()가 자동으로 호출되므로 별도 처리 불필요
    }
    
    protected override void OnDrawGizmosSelected()
    {
        // 근접 공격 범위 시각화
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

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