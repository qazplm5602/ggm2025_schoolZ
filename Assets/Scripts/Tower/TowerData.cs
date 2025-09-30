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
    public Sprite towerIcon;
    public int cost = 100;

    [Header("Combat Stats")]
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackCooltime = 1f; 

    [Header("Special Effects")]
    public bool useStunEffect = false;
    public float stunDuration = 2f;

    public bool useSlowEffect = false;
    public float slowDuration = 3f;
    [Range(0.1f, 1f)]
    public float slowSpeedMultiplier = 0.5f;


    [Header("Ranged Tower Settings")]
    public bool useAreaAttack = false; 
    public float areaAttackRadius = 2f;

    [Header("Upgrade")]
    public bool canUpgrade = true;
    public TowerData upgradedVersion1;
    public TowerData upgradedVersion2;
}
