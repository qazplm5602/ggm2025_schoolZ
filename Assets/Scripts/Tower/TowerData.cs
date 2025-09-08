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
    public TowerType towerType;
    // towerPrefab 제거 - 타워 프리팹이 직접 TowerData를 참조하도록 변경
    public Sprite towerIcon;
    public int cost = 100;

    [Header("Combat Stats")]
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackCooltime = 1f; // 초당 공격 횟수

    [Header("Ranged Tower Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    // Melee Tower Settings 제거 - AttackRange로 통합

    [Header("Upgrade")]
    public bool canUpgrade = true;
    public TowerData upgradedVersion;
}
