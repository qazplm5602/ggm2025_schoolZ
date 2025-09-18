using UnityEngine;

public class RangedTower : BaseTower
{
    // 원거리 타워는 간단한 단일 타겟 공격 사용

    protected override void PerformAttackLogic()
    {
        // 타겟 존재 및 범위 내 확인
        if (currentTarget == null)
        {
            return;
        }

        // 공격 시점에 다시 한 번 범위 확인 (안전장치)
        if (!IsTargetInRange(currentTarget))
        {
            currentTarget = null; // 타겟 리셋
            return;
        }

        // 타겟이 여전히 유효한지 확인
        IEnemy enemyComponent = currentTarget.GetComponent<IEnemy>();
        if (enemyComponent == null)
        {
            currentTarget = null; // 타겟 리셋
            return;
        }

        // 모든 체크 통과 시 공격 수행
        PerformSimpleAttack();
    }
    
    
    /// <summary>
    /// 설정에 따른 공격 수행 (단일 타겟 또는 범위 공격)
    /// </summary>
    private void PerformSimpleAttack()
    {
        if (currentTarget == null) return;

        // TowerData에서 직접 설정 확인
        if (towerData != null && towerData.useAreaAttack)
        {
            // 범위 공격 모드
            PerformAreaAttack();
        }
        else
        {
            // 단일 타겟 공격 모드 (기본)
            PerformSingleTargetAttack();
        }
    }

    /// <summary>
    /// 단일 타겟 공격
    /// </summary>
    private void PerformSingleTargetAttack()
    {
        IEnemy enemy = currentTarget.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(AttackDamage);
        }
    }

    /// <summary>
    /// 범위 공격 (타겟 주변 범위 내 모든 적)
    /// </summary>
    private void PerformAreaAttack()
    {
        // 타겟 유효성 재확인
        if (currentTarget == null)
        {
            return;
        }

        // 현재 타겟을 중심으로 범위 내 모든 적 찾기 (TowerData에서 직접 읽기)
        Vector3 center = currentTarget.position;
        float attackRadius = (towerData != null) ? towerData.areaAttackRadius : 2f;
        Collider[] enemiesInRange = Physics.OverlapSphere(center, attackRadius, enemyLayer);

        foreach (Collider collider in enemiesInRange)
        {
            IEnemy enemy = collider.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);
            }
        }
    }

    /// <summary>
    /// 원거리 타워 특화 시각화
    /// </summary>
    protected override void DrawTowerSpecificGizmos()
    {
        if (currentTarget == null || towerData == null) return;

        // 범위 공격 모드일 때만 특화 시각화
        if (towerData.useAreaAttack)
        {
            DrawAreaAttackVisualization();
        }
    }

    /// <summary>
    /// 범위 공격 시각화
    /// </summary>
    private void DrawAreaAttackVisualization()
    {
        // 범위 공격 반경 표시 (TowerData에서 직접 읽기)
        float attackRadius = (towerData != null) ? towerData.areaAttackRadius : 2f;
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // 반투명 빨간색
        Gizmos.DrawWireSphere(currentTarget.position, attackRadius);

        // 범위 정보 표시
        Vector3 labelPos = currentTarget.position + Vector3.up * (attackRadius + 0.5f);
        DrawLabel(labelPos, $"범위 공격\n반경: {attackRadius}m");
    }
}