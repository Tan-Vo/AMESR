public interface IDamageable
{
    bool IsAlive { get; }
    /// <summary>Applies resolved damage to this entity.</summary>
    void TakeDamage(int amount);
}
