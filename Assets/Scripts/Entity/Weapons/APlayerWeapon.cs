using FishNet.Object;
using UnityEngine;

namespace Entity.Weapons
{
    public abstract class APlayerWeapon : NetworkBehaviour
    {
        public int damage;

        public LayerMask weaponHitLayers;
        [SerializeField] private Transform _cameraTransform;

        public float maxRange = 20f;

        // private void Awake()
        // {
        //     _cameraTransform = Camera.main.transform;
        // }

        public void Fire()
        {
            AnimateWeapon();
            Debug.DrawLine(_cameraTransform.position, _cameraTransform.forward * maxRange, Color.green, duration: 0.5f, depthTest: true);
            if (!Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, maxRange,
                    weaponHitLayers))
            {
                Debug.Log("No hit");
                return;
            }
            if (hit.transform.TryGetComponent(out EntityHealthManager health))
            {
                health.TakeDamage(damage);
                Debug.Log($"Hit {health.Owner.ClientId} for {damage}");
            }
        }

        public abstract void AnimateWeapon();
    }
}


