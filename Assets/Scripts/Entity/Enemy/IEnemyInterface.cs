using UnityEngine;

namespace Entity.Enemy
{
    /// <summary>
    /// Common interface for all enemies
    /// </summary>
    public interface IEnemyInterface
    {

        /// <summary>
        /// Executes basic attack at the specified target
        /// </summary>
        /// <param name="target">Direction to attack in</param>
        void BasicAttack(Vector3 target);
    
        /// <summary>
        /// Damage points to inflict on this enemy
        /// </summary>
        /// <param name="damage">Damage to take</param>
        void TakeDamage(float damage);
        
        /// <summary>
        /// Enemy tries to move to the given global position
        /// </summary>
        /// <param name="target">Global position to move towards</param>
        void MoveTo(Transform target);
        
        /// <summary>
        /// Executes the death sequence without cleanup management
        /// </summary>
        void ExecuteDeath();

        /// <summary>
        /// Cleans up the allocated resources of this game object
        /// </summary>
        void CleanupResources();
    }
}
