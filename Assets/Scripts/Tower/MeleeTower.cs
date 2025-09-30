using UnityEngine;
using System.Collections.Generic;

public class MeleeTower : BaseTower
{

    protected override void FindTarget()
    {
        // 근접 타워는 타겟을 찾기만 하면 범위 공격을 수행
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, AttackRange, enemyLayer);

        Transform nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider enemy in enemiesInRange)
        {
            if (enemy.GetComponent<BasicEnemy>() != null)
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
            BasicEnemy enemy = collider.GetComponent<BasicEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);

                if (towerData.useStunEffect)
                {
                    BasicEnemy baseEnemy = collider.GetComponent<BasicEnemy>();
                    if (baseEnemy != null)
                    {
                        baseEnemy.ApplyStun(towerData.stunDuration);
                    }
                }

                hitCount++;
            }
        }

    }
    

    protected override void ApplySpecialEffectsToTarget()
    {
    }
    
    protected override void DrawTowerSpecificGizmos()
    {
        // 근접 타워의 고유 시각화
        DrawMeleeVisualization();
    }

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