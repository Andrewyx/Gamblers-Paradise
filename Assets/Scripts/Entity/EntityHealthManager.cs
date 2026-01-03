using FishNet.Object;
using UnityEngine;

namespace Entity
{
    public class EntityHealthManager : NetworkBehaviour
    {
        [SerializeField] private int maxHealth = 1;

        [SerializeField] private int _currentHealth;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                enabled = false;
                return;
            }
            _currentHealth = maxHealth;
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;
            Debug.Log("Entity Health: " + _currentHealth);
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        private void Die()
        {
            Debug.Log("Played died");
            Destroy(gameObject);
        }
    }
}
