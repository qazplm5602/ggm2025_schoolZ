using UnityEngine;

public enum TowerType
{
    Ranged,
    Melee
}

public class TowerData : ScriptableObject
{
    [Header("Basic Info")]
    public string towerName;
    [TextArea(2, 4)]
    public string description;
    public TowerType towerType;
    // towerPrefab 제거 - 타워 프리팹이 직접 TowerData를 참조하도록 변경
    public Sprite towerIcon;
    public int cost = 100;

    [Header("Combat Stats")]
    [Tooltip("공격 범위 (미터 단위) - 3m: 가까운 거리, 5m: 중간 거리, 10m: 먼 거리")]
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackCooltime = 1f; // 초당 공격 횟수

    [Header("Special Effects")]
    [Tooltip("스턴 효과 사용 여부")]
    public bool useStunEffect = false;
    [Tooltip("스턴 지속 시간 (초)")]
    public float stunDuration = 2f;

    [Tooltip("슬로우 효과 사용 여부")]
    public bool useSlowEffect = false;
    [Tooltip("슬로우 지속 시간 (초)")]
    public float slowDuration = 3f;
    [Tooltip("슬로우 시 이동속도 배율 (0.1 = 10%, 0.5 = 50%)")]
    [Range(0.1f, 1f)]
    public float slowSpeedMultiplier = 0.5f;


    [Header("Ranged Tower Settings")]
    [Tooltip("원거리 타워의 공격 방식 - 단일 타겟 또는 범위 공격")]
    public bool useAreaAttack = false; // false: 단일 타겟, true: 범위 공격
    [Tooltip("범위 공격 시 타겟 주변 반경 (미터)")]
    public float areaAttackRadius = 2f; // 범위 공격 반경

    // Melee Tower는 AttackRange만 사용 (범위 공격)
    // meleeAreaRadius 제거됨 - AttackRange로 통합

    [Header("Upgrade")]
    public bool canUpgrade = true;

    [Tooltip("업그레이드 옵션 1")]
    public TowerData upgradedVersion1;

    [Tooltip("업그레이드 옵션 2")]
    public TowerData upgradedVersion2;
}
