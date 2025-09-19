using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class BaseTower : MonoBehaviour
{
    [Header("Tower Base Settings")]
    [SerializeField] public TowerData towerData;
    [SerializeField] protected LayerMask enemyLayer = -1; // 기본값: Everything

    [Header("Animation")]
    [SerializeField] protected Animator towerAnimator;
    [SerializeField] protected string attackAnimationName;

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
        // 업그레이드 옵션이 있는 경우 첫 번째 옵션으로 업그레이드
        if (towerData?.canUpgrade == true && towerData.upgradedVersion1 != null)
        {
            // 기본적으로 첫 번째 옵션으로 업그레이드
            towerData = towerData.upgradedVersion1;
            OnUpgraded();
        }
        else
        {
            Debug.LogWarning("업그레이드할 수 있는 타워 버전이 없습니다.");
        }
    }

    /// <summary>
    /// 선택된 업그레이드 옵션으로 업그레이드 (0: 옵션1, 1: 옵션2)
    /// </summary>
    public virtual void UpgradeTower(int optionIndex)
    {
        if (towerData == null)
        {
            Debug.LogError("UpgradeTower 실패: towerData가 null입니다!");
            return;
        }

        if (towerData.canUpgrade != true)
        {
            Debug.LogError($"UpgradeTower 실패: {towerData.towerName}은 업그레이드 불가능합니다!");
            return;
        }

        if (optionIndex < 0 || optionIndex > 1)
        {
            Debug.LogError($"UpgradeTower 실패: 유효하지 않은 옵션 인덱스 {optionIndex}");
            return;
        }

        TowerData selectedUpgrade = null;

        if (optionIndex == 0 && towerData.upgradedVersion1 != null)
        {
            selectedUpgrade = towerData.upgradedVersion1;
            Debug.Log($"타워 업그레이드: {towerData.towerName} -> {selectedUpgrade.towerName} (옵션1)");
        }
        else if (optionIndex == 1 && towerData.upgradedVersion2 != null)
        {
            selectedUpgrade = towerData.upgradedVersion2;
            Debug.Log($"타워 업그레이드: {towerData.towerName} -> {selectedUpgrade.towerName} (옵션2)");
        }
        else
        {
            Debug.LogError($"UpgradeTower 실패: 업그레이드 옵션 {optionIndex}이(가) null입니다!");
            return;
        }

        // 데이터 업그레이드
        var oldTowerData = towerData;
        towerData = selectedUpgrade;
        Debug.Log($"타워 데이터 변경 완료: {oldTowerData.towerName} -> {towerData.towerName}");

        OnUpgraded();
    }

    /// <summary>
    /// 사용 가능한 업그레이드 옵션들을 반환 (TowerData 배열)
    /// </summary>
    public TowerData[] GetAvailableUpgradeOptions()
    {
        if (towerData?.canUpgrade != true)
            return new TowerData[0];

        var options = new System.Collections.Generic.List<TowerData>();

        // upgradedVersion1과 upgradedVersion2 추가
        if (towerData.upgradedVersion1 != null)
            options.Add(towerData.upgradedVersion1);

        if (towerData.upgradedVersion2 != null)
            options.Add(towerData.upgradedVersion2);

        return options.ToArray();
    }

    /// <summary>
    /// 업그레이드 옵션의 이름을 반환
    /// </summary>
    public string[] GetUpgradeOptionNames()
    {
        var options = GetAvailableUpgradeOptions();
        var names = new string[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            names[i] = options[i].towerName;
        }

        return names;
    }

    /// <summary>
    /// 업그레이드 옵션의 설명을 반환
    /// </summary>
    public string[] GetUpgradeOptionDescriptions()
    {
        var options = GetAvailableUpgradeOptions();
        var descriptions = new string[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            TowerData option = options[i];
            if (!string.IsNullOrEmpty(option.description))
            {
                descriptions[i] = option.description;
            }
            else
            {
                // 기본 설명 생성
                descriptions[i] = $"{option.towerName}\n" +
                    $"공격력: {option.attackDamage}\n" +
                    $"범위: {option.attackRange}m\n" +
                    $"쿨타임: {option.attackCooltime}s";
            }
        }

        return descriptions;
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

        if (enemiesInRange.Length == 0)
        {
            currentTarget = null;
            return;
        }

        // IEnemy 컴포넌트가 있는 적들만 필터링
        System.Collections.Generic.List<Transform> validEnemies = new System.Collections.Generic.List<Transform>();
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

        // 가장 가까운 적 찾기
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
    
    /// <summary>
    /// 공격 수행 (모든 타워에서 공통적으로 사용)
    /// </summary>
    protected virtual void PerformAttack()
    {
        // 애니메이션 재생
        PlayAttackAnimation();

        // 공격 이펙트 재생
        PlayAttackEffects();

        // 실제 공격 로직 (하위 클래스에서 오버라이드)
        PerformAttackLogic();

        // 스턴/슬로우 효과 적용 (타워 데이터에 따라)
        ApplySpecialEffectsToTarget();
    }

    /// <summary>
    /// 실제 공격 로직 (하위 클래스에서 오버라이드하여 구현)
    /// </summary>
    protected abstract void PerformAttackLogic();

    /// <summary>
    /// 스턴/슬로우 효과를 타겟에 적용
    /// </summary>
    protected virtual void ApplySpecialEffectsToTarget()
    {
        if (currentTarget == null || towerData == null) return;

        // 타겟에서 BaseEnemy 컴포넌트 찾기
        BaseEnemy enemy = currentTarget.GetComponent<BaseEnemy>();
        if (enemy == null) return;

        // 스턴 효과 적용
        if (towerData.useStunEffect)
        {
            enemy.ApplyStun(towerData.stunDuration);
            Debug.Log($"{towerData.towerName}: 스턴 효과 적용 ({towerData.stunDuration}초)");
        }

        // 슬로우 효과 적용
        if (towerData.useSlowEffect)
        {
            enemy.ApplySlow(towerData.slowDuration, towerData.slowSpeedMultiplier);
            Debug.Log($"{towerData.towerName}: 슬로우 효과 적용 ({towerData.slowDuration}초, {towerData.slowSpeedMultiplier:P0})");
        }
    }

    /// <summary>
    /// 공격 애니메이션 재생
    /// </summary>
    protected virtual void PlayAttackAnimation()
    {
        if (towerAnimator != null && !string.IsNullOrEmpty(attackAnimationName))
        {
            towerAnimator.SetTrigger(attackAnimationName);
        }
    }

    /// <summary>
    /// 공격 이펙트 재생
    /// </summary>
    protected virtual void PlayAttackEffects()
    {
        // 이펙트 기능 제거됨
    }
    
    // 가상 메서드 - 필요시 오버라이드
    protected virtual void ApplyTowerData()
    {
        // 기본 설정 적용 및 검증
        if (towerData != null)
        {
            // 설정 값 검증 (경고는 제거)
            if (AttackRange <= 0)
            {
                // AttackRange가 0 이하일 때는 기본값 사용
            }
            if (AttackDamage <= 0)
            {
                // AttackDamage가 0 이하일 때는 기본값 사용
            }
            if (AttackCooltime <= 0)
            {
                // AttackCooltime이 0 이하일 때는 기본값 사용
            }
        }
    }

    protected virtual void OnUpgraded()
    {
        // 업그레이드 시 TowerData 정보 재적용
        ApplyTowerData();
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // 공격 범위 시각화
        DrawAttackRange();

        // 현재 타겟 표시
        DrawCurrentTarget();

        // 타워 타입별 추가 시각화
        DrawTowerSpecificGizmos();
    }


    /// <summary>
    /// 공격 범위 표시
    /// </summary>
    protected virtual void DrawAttackRange()
    {
        // 기본 공격 범위 (빨간색)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f); // 반투명 빨간색
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        // 범위 채우기 (더 연한 빨간색)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.1f);
        Gizmos.DrawSphere(transform.position, AttackRange);

        // 범위 정보 텍스트
        DrawRangeLabel();
    }

    /// <summary>
    /// 현재 타겟 표시
    /// </summary>
    protected virtual void DrawCurrentTarget()
    {
        if (currentTarget == null) return;

        // 타겟 방향 선 표시
        Gizmos.color = Color.yellow;
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        Gizmos.DrawLine(transform.position, currentTarget.position);

        // 타겟 위치 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentTarget.position, 0.3f);

        // 타겟과의 거리 표시
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        DrawLabel(currentTarget.position + Vector3.up * 1.5f, $"{distance:F1}m");
    }

    /// <summary>
    /// 타워 타입별 추가 시각화 (하위 클래스에서 오버라이드 가능)
    /// </summary>
    protected virtual void DrawTowerSpecificGizmos()
    {
        // 기본적으로 아무것도 표시하지 않음
        // 하위 클래스에서 오버라이드하여 사용
    }

    /// <summary>
    /// 범위 정보 라벨 표시
    /// </summary>
    private void DrawRangeLabel()
    {
        Vector3 labelPosition = transform.position + Vector3.up * (AttackRange + 0.5f);
        string rangeText = $"{AttackRange:F1}m";
        DrawLabel(labelPosition, rangeText);
    }

    /// <summary>
    /// 3D 텍스트 라벨 표시
    /// </summary>
    protected void DrawLabel(Vector3 position, string text)
    {
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
        #endif
    }

    /// <summary>
    /// 타워 파괴 시 정리 작업
    /// </summary>
    private void OnDestroy()
    {
        // 부모가 TowerPlacementZone인 경우 점유 상태 해제
        if (transform.parent != null)
        {
            TowerPlacementZone zone = transform.parent.GetComponent<TowerPlacementZone>();
            if (zone != null)
            {
                zone.SetOccupied(false);
            }
        }
    }
}