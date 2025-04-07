using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace Entity.Projectiles
{
    public class ProjectileManager : NetworkBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed;
        [SerializeField] private float projectileRange;
        [SerializeField] private Transform projectileOrigin;
        private List<Projectile> _projectiles = new List<Projectile>();

        private void Update()
        {
            foreach (Projectile projectile in _projectiles)
            {
                projectile.projectileTransform.position += projectile.direction * Time.deltaTime * projectileSpeed;
            }
            
            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.Mouse0))
                FireProjectile();
        }

        private void FireProjectile()
        {
            Vector3 spawnPosition = projectileOrigin.position;
            Vector3 direction = projectileOrigin.forward;
            
            SpawnProjectileLocal(spawnPosition, direction);
            SpawnProjectile(spawnPosition, direction, TimeManager.Tick);
        }

        private void SpawnProjectileLocal(Vector3 spawnPosition, Vector3 direction)
        {
            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            _projectiles.Add(new Projectile() { projectileTransform = projectile.transform, direction = direction });
        }

        [ServerRpc]
        private void SpawnProjectile(Vector3 startPosition, Vector3 direction, uint startTick)
        {
            SpawnProjectileObserver(startPosition, direction, startTick);
        }

        [ObserversRpc(ExcludeOwner = true)]
        private void SpawnProjectileObserver(Vector3 startPosition, Vector3 direction, uint startTick)
        {
            float timeDiff = (float)(TimeManager.Tick - startTick) / TimeManager.TickRate;
            Vector3 compensatedPosition = startPosition + direction * (timeDiff * projectileSpeed);
            SpawnProjectileLocal(compensatedPosition, direction);
        }
    }
}