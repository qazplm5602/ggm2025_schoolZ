using UnityEngine;

public interface IEnemy
{
    float MaxHealth { get; }
    float CurrentHealth { get; }
    float MoveSpeed { get; }
    bool IsAlive { get; }
    
    void TakeDamage(float damage);
    void Die();
    void Move();
    void Initialize();
}