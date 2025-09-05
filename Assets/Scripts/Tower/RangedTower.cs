using UnityEngine;

public class RangedTower : BaseTower
{
    [Header("Ranged Tower Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    
    protected override void ApplyTowerData()
    {
        if (towerData != null)
        {
            if (projectilePrefab == null)
                projectilePrefab = towerData.projectilePrefab;
            if (projectileSpeed <= 0)
                projectileSpeed = towerData.projectileSpeed;
        }
    }

    protected override void PerformAttack()
    {
        if (currentTarget == null) return;
        
        // 머즐 플래시 재생
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        // 발사체 생성 및 발사
        if (projectilePrefab != null)
        {
            FireProjectile();
        }
        else
        {
            // 발사체가 없으면 즉시 데미지 적용
            ApplyDirectDamage();
        }
        
        Debug.Log($"원거리 타워가 {currentTarget.name}을 공격했습니다!");
    }
    
    private void FireProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, transform.rotation);

        // Projectile 스크립트가 있으면 초기화
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.Initialize(currentTarget, AttackDamage);
        }
        else
        {
            // 기본 발사체 동작 (Rigidbody 사용)
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (currentTarget.position - transform.position).normalized;
                rb.linearVelocity = direction * projectileSpeed;
            }
        }
    }
    
    private void ApplyDirectDamage()
    {
        IEnemy enemy = currentTarget.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(AttackDamage);
        }
    }
}