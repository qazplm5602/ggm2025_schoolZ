using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 5f;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public ParticleSystem trailEffect;
    
    private Transform target;
    private Vector3 targetPosition;
    private bool hasTarget = false;
    
    private void Start()
    {
        // 일정 시간 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }
    
    private void Update()
    {
        if (hasTarget)
        {
            MoveTowardsTarget();
        }
        else
        {
            // 타겟이 없으면 직진
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
    }
    
    public void Initialize(Transform enemyTarget, float projectileDamage)
    {
        target = enemyTarget;
        damage = projectileDamage;
        hasTarget = true;
        
        if (target != null)
        {
            targetPosition = target.position;
            
            // 타겟 방향으로 회전
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    private void MoveTowardsTarget()
    {
        if (target != null)
        {
            // 타겟이 살아있으면 계속 추적
            targetPosition = target.position;
        }
        
        // 타겟 위치로 이동
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        
        // 타겟에 도달했는지 확인
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.2f)
        {
            HitTarget();
        }
    }
    
    private void HitTarget()
    {
        // 타겟에게 데미지 적용
        if (target != null)
        {
            IEnemy enemy = target.GetComponent<IEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
        
        // 히트 이펙트 재생
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, transform.rotation);
        }
        
        // 발사체 파괴
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // 적과 충돌했을 때
        IEnemy enemy = other.GetComponent<IEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            
            // 히트 이펙트 재생
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }
            
            Destroy(gameObject);
        }
    }
}