using UnityEngine;
using System.Collections.Generic;

public class MeleeTower : BaseTower
{
    // Melee Tower - 범위 공격 특화
    


    protected override void FindTarget()
    {
        // 근접 타워는 타겟을 찾기만 하면 범위 공격을 수행
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.GetComponent<IEnemy>() != null)
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
    }
    
    
    protected override bool IsTargetInRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= AttackRange;
    }
    
    protected override void PerformAttackLogic()
    {
        if (currentTarget == null) return;

        // 타겟을 중심으로 범위 공격 수행
        PerformAreaAttack();
    }

    /// <summary>
    /// 타겟을 중심으로 범위 공격 수행
    /// </summary>
    private void PerformAreaAttack()
    {
        if (currentTarget == null) return;

        // 타워의 공격 범위 내에서 적을 찾아 공격 (attackRange와 meleeAreaRadius 통합)
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        int hitCount = 0;
        foreach (Collider collider in enemiesInRange)
        {
            IEnemy enemy = collider.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);
                hitCount++;
            }
        }

    }


    /// <summary>
    /// 근접 타워 전용 공격 이펙트 재생 (BaseTower의 PlayAttackEffects 오버라이드)
    /// </summary>
    protected override void PlayAttackEffects()
    {
        // 기본 이펙트 재생 (머즐 플래시)
        base.PlayAttackEffects();

        // 근접 타워 특유의 이펙트는 PerformAreaAttack에서 개별적으로 처리
    }
    
    protected override void OnUpgraded()
    {
        base.OnUpgraded();
        // ApplyTowerData()가 자동으로 호출되므로 별도 처리 불필요
    }
    
    /// <summary>
    /// 근접 타워 특화 시각화
    /// </summary>
    protected override void DrawTowerSpecificGizmos()
    {
        // 근접 타워의 고유 시각화
        DrawMeleeVisualization();
    }

    /// <summary>
    /// 근접 타워 시각화 (타워 중심 범위 공격)
    /// </summary>
    private void DrawMeleeVisualization()
    {
        if (currentTarget == null) return;

        // 타워 중심 범위 공격 반경 표시 (attackRange 사용)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // 반투명 주황색
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // 타워에서 타겟으로의 선
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, currentTarget.position);

        // 타겟 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentTarget.position, 0.3f);

        // 범위 정보 표시
        Vector3 infoPos = transform.position + Vector3.up * (AttackRange + 0.5f);
        DrawLabel(infoPos, $"근접 범위 공격\n반경: {AttackRange}m");
    }

}